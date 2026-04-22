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

        public bool WidgetEnabled { get; set; } = false;
        public bool WidgetCompactMode { get; set; } = false;
        public double WidgetOpacity { get; set; } = 0.85;
        public bool WidgetAutoHide { get; set; } = true;
        public int WidgetAutoHideDelayMs { get; set; } = 3000;
        public int WidgetLeft { get; set; } = -1;
        public int WidgetTop { get; set; } = -1;
        public bool WidgetVertical { get; set; } = false;

        internal static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CtrlCV");

        private static readonly string LegacySettingsPath = Path.Combine(SettingsDir, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private static CtrlCvStore? _store;

        internal static void AttachStore(CtrlCvStore store)
        {
            _store = store;
        }

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
            if (_store != null)
                return _store.LoadSettings();

            try
            {
                if (File.Exists(LegacySettingsPath))
                {
                    var json = File.ReadAllText(LegacySettingsPath);
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
            if (_store != null)
            {
                _store.MarkSettingsDirty(this);
                return;
            }

            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(LegacySettingsPath, json);
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to save settings", ex);
                throw;
            }
        }

        public void ResetToDefaults()
        {
            var defaults = new AppSettings();
            PasteModifier = defaults.PasteModifier;
            MaxSlots = defaults.MaxSlots;
            ScreenshotModifier = defaults.ScreenshotModifier;
            StartMinimized = defaults.StartMinimized;
            RunAtStartup = defaults.RunAtStartup;
            WidgetEnabled = defaults.WidgetEnabled;
            WidgetCompactMode = defaults.WidgetCompactMode;
            WidgetOpacity = defaults.WidgetOpacity;
            WidgetAutoHide = defaults.WidgetAutoHide;
            WidgetAutoHideDelayMs = defaults.WidgetAutoHideDelayMs;
            WidgetLeft = defaults.WidgetLeft;
            WidgetTop = defaults.WidgetTop;
            WidgetVertical = defaults.WidgetVertical;
        }
    }
}
