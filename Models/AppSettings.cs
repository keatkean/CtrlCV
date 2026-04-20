using System.Text.Json;

namespace CtrlCV
{
    public enum ModifierOption
    {
        Ctrl,
        CtrlAlt,
        CtrlShift
    }

    public class AppSettings
    {
        public ModifierOption PasteModifier { get; set; } = ModifierOption.Ctrl;
        public int MaxSlots { get; set; } = 10;
        public ModifierOption ScreenshotModifier { get; set; } = ModifierOption.CtrlAlt;
        public bool StartMinimized { get; set; } = false;
        public bool RunAtStartup { get; set; } = false;
        public bool AutoExtractTextFromScreenshots { get; set; } = false;

        public bool WidgetEnabled { get; set; } = false;
        public bool WidgetCompactMode { get; set; } = false;
        public double WidgetOpacity { get; set; } = 0.85;
        public bool WidgetAutoHide { get; set; } = true;
        public int WidgetAutoHideDelayMs { get; set; } = 3000;
        public int WidgetLeft { get; set; } = -1;
        public int WidgetTop { get; set; } = -1;
        public bool WidgetVertical { get; set; } = false;

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CtrlCV");

        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public uint GetPasteModifierFlags()
        {
            return PasteModifier switch
            {
                ModifierOption.CtrlAlt => NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
                ModifierOption.CtrlShift => NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT,
                _ => NativeMethods.MOD_CONTROL
            };
        }

        public uint GetScreenshotModifierFlags()
        {
            return ScreenshotModifier switch
            {
                ModifierOption.Ctrl => NativeMethods.MOD_CONTROL,
                ModifierOption.CtrlAlt => NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
                _ => NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT
            };
        }

        public string GetPasteModifierDisplayName()
        {
            return PasteModifier switch
            {
                ModifierOption.CtrlAlt => "Ctrl+Alt",
                ModifierOption.CtrlShift => "Ctrl+Shift",
                _ => "Ctrl"
            };
        }

        public string GetScreenshotModifierDisplayName()
        {
            return ScreenshotModifier switch
            {
                ModifierOption.Ctrl => "Ctrl",
                ModifierOption.CtrlAlt => "Ctrl+Alt",
                _ => "Ctrl+Shift"
            };
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        settings.MaxSlots = Math.Clamp(settings.MaxSlots, 1, 10);
                        settings.WidgetOpacity = Math.Clamp(settings.WidgetOpacity, 0.2, 1.0);
                        settings.WidgetAutoHideDelayMs = Math.Clamp(settings.WidgetAutoHideDelayMs, 1000, 10000);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to load settings", ex);
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to save settings", ex);
                throw;
            }
        }
    }
}
