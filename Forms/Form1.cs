using System.Diagnostics;
using System.Runtime.InteropServices;
using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    public partial class Form1 : Form
    {
        private AppSettings _settings;
        private CtrlCvStore _store = null!;
        private ClipboardManager _clipboardManager = null!;
        private HotkeyManager _hotkeyManager = null!;
        private PasteService _pasteService = null!;
        private FloatingWidgetForm? _widget;
        private bool _isExiting;
        private readonly Queue<(string Title, string Message)> _pendingEarlyNotifications = new();

        public Form1()
        {
            _store = new CtrlCvStore(AppSettings.SettingsDir);
            _store.NotificationRequested += OnEarlyStoreNotification;
            AppSettings.AttachStore(_store);
            _settings = AppSettings.Load();
            InitializeComponent();
            LoadAppIcon();
            menuShow.Font = new Font(menuShow.Font, FontStyle.Bold);
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
            listViewSlots.Resize += (_, _) => AutoSizeColumns();

            var tip = new ToolTip { AutoPopDelay = 6000, InitialDelay = 500, ReshowDelay = 200 };
            tip.SetToolTip(btnClearAll, "Removes all unpinned items. Pinned items are kept.\nTo delete a pinned item, right-click it \u2192 Remove.");
            tip.SetToolTip(btnRemoveSelected, "Removes the currently selected item (works for both pinned and unpinned).");
        }

        private void LoadAppIcon()
        {
            try
            {
                var stream = typeof(Form1).Assembly.GetManifestResourceStream("CtrlCV.Logo.ico");
                if (stream != null)
                {
                    var appIcon = new Icon(stream);
                    Icon = appIcon;
                    notifyIcon.Icon = appIcon;
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to load app icon", ex);
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            Text = $"CtrlCV - Clipboard Manager v{versionStr}";
            notifyIcon.Text = $"CtrlCV v{versionStr}";

            _store.NotificationRequested -= OnEarlyStoreNotification;

            _clipboardManager = new ClipboardManager(Handle, _settings, _store);
            _clipboardManager.SlotsChanged += OnSlotsChanged;
            _clipboardManager.NotificationRequested += ShowTrayNotification;

            while (_pendingEarlyNotifications.Count > 0)
            {
                var n = _pendingEarlyNotifications.Dequeue();
                ShowTrayNotification(n.Title, n.Message);
            }

            _clipboardManager.NotifyChanged();

            if (!_clipboardManager.StartListening())
            {
                MessageBox.Show(
                    "Failed to start clipboard monitoring. The application cannot function.\n\n" +
                    $"Error code: {Marshal.GetLastWin32Error()}",
                    "CtrlCV - Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _isExiting = true;
                Application.Exit();
                return;
            }

            _hotkeyManager = new HotkeyManager(Handle);
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            _hotkeyManager.RegisterAll(_settings);

            _pasteService = new PasteService(_clipboardManager);
            _pasteService.NotificationRequested += ShowTrayNotification;

            UpdateStatusLabel();

            if (_settings.WidgetEnabled)
                ShowWidget();

            if (_settings.StartMinimized)
            {
                Hide();
                WindowState = FormWindowState.Minimized;
            }
        }

        private void ShowWidget()
        {
            if (_widget != null && !_widget.IsDisposed)
                return;

            _widget = new FloatingWidgetForm(_clipboardManager, _pasteService, _settings);
            _widget.Owner = this;
            _widget.Show();
        }

        private void HideWidget()
        {
            if (_widget == null || _widget.IsDisposed)
            {
                _widget = null;
                return;
            }

            _widget.Close();
            _widget.Dispose();
            _widget = null;
        }

        #region WndProc

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CLIPBOARDUPDATE:
                    _clipboardManager?.HandleClipboardUpdate();
                    break;

                case WM_HOTKEY:
                    _hotkeyManager?.HandleHotkeyMessage(m.WParam.ToInt32());
                    break;
            }

            base.WndProc(ref m);
        }

        #endregion

        #region Event Handlers

        private void OnSlotsChanged()
        {
            RefreshListView();
            if (_widget != null && !_widget.IsDisposed)
                _widget.RefreshSlots();
        }

        private void OnHotkeyPressed(int id)
        {
            if (id == HotkeyManager.SCREENSHOT_HOTKEY_ID)
            {
                ShowScreenshotMenu();
                return;
            }

            if (id >= 0 && id < _clipboardManager.Slots.Count)
            {
                _ = _pasteService.PasteFromSlotAsync(id);
            }
        }

        #endregion

        #region Screenshot

        private void ShowScreenshotMenu()
        {
            var pos = Cursor.Position;
            screenshotMenu.Show(pos);
        }

        private void MenuFullScreen_Click(object? sender, EventArgs e)
        {
            try
            {
                var bmp = ScreenshotHelper.CaptureFullScreen();
                if (bmp != null)
                {
                    StoreScreenshot(bmp);
                }
                else
                {
                    ShowTrayNotification("Screenshot failed", "Could not capture the screen.");
                }
            }
            catch (Exception ex)
            {
                LogError("Full screen capture error", ex);
                ShowTrayNotification("Screenshot failed", "An error occurred during capture.");
            }
        }

        private void MenuActiveWindow_Click(object? sender, EventArgs e)
        {
            try
            {
                var bmp = ScreenshotHelper.CaptureActiveWindow();
                if (bmp != null)
                {
                    StoreScreenshot(bmp);
                }
                else
                {
                    ShowTrayNotification("Screenshot failed", "Could not capture the active window. It may be minimized or unavailable.");
                }
            }
            catch (Exception ex)
            {
                LogError("Active window capture error", ex);
                ShowTrayNotification("Screenshot failed", "An error occurred during capture.");
            }
        }

        private void MenuSelectRegion_Click(object? sender, EventArgs e)
        {
            try
            {
                using var overlay = new ScreenshotOverlayForm();
                if (overlay.ShowDialog() == DialogResult.OK && overlay.SelectedRegion.Width > 0 && overlay.SelectedRegion.Height > 0)
                {
                    var bmp = ScreenshotHelper.CaptureRegion(overlay.SelectedRegion);
                    if (bmp != null)
                    {
                        StoreScreenshot(bmp);
                    }
                    else
                    {
                        ShowTrayNotification("Screenshot failed", "Could not capture the selected region.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Region capture error", ex);
                ShowTrayNotification("Screenshot failed", "An error occurred during capture.");
            }
        }

        private void StoreScreenshot(Bitmap bmp)
        {
            _clipboardManager.SetSuppressMonitoring(true);
            try
            {
                var item = new ClipboardItem(bmp);
                _clipboardManager.AddSlot(item);

                ClipboardManager.ClipboardRetry(() =>
                {
                    Clipboard.SetImage(bmp);
                    return true;
                });
            }
            finally
            {
                bmp.Dispose();
                BeginInvoke(() => _clipboardManager.SetSuppressMonitoring(false));
            }
        }

        #endregion

        #region ListView UI

        private void RefreshListView()
        {
            listViewSlots.BeginUpdate();
            try
            {
                listViewSlots.Items.Clear();
                imageListThumbs.Images.Clear();

                int thumbIndex = 0;
                var slots = _clipboardManager.Slots;
                var modName = _settings.GetPasteModifierDisplayName();

                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    int slotNumber = i + 1;
                    string displayNumber = slotNumber == 10 ? "0" : slotNumber.ToString();

                    string pinPrefix = slot.IsPinned ? "\U0001F4CC " : "";
                    var lvi = new ListViewItem($"{pinPrefix}{modName}+{displayNumber}");
                    lvi.SubItems.Add(slot.ItemType.ToString());
                    lvi.SubItems.Add(slot.GetPreview());

                    if (slot.ItemType == ClipboardItemType.Image)
                    {
                        using var thumb = slot.CreateThumbnail(32, 32);
                        if (thumb != null)
                        {
                            imageListThumbs.Images.Add(thumb);
                            lvi.ImageIndex = thumbIndex;
                            thumbIndex++;
                        }
                    }

                    listViewSlots.Items.Add(lvi);
                }

                AutoSizeColumns();
            }
            finally
            {
                listViewSlots.EndUpdate();
            }

            UpdateStatusLabel();
        }

        private void AutoSizeColumns()
        {
            colSlot.Width = -1;
            colType.Width = -1;
            int remaining = listViewSlots.ClientSize.Width - colSlot.Width - colType.Width;
            colPreview.Width = Math.Max(remaining, 100);
        }

        private void UpdateStatusLabel()
        {
            int count = _clipboardManager?.Slots.Count ?? 0;
            int max = _settings.MaxSlots;
            if (count >= max)
                lblStatus.Text = $"Monitoring... ({count}/{max} slots used \u2014 oldest will be replaced on next copy)";
            else
                lblStatus.Text = $"Monitoring... ({count}/{max} slots used)";
        }

        #endregion

        #region Button / Menu Handlers

        private void BtnClearAll_Click(object? sender, EventArgs e)
        {
            _clipboardManager.ClearAll();
        }

        private void BtnRemoveSelected_Click(object? sender, EventArgs e)
        {
            RemoveSelectedSlot();
        }

        private void MenuSlotRemove_Click(object? sender, EventArgs e)
        {
            RemoveSelectedSlot();
        }

        private void MenuSlotPin_Click(object? sender, EventArgs e)
        {
            if (listViewSlots.SelectedIndices.Count == 0)
                return;

            var slots = _clipboardManager.Slots;
            bool anyUnpinned = false;
            foreach (int idx in listViewSlots.SelectedIndices)
            {
                if (idx < slots.Count && !slots[idx].IsPinned)
                {
                    anyUnpinned = true;
                    break;
                }
            }

            foreach (int idx in listViewSlots.SelectedIndices)
            {
                if (idx < slots.Count)
                    _clipboardManager.SetPinned(idx, anyUnpinned);
            }
        }

        private void ContextMenuSlot_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            int count = listViewSlots.SelectedIndices.Count;
            bool hasSelection = count > 0;
            menuSlotPin.Enabled = hasSelection;
            menuSlotRemove.Enabled = hasSelection;
            menuSlotRemove.Text = count > 1 ? $"Remove ({count})" : "Remove";

            if (hasSelection)
            {
                var slots = _clipboardManager.Slots;
                bool allPinned = true;
                foreach (int idx in listViewSlots.SelectedIndices)
                {
                    if (idx < slots.Count && !slots[idx].IsPinned)
                    {
                        allPinned = false;
                        break;
                    }
                }
                menuSlotPin.Text = allPinned ? "Unpin" : "Pin";
            }
            else
            {
                menuSlotPin.Text = "Pin";
            }
        }

        private void RemoveSelectedSlot()
        {
            if (listViewSlots.SelectedIndices.Count == 0)
                return;

            int firstIndex = listViewSlots.SelectedIndices[0];

            var indices = new List<int>();
            foreach (int idx in listViewSlots.SelectedIndices)
                indices.Add(idx);

            _clipboardManager.RemoveSlots(indices);

            if (listViewSlots.Items.Count > 0)
            {
                int newIndex = Math.Min(firstIndex, listViewSlots.Items.Count - 1);
                listViewSlots.Items[newIndex].Selected = true;
            }
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            OpenSettings();
        }

        private void OpenSettings()
        {
            bool wasWidgetEnabled = _settings.WidgetEnabled;
            using var form = new SettingsForm(_settings, ForgetPersistedPins, ResetSettingsToDefaults);
            if (form.ShowDialog(this) == DialogResult.OK && form.SettingsChanged)
            {
                _hotkeyManager.UnregisterAll();
                _hotkeyManager.RegisterAll(_settings);
                _clipboardManager.TrimToMaxSlots();

                if (_settings.WidgetEnabled && !wasWidgetEnabled)
                    ShowWidget();
                else if (!_settings.WidgetEnabled && wasWidgetEnabled)
                    HideWidget();
                else if (_widget != null && !_widget.IsDisposed)
                    _widget.ApplySettings();
            }
        }

        private async void MenuCheckForUpdates_Click(object? sender, EventArgs e)
        {
            menuCheckForUpdates.Enabled = false;
            menuCheckForUpdates.Text = "Checking...";
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var currentVersion = version != null
                    ? $"{version.Major}.{version.Minor}.{version.Build}"
                    : "0.0.0";

                var result = await UpdateChecker.CheckForUpdateAsync(currentVersion);

                if (!result.IsUpdateAvailable)
                {
                    MessageBox.Show(
                        $"You are running the latest version (v{currentVersion}).",
                        "CtrlCV - No Updates",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var notes = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                    ? ""
                    : $"\n\nRelease notes:\n{result.ReleaseNotes}";

                var confirmDownload = MessageBox.Show(
                    $"A new version is available: v{result.LatestVersion}\n" +
                    $"You are running v{currentVersion}.{notes}\n\n" +
                    "Do you want to download and install the update?",
                    "CtrlCV - Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmDownload != DialogResult.Yes)
                    return;

                if (string.IsNullOrEmpty(result.DownloadUrl))
                {
                    Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
                    return;
                }

                menuCheckForUpdates.Text = "Downloading...";

                var currentExePath = Environment.ProcessPath
                    ?? Path.Combine(AppContext.BaseDirectory, "CtrlCV.exe");
                var updateFilePath = currentExePath + ".update";

                var progress = new Progress<int>(pct =>
                {
                    if (pct < 100)
                        menuCheckForUpdates.Text = $"Downloading... {pct}%";
                    else
                        menuCheckForUpdates.Text = "Download complete";
                });

                await UpdateChecker.DownloadUpdateAsync(result.DownloadUrl, updateFilePath, progress);

                var confirmRestart = MessageBox.Show(
                    "Update downloaded successfully.\n\nRestart now to apply the update?",
                    "CtrlCV - Update Ready",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmRestart != DialogResult.Yes)
                {
                    ShowTrayNotification("Update ready",
                        "The update has been downloaded. Restart the app to apply it.");
                    return;
                }

                UpdateChecker.ApplyUpdateAndRestart(updateFilePath, currentExePath);
                _isExiting = true;
                notifyIcon.Visible = false;
                Application.Exit();
            }
            catch (Exception ex)
            {
                LogError("Update check failed", ex);
                MessageBox.Show(
                    "Could not check for updates. Please check your internet connection and try again.\n\n" +
                    $"Details: {ex.Message}",
                    "CtrlCV - Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                menuCheckForUpdates.Text = "Check for Updates";
                menuCheckForUpdates.Enabled = true;
            }
        }

        #endregion

        #region Tray Behavior

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_isExiting)
            {
                e.Cancel = true;
                Hide();
                ShowTrayNotification("CtrlCV minimized", "The app is still running in the system tray.\nDouble-click the tray icon to restore.");
                return;
            }

            CleanupResources();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void MenuShow_Click(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void MenuToggleWidget_Click(object? sender, EventArgs e)
        {
            if (_widget != null && !_widget.IsDisposed)
            {
                HideWidget();
                _settings.WidgetEnabled = false;
            }
            else
            {
                ShowWidget();
                _settings.WidgetEnabled = true;
            }
            try { _settings.Save(); }
            catch { }
        }

        private void MenuExit_Click(object? sender, EventArgs e)
        {
            _isExiting = true;
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        #endregion

        #region Cleanup

        private void CleanupResources()
        {
            try
            {
                _widget = null;
                _hotkeyManager?.Dispose();
                _clipboardManager?.Dispose();
                _store?.Dispose();
                notifyIcon.Visible = false;
            }
            catch (Exception ex)
            {
                LogError("Cleanup error", ex);
            }
        }

        #endregion

        #region Notifications and Logging

        private void OnEarlyStoreNotification(string title, string message)
        {
            _pendingEarlyNotifications.Enqueue((title, message));
        }

        internal void ForgetPersistedPins()
        {
            _clipboardManager?.ForgetPersistedPins();
        }

        internal void ResetSettingsToDefaults()
        {
            _settings.ResetToDefaults();
            _store?.MarkSettingsDirty(_settings);
            StartupRegistry.TrySet(_settings.RunAtStartup, out _);
        }

        private void ShowTrayNotification(string title, string message)
        {
            try
            {
                notifyIcon.BalloonTipTitle = title;
                notifyIcon.BalloonTipText = message;
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon.ShowBalloonTip(3000);
            }
            catch (Exception ex)
            {
                LogError("Notification error", ex);
            }
        }

        internal static void LogError(string context, Exception ex)
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "ctrlcv_error.log");
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}\n";
                File.AppendAllText(logPath, entry);
            }
            catch
            {
            }
        }

        #endregion
    }
}
