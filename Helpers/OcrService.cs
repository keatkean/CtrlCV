using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace CtrlCV
{
    internal static class OcrService
    {
        public static async Task<string?> ExtractTextAsync(Image image)
        {
            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine == null)
                return null;

            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;

            using var randomAccessStream = memoryStream.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var result = await engine.RecognizeAsync(softwareBitmap);
            var text = result.Text?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
