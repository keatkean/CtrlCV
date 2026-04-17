using System.ComponentModel;

namespace CtrlCV
{
    internal static class ScreenshotHelper
    {
        public static Bitmap? CaptureFullScreen()
        {
            try
            {
                var bounds = SystemInformation.VirtualScreen;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return null;

                var bmp = new Bitmap(bounds.Width, bounds.Height);
                try
                {
                    using var g = Graphics.FromImage(bmp);
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    return bmp;
                }
                catch
                {
                    bmp.Dispose();
                    throw;
                }
            }
            catch (Win32Exception) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (ArgumentException) { return null; }
        }

        public static Bitmap? CaptureActiveWindow()
        {
            try
            {
                var hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    return null;

                if (!NativeMethods.GetWindowRect(hwnd, out var rect))
                    return null;

                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                if (w <= 0 || h <= 0)
                    return null;

                var bmp = new Bitmap(w, h);
                try
                {
                    using var g = Graphics.FromImage(bmp);
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(w, h));
                    return bmp;
                }
                catch
                {
                    bmp.Dispose();
                    throw;
                }
            }
            catch (Win32Exception) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (ArgumentException) { return null; }
        }

        public static Bitmap? CaptureRegion(Rectangle region)
        {
            try
            {
                if (region.Width <= 0 || region.Height <= 0)
                    return null;

                var bmp = new Bitmap(region.Width, region.Height);
                try
                {
                    using var g = Graphics.FromImage(bmp);
                    g.CopyFromScreen(region.Location, Point.Empty, region.Size);
                    return bmp;
                }
                catch
                {
                    bmp.Dispose();
                    throw;
                }
            }
            catch (Win32Exception) { return null; }
            catch (InvalidOperationException) { return null; }
            catch (ArgumentException) { return null; }
        }
    }
}
