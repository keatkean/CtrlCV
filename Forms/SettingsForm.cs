using Microsoft.Win32;

namespace CtrlCV
{
    internal class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        public bool SettingsChanged { get; private set; }

        private ComboBox cmbPasteModifier = null!;
        private NumericUpDown nudMaxSlots = null!;
        private ComboBox cmbScreenshotModifier = null!;
        private CheckBox chkStartMinimized = null!;
        private CheckBox chkRunAtStartup = null!;
        private CheckBox chkAutoExtractText = null!;
        private CheckBox chkWidgetEnabled = null!;
        private CheckBox chkWidgetCompact = null!;
        private TrackBar trkWidgetOpacity = null!;
        private Label lblOpacityValue = null!;
        private CheckBox chkWidgetAutoHide = null!;
        private NumericUpDown nudAutoHideDelay = null!;
        private ComboBox cmbWidgetOrientation = null!;
        private Button btnResetPosition = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            InitializeSettingsUI();
            LoadAppIcon();
            LoadCurrentValues();
        }

        private void LoadAppIcon()
        {
            try
            {
                var stream = typeof(Form1).Assembly.GetManifestResourceStream("CtrlCV.Logo.ico");
                if (stream != null)
                    Icon = new Icon(stream);
            }
            catch { }
        }

        private int Scale(int baseValue) => (int)Math.Round(baseValue * DeviceDpi / 96.0);

        private void InitializeSettingsUI()
        {
            Text = "CtrlCV - Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(Scale(12));

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            // Paste modifier
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Paste hotkey modifier:"), 0, row);
            cmbPasteModifier = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPasteModifier.Items.AddRange(new object[] { "Ctrl", "Ctrl+Alt", "Ctrl+Shift" });
            table.Controls.Add(cmbPasteModifier, 1, row);
            row++;

            // Max slots
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Maximum slots (1-10):"), 0, row);
            nudMaxSlots = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 10, DecimalPlaces = 0 };
            table.Controls.Add(nudMaxSlots, 1, row);
            row++;

            // Screenshot modifier
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Screenshot hotkey modifier:"), 0, row);
            cmbScreenshotModifier = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbScreenshotModifier.Items.AddRange(new object[] { "Ctrl", "Ctrl+Alt", "Ctrl+Shift" });
            table.Controls.Add(cmbScreenshotModifier, 1, row);
            row++;

            // Hint row
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblHint = new Label
            {
                Text = "Paste: [modifier]+1 ... [modifier]+0\nScreenshot: [modifier]+PrintScreen",
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(3, 6, 3, 6)
            };
            table.SetColumnSpan(lblHint, 2);
            table.Controls.Add(lblHint, 0, row);
            row++;

            // Start minimized
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            chkStartMinimized = new CheckBox { Text = "Start minimized to system tray", AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
            table.SetColumnSpan(chkStartMinimized, 2);
            table.Controls.Add(chkStartMinimized, 0, row);
            row++;

            // Run at startup
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            chkRunAtStartup = new CheckBox { Text = "Run at Windows startup", AutoSize = true, Margin = new Padding(3, 3, 3, 6) };
            table.SetColumnSpan(chkRunAtStartup, 2);
            table.Controls.Add(chkRunAtStartup, 0, row);
            row++;

            // Auto-extract text from screenshots (OCR)
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            chkAutoExtractText = new CheckBox
            {
                Text = "Auto-extract text from screenshots (OCR)",
                AutoSize = true,
                Margin = new Padding(3, 3, 3, 3)
            };
            table.SetColumnSpan(chkAutoExtractText, 2);
            table.Controls.Add(chkAutoExtractText, 0, row);
            row++;

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblAutoExtractHint = new Label
            {
                Text = "When enabled, text is extracted automatically after each screenshot.\nYou can still right-click any image slot to extract text manually.",
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(20, 0, 3, 6)
            };
            table.SetColumnSpan(lblAutoExtractHint, 2);
            table.Controls.Add(lblAutoExtractHint, 0, row);
            row++;

            // Widget section separator
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lblWidgetHeader = new Label
            {
                Text = "Floating Widget",
                AutoSize = true,
                Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
                Margin = new Padding(3, 12, 3, 4)
            };
            table.SetColumnSpan(lblWidgetHeader, 2);
            table.Controls.Add(lblWidgetHeader, 0, row);
            row++;

            // Enable widget
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            chkWidgetEnabled = new CheckBox { Text = "Enable floating widget", AutoSize = true, Margin = new Padding(3, 3, 3, 3) };
            table.SetColumnSpan(chkWidgetEnabled, 2);
            table.Controls.Add(chkWidgetEnabled, 0, row);
            row++;

            // Compact mode
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            chkWidgetCompact = new CheckBox { Text = "Compact mode (small circles)", AutoSize = true, Margin = new Padding(3, 3, 3, 3) };
            table.SetColumnSpan(chkWidgetCompact, 2);
            table.Controls.Add(chkWidgetCompact, 0, row);
            row++;

            // Opacity
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Opacity:"), 0, row);
            var opacityPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
            trkWidgetOpacity = new TrackBar
            {
                Minimum = 20,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 10,
                Width = Scale(160)
            };
            trkWidgetOpacity.ValueChanged += (_, _) =>
            {
                lblOpacityValue.Text = $"{trkWidgetOpacity.Value}%";
            };
            lblOpacityValue = new Label { AutoSize = true, Margin = new Padding(4, 8, 0, 0) };
            opacityPanel.Controls.Add(trkWidgetOpacity);
            opacityPanel.Controls.Add(lblOpacityValue);
            table.Controls.Add(opacityPanel, 1, row);
            row++;

            // Auto-hide
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var autoHidePanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
            chkWidgetAutoHide = new CheckBox { Text = "Auto-hide after", AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            nudAutoHideDelay = new NumericUpDown { Minimum = 1, Maximum = 10, DecimalPlaces = 0, Width = Scale(50) };
            var lblSeconds = new Label { Text = "seconds", AutoSize = true, Margin = new Padding(4, 8, 0, 0) };
            autoHidePanel.Controls.Add(chkWidgetAutoHide);
            autoHidePanel.Controls.Add(nudAutoHideDelay);
            autoHidePanel.Controls.Add(lblSeconds);
            table.SetColumnSpan(autoHidePanel, 2);
            table.Controls.Add(autoHidePanel, 0, row);
            row++;

            // Orientation
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(MakeLabel("Orientation:"), 0, row);
            cmbWidgetOrientation = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbWidgetOrientation.Items.AddRange(new object[] { "Horizontal", "Vertical" });
            table.Controls.Add(cmbWidgetOrientation, 1, row);
            row++;

            // Reset position
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            btnResetPosition = new Button
            {
                Text = "Reset Widget Position",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                Margin = new Padding(3, 6, 3, 6)
            };
            btnResetPosition.Click += BtnResetPosition_Click;
            table.SetColumnSpan(btnResetPosition, 2);
            table.Controls.Add(btnResetPosition, 0, row);
            row++;

            // Button row
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 0)
            };

            btnCancel = new Button { Text = "Cancel", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly, DialogResult = DialogResult.Cancel };
            btnSave = new Button { Text = "Save", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly, Margin = new Padding(0, 0, 8, 0) };
            btnSave.Click += BtnSave_Click;
            var btnDefaults = new Button { Text = "Restore Defaults", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly, Margin = new Padding(0, 0, 8, 0) };
            btnDefaults.Click += BtnDefaults_Click;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnDefaults);

            table.SetColumnSpan(buttonPanel, 2);
            table.Controls.Add(buttonPanel, 0, row);

            Controls.Add(table);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 8, 10, 3)
            };
        }

        private void LoadCurrentValues()
        {
            cmbPasteModifier.SelectedIndex = (int)_settings.PasteModifier;
            nudMaxSlots.Value = _settings.MaxSlots;
            cmbScreenshotModifier.SelectedIndex = (int)_settings.ScreenshotModifier;
            chkStartMinimized.Checked = _settings.StartMinimized;
            chkRunAtStartup.Checked = _settings.RunAtStartup;
            chkAutoExtractText.Checked = _settings.AutoExtractTextFromScreenshots;

            chkWidgetEnabled.Checked = _settings.WidgetEnabled;
            chkWidgetCompact.Checked = _settings.WidgetCompactMode;
            trkWidgetOpacity.Value = (int)(_settings.WidgetOpacity * 100);
            lblOpacityValue.Text = $"{trkWidgetOpacity.Value}%";
            chkWidgetAutoHide.Checked = _settings.WidgetAutoHide;
            nudAutoHideDelay.Value = Math.Clamp(_settings.WidgetAutoHideDelayMs / 1000, 1, 10);
            cmbWidgetOrientation.SelectedIndex = _settings.WidgetVertical ? 1 : 0;
        }

        private void BtnDefaults_Click(object? sender, EventArgs e)
        {
            var defaults = new AppSettings();
            cmbPasteModifier.SelectedIndex = (int)defaults.PasteModifier;
            nudMaxSlots.Value = defaults.MaxSlots;
            cmbScreenshotModifier.SelectedIndex = (int)defaults.ScreenshotModifier;
            chkStartMinimized.Checked = defaults.StartMinimized;
            chkRunAtStartup.Checked = defaults.RunAtStartup;
            chkAutoExtractText.Checked = defaults.AutoExtractTextFromScreenshots;

            chkWidgetEnabled.Checked = defaults.WidgetEnabled;
            chkWidgetCompact.Checked = defaults.WidgetCompactMode;
            trkWidgetOpacity.Value = (int)(defaults.WidgetOpacity * 100);
            chkWidgetAutoHide.Checked = defaults.WidgetAutoHide;
            nudAutoHideDelay.Value = defaults.WidgetAutoHideDelayMs / 1000;
            cmbWidgetOrientation.SelectedIndex = defaults.WidgetVertical ? 1 : 0;
        }

        private void BtnResetPosition_Click(object? sender, EventArgs e)
        {
            _settings.WidgetLeft = -1;
            _settings.WidgetTop = -1;
            MessageBox.Show(
                "Widget position will be reset when the widget is next shown.",
                "CtrlCV",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var newPasteModifier = (ModifierOption)cmbPasteModifier.SelectedIndex;
            var newMaxSlots = (int)nudMaxSlots.Value;
            var newScreenshotModifier = (ModifierOption)cmbScreenshotModifier.SelectedIndex;
            var newStartMinimized = chkStartMinimized.Checked;
            var newRunAtStartup = chkRunAtStartup.Checked;
            var newAutoExtractText = chkAutoExtractText.Checked;
            var newWidgetEnabled = chkWidgetEnabled.Checked;
            var newWidgetCompact = chkWidgetCompact.Checked;
            var newWidgetOpacity = trkWidgetOpacity.Value / 100.0;
            var newWidgetAutoHide = chkWidgetAutoHide.Checked;
            var newWidgetAutoHideDelay = (int)nudAutoHideDelay.Value * 1000;
            var newWidgetVertical = cmbWidgetOrientation.SelectedIndex == 1;

            SettingsChanged =
                newPasteModifier != _settings.PasteModifier ||
                newMaxSlots != _settings.MaxSlots ||
                newScreenshotModifier != _settings.ScreenshotModifier ||
                newStartMinimized != _settings.StartMinimized ||
                newRunAtStartup != _settings.RunAtStartup ||
                newAutoExtractText != _settings.AutoExtractTextFromScreenshots ||
                newWidgetEnabled != _settings.WidgetEnabled ||
                newWidgetCompact != _settings.WidgetCompactMode ||
                Math.Abs(newWidgetOpacity - _settings.WidgetOpacity) > 0.01 ||
                newWidgetAutoHide != _settings.WidgetAutoHide ||
                newWidgetAutoHideDelay != _settings.WidgetAutoHideDelayMs ||
                newWidgetVertical != _settings.WidgetVertical;

            _settings.PasteModifier = newPasteModifier;
            _settings.MaxSlots = newMaxSlots;
            _settings.ScreenshotModifier = newScreenshotModifier;
            _settings.StartMinimized = newStartMinimized;
            _settings.RunAtStartup = newRunAtStartup;
            _settings.AutoExtractTextFromScreenshots = newAutoExtractText;
            _settings.WidgetEnabled = newWidgetEnabled;
            _settings.WidgetCompactMode = newWidgetCompact;
            _settings.WidgetOpacity = newWidgetOpacity;
            _settings.WidgetAutoHide = newWidgetAutoHide;
            _settings.WidgetAutoHideDelayMs = newWidgetAutoHideDelay;
            _settings.WidgetVertical = newWidgetVertical;

            try
            {
                _settings.Save();
                SetRunAtStartup(newRunAtStartup);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save settings:\n\n{ex.Message}",
                    "CtrlCV - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void SetRunAtStartup(bool enabled)
        {
            const string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string valueName = "CtrlCV";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyName, writable: true);
                if (key == null) return;

                if (enabled)
                {
                    var exePath = Application.ExecutablePath;
                    key.SetValue(valueName, $"\"{exePath}\"");
                }
                else
                {
                    if (key.GetValue(valueName) != null)
                        key.DeleteValue(valueName, throwOnMissingValue: false);
                }
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to set startup registry", ex);
                MessageBox.Show(
                    $"Could not update Windows startup setting:\n\n{ex.Message}",
                    "CtrlCV - Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
