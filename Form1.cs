using System.Runtime.InteropServices;
using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    public partial class Form1 : Form
    {
        private readonly List<ClipboardItem> _slots = new();
        private AppSettings _settings;
        private bool _isPasting;
        private bool _isProcessingClipboard;
        private string? _lastClipboardText;
        private bool _clipboardListenerRegistered;
        private readonly List<int> _registeredHotkeyIds = new();
        private bool _isExiting;

        public Form1()
        {
            _settings = AppSettings.Load();
            InitializeComponent();
            LoadAppIcon();
            menuShow.Font = new Font(menuShow.Font, FontStyle.Bold);
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
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
            if (!AddClipboardFormatListener(Handle))
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
            _clipboardListenerRegistered = true;

            RegisterAllHotkeys();
            UpdateStatusLabel();

            if (_settings.StartMinimized)
            {
                Hide();
                WindowState = FormWindowState.Minimized;
            }
        }

        #region Hotkey Registration

        private void RegisterAllHotkeys()
        {
            var failedKeys = new List<string>();
            var pasteModFlags = _settings.GetPasteModifierFlags();
            var pasteModName = _settings.GetPasteModifierDisplayName();
            int slotsToRegister = Math.Min(_settings.MaxSlots, 10);

            for (int i = 0; i < Math.Min(slotsToRegister, 9); i++)
            {
                uint vk = (uint)(0x31 + i); // VK_1 through VK_9
                int id = HOTKEY_SLOT_BASE + i;
                if (RegisterHotKey(Handle, id, pasteModFlags, vk))
                    _registeredHotkeyIds.Add(id);
                else
                    failedKeys.Add($"{pasteModName}+{i + 1}");
            }

            if (slotsToRegister == 10)
            {
                if (RegisterHotKey(Handle, HOTKEY_SLOT_BASE + 9, pasteModFlags, 0x30))
                    _registeredHotkeyIds.Add(HOTKEY_SLOT_BASE + 9);
                else
                    failedKeys.Add($"{pasteModName}+0");
            }

            var ssModFlags = _settings.GetScreenshotModifierFlags();
            var ssModName = _settings.GetScreenshotModifierDisplayName();
            if (RegisterHotKey(Handle, HOTKEY_SCREENSHOT, ssModFlags, VK_SNAPSHOT))
                _registeredHotkeyIds.Add(HOTKEY_SCREENSHOT);
            else
                failedKeys.Add($"{ssModName}+PrintScreen");

            if (failedKeys.Count > 0)
            {
                MessageBox.Show(
                    "Could not register the following hotkeys (already in use by another app):\n" +
                    string.Join("\n", failedKeys.Select(k => $"  - {k}")) +
                    "\n\nThese shortcuts will not work until the conflict is resolved.",
                    "CtrlCV - Hotkey Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void UnregisterAllHotkeys()
        {
            foreach (var id in _registeredHotkeyIds)
            {
                UnregisterHotKey(Handle, id);
            }
            _registeredHotkeyIds.Clear();
        }

        #endregion

        #region WndProc

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CLIPBOARDUPDATE:
                    OnClipboardUpdate();
                    break;

                case WM_HOTKEY:
                    OnHotKey(m.WParam.ToInt32());
                    break;
            }

            base.WndProc(ref m);
        }

        #endregion

        #region Clipboard Monitoring

        private void OnClipboardUpdate()
        {
            if (_isPasting || _isProcessingClipboard)
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
                LogError("Clipboard monitoring error", ex);
            }
            finally
            {
                _isProcessingClipboard = false;
            }
        }

        private static T? ClipboardRetry<T>(Func<T> action, int maxRetries = 3, int delayMs = 100)
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

        #endregion

        #region Slot Management

        private void AddSlot(ClipboardItem item)
        {
            while (_slots.Count >= _settings.MaxSlots)
            {
                var oldest = _slots[0];
                _slots.RemoveAt(0);
                oldest.Dispose();

                ShowTrayNotification("Clipboard full", "Oldest item replaced to make room.");
            }

            _slots.Add(item);
            RefreshListView();
        }

        private void RemoveSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return;

            var item = _slots[index];
            _slots.RemoveAt(index);
            item.Dispose();
            RefreshListView();
        }

        private void ClearAllSlots()
        {
            foreach (var slot in _slots)
                slot.Dispose();
            _slots.Clear();
            _lastClipboardText = null;
            RefreshListView();
        }

        #endregion

        #region Hotkey Handling

        private void OnHotKey(int hotkeyId)
        {
            if (hotkeyId == HOTKEY_SCREENSHOT)
            {
                ShowScreenshotMenu();
                return;
            }

            int slotIndex = hotkeyId - HOTKEY_SLOT_BASE;
            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                PasteFromSlot(slotIndex);
            }
        }

        #endregion

        #region Paste Simulation

        private async void PasteFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return;

            _isPasting = true;
            try
            {
                var item = _slots[slotIndex];
                bool clipboardSet = false;

                if (item.ItemType == ClipboardItemType.Text && item.Text != null)
                {
                    clipboardSet = ClipboardRetry(() =>
                    {
                        Clipboard.SetText(item.Text);
                        return true;
                    }) == true;
                }
                else if (item.ItemType == ClipboardItemType.Image && item.ImageData != null)
                {
                    clipboardSet = ClipboardRetry(() =>
                    {
                        Clipboard.SetImage(item.ImageData);
                        return true;
                    }) == true;
                }

                if (!clipboardSet)
                {
                    ShowTrayNotification("Paste failed", "Could not access clipboard. It may be in use by another application.");
                    return;
                }

                await Task.Delay(50);

                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V, 0, 0, UIntPtr.Zero);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                LogError("Paste simulation error", ex);
                ShowTrayNotification("Paste failed", "An error occurred during paste.");
            }
            finally
            {
                _isPasting = false;
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
            _isPasting = true;
            try
            {
                var item = new ClipboardItem(bmp);
                AddSlot(item);

                ClipboardRetry(() =>
                {
                    Clipboard.SetImage(bmp);
                    return true;
                });
            }
            finally
            {
                bmp.Dispose();
                BeginInvoke(() => _isPasting = false);
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

                var modName = _settings.GetPasteModifierDisplayName();
                for (int i = 0; i < _slots.Count; i++)
                {
                    var slot = _slots[i];
                    int slotNumber = i + 1;
                    string displayNumber = slotNumber == 10 ? "0" : slotNumber.ToString();

                    var lvi = new ListViewItem($"{modName}+{displayNumber}");
                    lvi.SubItems.Add(slot.ItemType.ToString());
                    lvi.SubItems.Add(slot.GetPreview());

                    if (slot.ItemType == ClipboardItemType.Image)
                    {
                        var thumb = slot.CreateThumbnail(32, 32);
                        if (thumb != null)
                        {
                            imageListThumbs.Images.Add(thumb);
                            lvi.ImageIndex = thumbIndex;
                            thumbIndex++;
                        }
                    }

                    listViewSlots.Items.Add(lvi);
                }

                if (colPreview.Width != -2)
                    colPreview.Width = -2;
            }
            finally
            {
                listViewSlots.EndUpdate();
            }

            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            int count = _slots.Count;
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
            ClearAllSlots();
        }

        private void BtnRemoveSelected_Click(object? sender, EventArgs e)
        {
            if (listViewSlots.SelectedIndices.Count == 0)
                return;

            int selectedIndex = listViewSlots.SelectedIndices[0];
            RemoveSlot(selectedIndex);
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            OpenSettings();
        }

        private void OpenSettings()
        {
            using var form = new SettingsForm(_settings);
            if (form.ShowDialog(this) == DialogResult.OK && form.SettingsChanged)
            {
                UnregisterAllHotkeys();
                RegisterAllHotkeys();

                while (_slots.Count > _settings.MaxSlots)
                {
                    var oldest = _slots[0];
                    _slots.RemoveAt(0);
                    oldest.Dispose();
                }

                RefreshListView();
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
                if (_clipboardListenerRegistered)
                {
                    RemoveClipboardFormatListener(Handle);
                    _clipboardListenerRegistered = false;
                }

                UnregisterAllHotkeys();

                foreach (var slot in _slots)
                    slot.Dispose();
                _slots.Clear();

                notifyIcon.Visible = false;
            }
            catch (Exception ex)
            {
                LogError("Cleanup error", ex);
            }
        }

        #endregion

        #region Notifications and Logging

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
                // Last resort: can't even write to log
            }
        }

        #endregion
    }
}
