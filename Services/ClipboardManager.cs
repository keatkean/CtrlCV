using System.Runtime.InteropServices;
using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    internal class ClipboardManager : IDisposable
    {
        private readonly List<ClipboardItem> _slots = new();
        private readonly IntPtr _hwnd;
        private readonly AppSettings _settings;
        private readonly CtrlCvStore? _store;
        private bool _isProcessingClipboard;
        private string? _lastClipboardText;
        private bool _clipboardListenerRegistered;
        private bool _suppressMonitoring;
        private bool _disposed;

        public IReadOnlyList<ClipboardItem> Slots => _slots;
        public event Action? SlotsChanged;
        public event Action<string, string>? NotificationRequested;

        public ClipboardManager(IntPtr windowHandle, AppSettings settings, CtrlCvStore? store = null)
        {
            _hwnd = windowHandle;
            _settings = settings;
            _store = store;

            if (_store != null)
            {
                _store.NotificationRequested += OnStoreNotification;

                var persisted = _store.LoadPinnedItems();
                foreach (var item in persisted)
                {
                    if (_slots.Count >= _settings.MaxSlots)
                    {
                        item.Dispose();
                        continue;
                    }
                    _slots.Add(item);
                }
            }
        }

        private void OnStoreNotification(string title, string body)
        {
            NotificationRequested?.Invoke(title, body);
        }

        public bool StartListening()
        {
            if (_clipboardListenerRegistered)
                return true;

            if (!AddClipboardFormatListener(_hwnd))
                return false;

            _clipboardListenerRegistered = true;
            return true;
        }

        public void StopListening()
        {
            if (_clipboardListenerRegistered)
            {
                RemoveClipboardFormatListener(_hwnd);
                _clipboardListenerRegistered = false;
            }
        }

        public void SetSuppressMonitoring(bool suppress)
        {
            _suppressMonitoring = suppress;
        }

        public void HandleClipboardUpdate()
        {
            if (_suppressMonitoring || _isProcessingClipboard)
                return;

            _isProcessingClipboard = true;
            try
            {
                if (ClipboardRetry(() => Clipboard.ContainsImage()) == true)
                {
                    var img = ClipboardRetry(() => Clipboard.GetImage());
                    if (img != null)
                    {
                        AddSlot(new ClipboardItem(img));
                        img.Dispose();
                        return;
                    }
                }

                if (ClipboardRetry(() => Clipboard.ContainsText()) == true)
                {
                    var text = ClipboardRetry(() => Clipboard.GetText());
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (text == _lastClipboardText)
                            return;

                        _lastClipboardText = text;
                        AddSlot(new ClipboardItem(text));
                    }
                }
            }
            catch (Exception ex)
            {
                Form1.LogError("Clipboard monitoring error", ex);
            }
            finally
            {
                _isProcessingClipboard = false;
            }
        }

        public void AddSlot(ClipboardItem item)
        {
            while (_slots.Count >= _settings.MaxSlots)
            {
                int evictIndex = _slots.FindIndex(s => !s.IsPinned);
                if (evictIndex < 0)
                {
                    item.Dispose();
                    NotificationRequested?.Invoke("Clipboard full", "All slots are pinned. Unpin or remove an item first.");
                    return;
                }

                _slots[evictIndex].Dispose();
                _slots.RemoveAt(evictIndex);
                NotificationRequested?.Invoke("Clipboard full", "Oldest unpinned item replaced to make room.");
            }

            _slots.Add(item);
            SlotsChanged?.Invoke();
            if (item.IsPinned)
                MarkPinnedDirty();
        }

        public void RemoveSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return;

            var item = _slots[index];
            bool wasPinned = item.IsPinned;
            _slots.RemoveAt(index);
            item.Dispose();
            SlotsChanged?.Invoke();
            if (wasPinned)
                MarkPinnedDirty();
        }

        public void RemoveSlots(IList<int> indices)
        {
            var sorted = indices.OrderByDescending(i => i).ToList();
            bool pinnedAffected = false;
            foreach (int idx in sorted)
            {
                if (idx >= 0 && idx < _slots.Count)
                {
                    if (_slots[idx].IsPinned)
                        pinnedAffected = true;
                    _slots[idx].Dispose();
                    _slots.RemoveAt(idx);
                }
            }
            SlotsChanged?.Invoke();
            if (pinnedAffected)
                MarkPinnedDirty();
        }

        public void ClearAll()
        {
            int removed = 0;
            int keptPinned = 0;
            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                if (_slots[i].IsPinned)
                {
                    keptPinned++;
                    continue;
                }
                _slots[i].Dispose();
                _slots.RemoveAt(i);
                removed++;
            }

            _lastClipboardText = null;

            if (removed == 0)
            {
                if (keptPinned > 0)
                {
                    NotificationRequested?.Invoke(
                        "Nothing to clear",
                        "All items are pinned. Use right-click \u2192 Remove, or Settings \u2192 Forget Persisted Pins.");
                }
                return;
            }

            SlotsChanged?.Invoke();

            if (keptPinned > 0)
            {
                NotificationRequested?.Invoke(
                    "Cleared unpinned items",
                    $"{keptPinned} pinned item{(keptPinned == 1 ? " was" : "s were")} kept. Use right-click \u2192 Remove to delete them.");
            }
        }

        public void TrimToMaxSlots()
        {
            bool pinnedAffected = false;
            while (_slots.Count > _settings.MaxSlots)
            {
                var oldest = _slots[0];
                if (oldest.IsPinned)
                    pinnedAffected = true;
                _slots.RemoveAt(0);
                oldest.Dispose();
            }
            SlotsChanged?.Invoke();
            if (pinnedAffected)
                MarkPinnedDirty();
        }

        public void TogglePin(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return;
            _slots[index].IsPinned = !_slots[index].IsPinned;
            SlotsChanged?.Invoke();
            MarkPinnedDirty();
        }

        public void SetPinned(int index, bool pinned)
        {
            if (index < 0 || index >= _slots.Count)
                return;
            if (_slots[index].IsPinned == pinned)
                return;
            _slots[index].IsPinned = pinned;
            SlotsChanged?.Invoke();
            MarkPinnedDirty();
        }

        public void NotifyChanged()
        {
            SlotsChanged?.Invoke();
        }

        private void MarkPinnedDirty()
        {
            _store?.MarkPinnedDirty(_slots);
        }

        public void ForgetPersistedPins()
        {
            bool anyUnpinned = false;
            foreach (var s in _slots)
            {
                if (s.IsPinned)
                {
                    s.IsPinned = false;
                    anyUnpinned = true;
                }
            }

            _store?.WipePinned();

            if (anyUnpinned)
                SlotsChanged?.Invoke();
        }

        public static T? ClipboardRetry<T>(Func<T> action, int maxRetries = 3, int delayMs = 100)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return action();
                }
                catch (ExternalException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(delayMs);
                }
                catch (ExternalException)
                {
                    return default;
                }
            }
            return default;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            StopListening();

            if (_store != null)
            {
                try { _store.FlushNow(); } catch { }
                _store.NotificationRequested -= OnStoreNotification;
            }

            foreach (var slot in _slots)
                slot.Dispose();
            _slots.Clear();
        }
    }
}
