using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    internal class PasteService
    {
        private readonly ClipboardManager _clipboardManager;

        public event Action<string, string>? NotificationRequested;

        public PasteService(ClipboardManager clipboardManager)
        {
            _clipboardManager = clipboardManager;
        }

        public async Task PasteFromSlotAsync(int slotIndex)
        {
            var slots = _clipboardManager.Slots;
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return;

            _clipboardManager.SetSuppressMonitoring(true);
            try
            {
                var item = slots[slotIndex];
                bool clipboardSet = false;

                if (item.ItemType == ClipboardItemType.Text && item.Text != null)
                {
                    clipboardSet = ClipboardManager.ClipboardRetry(() =>
                    {
                        Clipboard.SetText(item.Text);
                        return true;
                    }) == true;
                }
                else if (item.ItemType == ClipboardItemType.Image && item.ImageData != null)
                {
                    clipboardSet = ClipboardManager.ClipboardRetry(() =>
                    {
                        Clipboard.SetImage(item.ImageData);
                        return true;
                    }) == true;
                }

                if (!clipboardSet)
                {
                    NotificationRequested?.Invoke("Paste failed", "Could not access clipboard. It may be in use by another application.");
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
                Form1.LogError("Paste simulation error", ex);
                NotificationRequested?.Invoke("Paste failed", "An error occurred during paste.");
            }
            finally
            {
                _clipboardManager.SetSuppressMonitoring(false);
            }
        }
    }
}
