using System.Windows.Media.Imaging;
using System.Drawing;

namespace MindFlayer.intermutatio
{
    public static partial class ScreenCapture
    {
        public static BitmapSource CaptureScreen()
        {
            using var bitmap = new Bitmap(GetScreenWidth(), GetScreenHeight());
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            }
            return ConvertToBitmapSource(bitmap);
        }

        public static BitmapSource CaptureWindow(IntPtr handle)
        {
            var rect = GetWindowRect(handle);
            using var bitmap = new Bitmap(rect.Width, rect.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bitmap.Size);
            }
            return ConvertToBitmapSource(bitmap);
        }

        private static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution, System.Windows.Media.PixelFormats.Bgr32, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        private static Rectangle GetWindowRect(IntPtr handle)
        {
            GetWindowRect(handle, out RECT rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        [System.Runtime.InteropServices.LibraryImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.LibraryImport("user32.dll")]
        private static partial int GetSystemMetrics(int nIndex);

        private static int GetScreenWidth() => GetSystemMetrics(0);
        private static int GetScreenHeight() => GetSystemMetrics(1);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
