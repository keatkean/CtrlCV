namespace CtrlCV
{
    public enum ClipboardItemType
    {
        Text,
        Image
    }

    public class ClipboardItem : IDisposable
    {
        public ClipboardItemType ItemType { get; }
        public string? Text { get; }
        public Image? ImageData { get; private set; }
        public DateTime CopiedAt { get; }
        public bool IsPinned { get; set; }

        private bool _disposed;

        public ClipboardItem(string text)
        {
            ItemType = ClipboardItemType.Text;
            Text = text;
            CopiedAt = DateTime.Now;
        }

        public ClipboardItem(Image image)
        {
            ItemType = ClipboardItemType.Image;
            ImageData = (Image)image.Clone();
            CopiedAt = DateTime.Now;
        }

        public string GetPreview()
        {
            if (ItemType == ClipboardItemType.Text)
            {
                if (string.IsNullOrEmpty(Text))
                    return "(empty)";
                var singleLine = Text.ReplaceLineEndings(" ");
                return singleLine.Length > 100 ? singleLine[..100] + "..." : singleLine;
            }

            if (ImageData != null)
                return $"[Image {ImageData.Width}x{ImageData.Height}]";

            return "[Image - disposed]";
        }

        public Image? CreateThumbnail(int width, int height)
        {
            if (_disposed || ImageData == null)
                return null;

            try
            {
                return ImageData.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ImageData?.Dispose();
                ImageData = null;
                _disposed = true;
            }
        }
    }
}
