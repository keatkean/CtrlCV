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

        private void InitializeSettingsUI()
        {
            Text = "CtrlCV - Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(12);

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
        }

        private void BtnDefaults_Click(object? sender, EventArgs e)
        {
            var defaults = new AppSettings();
            cmbPasteModifier.SelectedIndex = (int)defaults.PasteModifier;
            nudMaxSlots.Value = defaults.MaxSlots;
            cmbScreenshotModifier.SelectedIndex = (int)defaults.ScreenshotModifier;
            chkStartMinimized.Checked = defaults.StartMinimized;
            chkRunAtStartup.Checked = defaults.RunAtStartup;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var newPasteModifier = (ModifierOption)cmbPasteModifier.SelectedIndex;
            var newMaxSlots = (int)nudMaxSlots.Value;
            var newScreenshotModifier = (ModifierOption)cmbScreenshotModifier.SelectedIndex;
            var newStartMinimized = chkStartMinimized.Checked;
            var newRunAtStartup = chkRunAtStartup.Checked;

            SettingsChanged =
                newPasteModifier != _settings.PasteModifier ||
                newMaxSlots != _settings.MaxSlots ||
                newScreenshotModifier != _settings.ScreenshotModifier ||
                newStartMinimized != _settings.StartMinimized ||
                newRunAtStartup != _settings.RunAtStartup;

            _settings.PasteModifier = newPasteModifier;
            _settings.MaxSlots = newMaxSlots;
            _settings.ScreenshotModifier = newScreenshotModifier;
            _settings.StartMinimized = newStartMinimized;
            _settings.RunAtStartup = newRunAtStartup;

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
