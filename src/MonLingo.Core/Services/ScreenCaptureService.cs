using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace MonLingo.Core.Services
{
    /// <summary>
    /// 螢幕截圖服務
    /// </summary>
    public class ScreenCaptureService
    {
        /// <summary>
        /// 截取指定區域的螢幕圖像
        /// </summary>
        /// <param name="region">截圖區域</param>
        /// <returns>截圖的位圖</returns>
        public Bitmap CaptureScreen(Rectangle region)
        {
            try
            {
                var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(region.Left, region.Top, 0, 0, new Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"截圖失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 截取整個螢幕
        /// </summary>
        /// <returns>全螢幕截圖</returns>
        public Bitmap CaptureFullScreen()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            return CaptureScreen(new Rectangle(0, 0, bounds.Width, bounds.Height));
        }

        /// <summary>
        /// 將 Bitmap 轉換為 BitmapSource (WPF 使用)
        /// </summary>
        /// <param name="bitmap">來源位圖</param>
        /// <returns>WPF 位圖源</returns>
        public BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
