using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    internal class HotkeyManager : IDisposable
    {
        public const int SCREENSHOT_HOTKEY_ID = -1;

        private readonly IntPtr _hwnd;
        private readonly List<int> _registeredHotkeyIds = new();
        private bool _disposed;

        public event Action<int>? HotkeyPressed;

        public HotkeyManager(IntPtr windowHandle)
        {
            _hwnd = windowHandle;
        }

        public void RegisterAll(AppSettings settings)
        {
            var failedKeys = new List<string>();
            var pasteModFlags = settings.GetPasteModifierFlags();
            var pasteModName = settings.GetPasteModifierDisplayName();
            int slotsToRegister = Math.Min(settings.MaxSlots, MAX_SLOTS);

            for (int i = 0; i < Math.Min(slotsToRegister, 9); i++)
            {
                uint vk = (uint)(0x31 + i);
                int id = HOTKEY_SLOT_BASE + i;
                if (RegisterHotKey(_hwnd, id, pasteModFlags, vk))
                    _registeredHotkeyIds.Add(id);
                else
                    failedKeys.Add($"{pasteModName}+{i + 1}");
            }

            if (slotsToRegister == 10)
            {
                if (RegisterHotKey(_hwnd, HOTKEY_SLOT_BASE + 9, pasteModFlags, 0x30))
                    _registeredHotkeyIds.Add(HOTKEY_SLOT_BASE + 9);
                else
                    failedKeys.Add($"{pasteModName}+0");
            }

            var ssModFlags = settings.GetScreenshotModifierFlags();
            var ssModName = settings.GetScreenshotModifierDisplayName();
            if (RegisterHotKey(_hwnd, HOTKEY_SCREENSHOT, ssModFlags, VK_SNAPSHOT))
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

        public void UnregisterAll()
        {
            foreach (var id in _registeredHotkeyIds)
                UnregisterHotKey(_hwnd, id);
            _registeredHotkeyIds.Clear();
        }

        public void HandleHotkeyMessage(int wParam)
        {
            if (wParam == HOTKEY_SCREENSHOT)
            {
                HotkeyPressed?.Invoke(SCREENSHOT_HOTKEY_ID);
                return;
            }

            int slotIndex = wParam - HOTKEY_SLOT_BASE;
            if (slotIndex >= 0 && slotIndex < MAX_SLOTS)
            {
                HotkeyPressed?.Invoke(slotIndex);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            UnregisterAll();
        }
    }
}
