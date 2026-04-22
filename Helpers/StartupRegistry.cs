using Microsoft.Win32;

namespace CtrlCV
{
    internal static class StartupRegistry
    {
        private const string RunKeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "CtrlCV";

        public static bool TrySet(bool enabled, out string? error)
        {
            error = null;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyName, writable: true);
                if (key == null)
                {
                    error = "Windows Run registry key is not accessible.";
                    return false;
                }

                if (enabled)
                {
                    var exePath = Application.ExecutablePath;
                    key.SetValue(ValueName, $"\"{exePath}\"");
                }
                else if (key.GetValue(ValueName) != null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }
                return true;
            }
            catch (Exception ex)
            {
                Form1.LogError("Failed to set startup registry", ex);
                error = ex.Message;
                return false;
            }
        }
    }
}
