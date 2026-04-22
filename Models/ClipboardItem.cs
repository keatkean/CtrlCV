using System.Drawing.Imaging;

namespace CtrlCV
{
    public enum ClipboardItemType
    {
        Text,
        Image
    }

    public class ClipboardItem : IDisposable
    {
        public const int MaxTextBytes = 5 * 1024 * 1024;
        public const int InlineImageByteThreshold = 1 * 1024 * 1024;
        public const int MaxPersistableImageBytes = 32 * 1024 * 1024;

        public ClipboardItemType ItemType { get; }
        public string? Text { get; }
        public Image? ImageData { get; private set; }
        public DateTime CopiedAt { get; }
        public bool IsPinned { get; set; }

        private byte[]? _cachedPngBytes;
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

        private ClipboardItem(string? text, Image? image, byte[]? pngBytes, DateTime copiedAt, bool isPinned)
        {
            if (image != null)
            {
                ItemType = ClipboardItemType.Image;
                ImageData = image;
                _cachedPngBytes = pngBytes;
            }
            else
            {
                ItemType = ClipboardItemType.Text;
                Text = text;
            }

            CopiedAt = copiedAt;
            IsPinned = isPinned;
        }

        public static ClipboardItem RehydrateText(string text, DateTime copiedAt, bool isPinned)
        {
            return new ClipboardItem(text, null, null, copiedAt, isPinned);
        }

        public static ClipboardItem RehydrateImage(byte[] pngBytes, DateTime copiedAt, bool isPinned)
        {
            if (pngBytes == null || pngBytes.Length == 0)
                throw new ArgumentException("Image bytes are empty.", nameof(pngBytes));

            using var ms = new MemoryStream(pngBytes, writable: false);
            var source = Image.FromStream(ms);

            if (source.Width <= 0 || source.Height <= 0)
            {
                source.Dispose();
                throw new InvalidDataException("Persisted image has invalid dimensions.");
            }

            var owned = new Bitmap(source);
            source.Dispose();

            return new ClipboardItem(null, owned, pngBytes, copiedAt, isPinned);
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

        public byte[]? GetOrEncodePng()
        {
            if (_disposed || ItemType != ClipboardItemType.Image || ImageData == null)
                return null;

            if (_cachedPngBytes != null)
                return _cachedPngBytes;

            try
            {
                using var ms = new MemoryStream();
                ImageData.Save(ms, ImageFormat.Png);
                _cachedPngBytes = ms.ToArray();
                return _cachedPngBytes;
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
                _cachedPngBytes = null;
                _disposed = true;
            }
        }
    }
}
