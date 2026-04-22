using System.Text;
using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace CtrlCV
{
    internal sealed class CtrlCvStore : IDisposable
    {
        private const int DebounceMs = 500;
        private const string PinnedCollection = "pinned_items";
        private const string KvCollection = "kv";
        private const string SettingsId = "settings";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true
        };

        private readonly string _dbPath;
        private readonly string _legacySettingsPath;
        private readonly System.Threading.Timer _timer;
        private readonly object _writeGate = new();
        private readonly object _warnGate = new();

        private LiteDatabase? _db;
        private ILiteCollection<PersistedClipboardItem>? _pinnedCol;
        private ILiteCollection<BsonDocument>? _kvCol;
        private ILiteStorage<string>? _storage;

        private List<PinnedSnapshotEntry>? _pendingPinned;
        private string? _pendingSettingsJson;

        private bool _textTooLargeWarned;
        private bool _imageTooLargeWarned;
        private bool _disposed;

        private readonly List<(string Title, string Message)> _earlyNotifications = new();
        private bool _notificationsReplayed;

        private event Action<string, string>? _notificationRequested;
        public event Action<string, string>? NotificationRequested
        {
            add
            {
                List<(string Title, string Message)>? toReplay = null;
                lock (_earlyNotifications)
                {
                    _notificationRequested += value;
                    if (!_notificationsReplayed && _earlyNotifications.Count > 0)
                    {
                        _notificationsReplayed = true;
                        toReplay = new List<(string, string)>(_earlyNotifications);
                        _earlyNotifications.Clear();
                    }
                }
                if (toReplay != null)
                {
                    foreach (var n in toReplay)
                        value?.Invoke(n.Title, n.Message);
                }
            }
            remove
            {
                lock (_earlyNotifications)
                {
                    _notificationRequested -= value;
                }
            }
        }

        private void RaiseNotification(string title, string message)
        {
            Action<string, string>? handler;
            lock (_earlyNotifications)
            {
                handler = _notificationRequested;
                if (handler == null)
                {
                    _earlyNotifications.Add((title, message));
                    return;
                }
            }
            handler.Invoke(title, message);
        }

        public bool DegradedMode { get; private set; }

        public string DatabasePath => _dbPath;

        public CtrlCvStore(string directory, string dbFileName = "CtrlCV.db", string legacySettingsFileName = "settings.json")
        {
            Directory.CreateDirectory(directory);
            _dbPath = Path.Combine(directory, dbFileName);
            _legacySettingsPath = Path.Combine(directory, legacySettingsFileName);
            _timer = new System.Threading.Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);

            OpenWithRecovery();
        }

        #region Open / recovery

        private void OpenWithRecovery()
        {
            try
            {
                OpenCore();
                return;
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to open LiteDB, attempting recovery", ex);
                TryRenameCorrupt();
            }

            try
            {
                OpenCore();
                RaiseNotification(
                    "CtrlCV",
                    "A corrupted database was replaced. Previously pinned items may have been lost.");
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to open LiteDB after recovery", ex);
                DegradedMode = true;
                _db = null;
                _pinnedCol = null;
                _kvCol = null;
                _storage = null;
                RaiseNotification(
                    "CtrlCV",
                    "Persistence is disabled for this session (database unavailable).");
            }
        }

        private void OpenCore()
        {
            var connStr = new ConnectionString
            {
                Filename = _dbPath,
                Connection = ConnectionType.Direct
            };

            _db = new LiteDatabase(connStr);
            _pinnedCol = _db.GetCollection<PersistedClipboardItem>(PinnedCollection);
            _pinnedCol.EnsureIndex(x => x.OrderIndex);
            _kvCol = _db.GetCollection<BsonDocument>(KvCollection);
            _storage = _db.GetStorage<string>();
        }

        private void TryRenameCorrupt()
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    var corrupt = _dbPath + ".corrupt-" + stamp;
                    File.Move(_dbPath, corrupt);
                }

                var log = _dbPath + "-log";
                if (File.Exists(log))
                {
                    try { File.Delete(log); } catch { }
                }
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to rename corrupt DB", ex);
            }
        }

        #endregion

        #region Load

        public List<ClipboardItem> LoadPinnedItems()
        {
            var result = new List<ClipboardItem>();
            if (_db == null || _pinnedCol == null || _storage == null)
                return result;

            try
            {
                var docs = _pinnedCol.Query()
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                foreach (var d in docs)
                {
                    try
                    {
                        var item = RehydrateDoc(d);
                        if (item != null)
                            result.Add(item);
                    }
                    catch (Exception ex)
                    {
                        Form1.LogError("Skipping corrupt persisted item", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to load pinned items", ex);
            }

            return result;
        }

        private ClipboardItem? RehydrateDoc(PersistedClipboardItem d)
        {
            var copiedLocal = DateTime.SpecifyKind(d.CopiedAtUtc, DateTimeKind.Utc).ToLocalTime();

            if (d.ItemType == "Image")
            {
                byte[]? bytes = d.InlineImageBytes;

                if ((bytes == null || bytes.Length == 0) && !string.IsNullOrEmpty(d.ImageFileId))
                {
                    var file = _storage!.FindById(d.ImageFileId);
                    if (file == null)
                        return null;

                    using var ms = new MemoryStream();
                    file.CopyTo(ms);
                    bytes = ms.ToArray();
                }

                if (bytes == null || bytes.Length == 0)
                    return null;

                if (bytes.Length > ClipboardItem.MaxPersistableImageBytes)
                    return null;

                return ClipboardItem.RehydrateImage(bytes, copiedLocal, isPinned: true);
            }

            if (d.ItemType == "Text")
            {
                var text = d.Text ?? string.Empty;
                if (Encoding.UTF8.GetByteCount(text) > ClipboardItem.MaxTextBytes)
                    return null;

                return ClipboardItem.RehydrateText(text, copiedLocal, isPinned: true);
            }

            return null;
        }

        public AppSettings LoadSettings()
        {
            if (_db != null && _kvCol != null)
            {
                try
                {
                    var doc = _kvCol.FindById(SettingsId);
                    if (doc != null && doc.ContainsKey("Json") && doc["Json"].IsString)
                        return DeserializeSettings(doc["Json"].AsString);
                }
                catch (Exception ex)
                {
                    Form1.LogError("Failed to read settings from DB", ex);
                }

                if (File.Exists(_legacySettingsPath))
                {
                    AppSettings? parsed = null;
                    string? json = null;
                    try
                    {
                        json = File.ReadAllText(_legacySettingsPath);
                        parsed = DeserializeSettings(json);
                    }
                    catch (Exception ex)
                    {
                        Form1.LogError("Failed to read/parse legacy settings", ex);
                    }

                    if (parsed != null && json != null)
                    {
                        try
                        {
                            var migDoc = new BsonDocument
                            {
                                ["_id"] = SettingsId,
                                ["Json"] = json
                            };
                            _kvCol.Upsert(migDoc);

                            try
                            {
                                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                                File.Move(_legacySettingsPath, _legacySettingsPath + ".migrated-" + stamp);
                            }
                            catch (Exception renEx)
                            {
                                Form1.LogError("Settings migrated to DB but failed to rename JSON file", renEx);
                            }
                        }
                        catch (Exception upEx)
                        {
                            Form1.LogError("Failed to upsert migrated settings into DB", upEx);
                        }

                        return parsed;
                    }
                }

                return new AppSettings();
            }

            return LoadSettingsFromLegacyJsonOrDefault();
        }

        private AppSettings LoadSettingsFromLegacyJsonOrDefault()
        {
            try
            {
                if (File.Exists(_legacySettingsPath))
                {
                    var json = File.ReadAllText(_legacySettingsPath);
                    return DeserializeSettings(json);
                }
            }
            catch (Exception ex)
            {
                Form1.LogError("Fallback settings read failed", ex);
            }
            return new AppSettings();
        }

        private static AppSettings DeserializeSettings(string json)
        {
            AppSettings s = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
            s.MaxSlots = Math.Clamp(s.MaxSlots, 1, 10);
            s.WidgetOpacity = Math.Clamp(s.WidgetOpacity, 0.2, 1.0);
            s.WidgetAutoHideDelayMs = Math.Clamp(s.WidgetAutoHideDelayMs, 1000, 10000);
            return s;
        }

        #endregion

        #region Mark dirty

        public void MarkPinnedDirty(IReadOnlyList<ClipboardItem> slots)
        {
            if (_disposed || _db == null)
                return;

            var entries = BuildPinnedSnapshot(slots);
            Volatile.Write(ref _pendingPinned, entries);
            ScheduleDebounce();
        }

        private List<PinnedSnapshotEntry> BuildPinnedSnapshot(IReadOnlyList<ClipboardItem> slots)
        {
            var entries = new List<PinnedSnapshotEntry>();
            int order = 0;
            foreach (var s in slots)
            {
                if (!s.IsPinned)
                    continue;

                if (s.ItemType == ClipboardItemType.Text)
                {
                    var text = s.Text ?? string.Empty;
                    if (Encoding.UTF8.GetByteCount(text) > ClipboardItem.MaxTextBytes)
                    {
                        NotifyTextTooLargeOnce();
                        continue;
                    }

                    entries.Add(new PinnedSnapshotEntry
                    {
                        OrderIndex = order++,
                        IsImage = false,
                        Text = text,
                        CopiedAtUtc = s.CopiedAt.ToUniversalTime()
                    });
                }
                else
                {
                    var png = s.GetOrEncodePng();
                    if (png == null || png.Length == 0)
                        continue;

                    if (png.Length > ClipboardItem.MaxPersistableImageBytes)
                    {
                        NotifyImageTooLargeOnce();
                        continue;
                    }

                    entries.Add(new PinnedSnapshotEntry
                    {
                        OrderIndex = order++,
                        IsImage = true,
                        ImagePngBytes = png,
                        CopiedAtUtc = s.CopiedAt.ToUniversalTime()
                    });
                }
            }
            return entries;
        }

        public void MarkSettingsDirty(AppSettings settings)
        {
            if (_disposed || _db == null)
                return;

            try
            {
                var json = JsonSerializer.Serialize(settings, JsonOpts);
                Volatile.Write(ref _pendingSettingsJson, json);
                ScheduleDebounce();
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to serialize settings", ex);
            }
        }

        private void NotifyTextTooLargeOnce()
        {
            lock (_warnGate)
            {
                if (_textTooLargeWarned)
                    return;
                _textTooLargeWarned = true;
            }
            RaiseNotification(
                "Too large to persist",
                "A pinned text exceeds the 5 MB persistence cap and will not survive restart.");
        }

        private void NotifyImageTooLargeOnce()
        {
            lock (_warnGate)
            {
                if (_imageTooLargeWarned)
                    return;
                _imageTooLargeWarned = true;
            }
            RaiseNotification(
                "Too large to persist",
                "A pinned image exceeds the 32 MB persistence cap and will not survive restart.");
        }

        private void ScheduleDebounce()
        {
            if (_disposed)
                return;
            try { _timer.Change(DebounceMs, Timeout.Infinite); }
            catch (ObjectDisposedException) { }
        }

        #endregion

        #region Write

        private void OnTimer(object? _)
        {
            if (_disposed)
                return;

            try { FlushNow(); }
            catch (Exception ex) { Form1.LogError("Debounced flush failed", ex); }
        }

        public void FlushNow()
        {
            var pinned = Interlocked.Exchange(ref _pendingPinned, null);
            var settingsJson = Interlocked.Exchange(ref _pendingSettingsJson, null);
            if (pinned == null && settingsJson == null)
                return;

            lock (_writeGate)
            {
                var db = _db;
                if (db == null)
                    return;

                try
                {
                    db.BeginTrans();
                    if (pinned != null)
                        WritePinnedInternal(pinned);
                    if (settingsJson != null)
                        WriteSettingsInternal(settingsJson);
                    db.Commit();
                }
                catch (Exception ex)
                {
                    try { db.Rollback(); } catch { }
                    Form1.LogError("DB write failed", ex);
                }
            }
        }

        private void WritePinnedInternal(List<PinnedSnapshotEntry> entries)
        {
            if (_pinnedCol == null || _storage == null)
                return;

            foreach (var existing in _pinnedCol.FindAll())
            {
                if (!string.IsNullOrEmpty(existing.ImageFileId))
                {
                    try { _storage.Delete(existing.ImageFileId); } catch { }
                }
            }
            _pinnedCol.DeleteAll();

            if (entries.Count == 0)
                return;

            var docs = new List<PersistedClipboardItem>(entries.Count);
            foreach (var e in entries)
            {
                var doc = new PersistedClipboardItem
                {
                    OrderIndex = e.OrderIndex,
                    ItemType = e.IsImage ? "Image" : "Text",
                    Text = e.IsImage ? null : e.Text,
                    CopiedAtUtc = e.CopiedAtUtc
                };

                if (e.IsImage && e.ImagePngBytes != null)
                {
                    if (e.ImagePngBytes.Length < ClipboardItem.InlineImageByteThreshold)
                    {
                        doc.InlineImageBytes = e.ImagePngBytes;
                    }
                    else
                    {
                        var fileId = "img_" + ObjectId.NewObjectId();
                        using var ms = new MemoryStream(e.ImagePngBytes, writable: false);
                        _storage.Upload(fileId, fileId + ".png", ms);
                        doc.ImageFileId = fileId;
                    }
                }

                docs.Add(doc);
            }

            _pinnedCol.InsertBulk(docs);
        }

        private void WriteSettingsInternal(string json)
        {
            if (_kvCol == null)
                return;

            var doc = new BsonDocument
            {
                ["_id"] = SettingsId,
                ["Json"] = json
            };
            _kvCol.Upsert(doc);
        }

        #endregion

        #region Wipe

        public void WipePinned()
        {
            if (_db == null || _pinnedCol == null || _storage == null)
                return;

            lock (_writeGate)
            {
                try
                {
                    _db.BeginTrans();
                    foreach (var existing in _pinnedCol.FindAll())
                    {
                        if (!string.IsNullOrEmpty(existing.ImageFileId))
                        {
                            try { _storage.Delete(existing.ImageFileId); } catch { }
                        }
                    }
                    _pinnedCol.DeleteAll();
                    _db.Commit();

                    Interlocked.Exchange(ref _pendingPinned, null);

                    try { _db.Checkpoint(); } catch { }
                    try { _db.Rebuild(); } catch { }
                }
                catch (Exception ex)
                {
                    try { _db.Rollback(); } catch { }
                    Form1.LogError("WipePinned failed", ex);
                }
            }
        }

        public void WipeSettings()
        {
            if (_db == null || _kvCol == null)
                return;

            lock (_writeGate)
            {
                try
                {
                    _kvCol.Delete(SettingsId);
                    Interlocked.Exchange(ref _pendingSettingsJson, null);
                }
                catch (Exception ex)
                {
                    Form1.LogError("WipeSettings failed", ex);
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                using var waitHandle = new ManualResetEvent(false);
                _timer.Dispose(waitHandle);
                waitHandle.WaitOne(TimeSpan.FromSeconds(2));
            }
            catch { }

            try { FlushNow(); } catch { }

            lock (_writeGate)
            {
                try { _db?.Checkpoint(); } catch { }
                try { _db?.Dispose(); } catch { }
                _db = null;
                _pinnedCol = null;
                _kvCol = null;
                _storage = null;
            }
        }

        private sealed class PinnedSnapshotEntry
        {
            public int OrderIndex;
            public bool IsImage;
            public string? Text;
            public byte[]? ImagePngBytes;
            public DateTime CopiedAtUtc;
        }
    }
}
