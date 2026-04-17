using System.Runtime.InteropServices;
using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    internal class ClipboardManager : IDisposable
    {
        private readonly List<ClipboardItem> _slots = new();
        private readonly IntPtr _hwnd;
        private readonly AppSettings _settings;
        private bool _isProcessingClipboard;
        private string? _lastClipboardText;
        private bool _clipboardListenerRegistered;
        private bool _suppressMonitoring;
        private bool _disposed;

        public IReadOnlyList<ClipboardItem> Slots => _slots;
        public event Action? SlotsChanged;
        public event Action<string, string>? NotificationRequested;

        public ClipboardManager(IntPtr windowHandle, AppSettings settings)
        {
            _hwnd = windowHandle;
            _settings = settings;
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
        }

        public void RemoveSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return;

            var item = _slots[index];
            _slots.RemoveAt(index);
            item.Dispose();
            SlotsChanged?.Invoke();
        }

        public void RemoveSlots(IList<int> indices)
        {
            var sorted = indices.OrderByDescending(i => i).ToList();
            foreach (int idx in sorted)
            {
                if (idx >= 0 && idx < _slots.Count)
                {
                    _slots[idx].Dispose();
                    _slots.RemoveAt(idx);
                }
            }
            SlotsChanged?.Invoke();
        }

        public void ClearAll()
        {
            foreach (var slot in _slots)
                slot.Dispose();
            _slots.Clear();
            _lastClipboardText = null;
            SlotsChanged?.Invoke();
        }

        public void TrimToMaxSlots()
        {
            while (_slots.Count > _settings.MaxSlots)
            {
                var oldest = _slots[0];
                _slots.RemoveAt(0);
                oldest.Dispose();
            }
            SlotsChanged?.Invoke();
        }

        public void NotifyChanged()
        {
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

            foreach (var slot in _slots)
                slot.Dispose();
            _slots.Clear();
        }
    }
}
