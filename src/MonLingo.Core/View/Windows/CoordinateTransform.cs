using System.Windows;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 座標轉換參數結構
    /// 用於將OCR結果座標轉換為實際螢幕座標
    /// </summary>
    public class CoordinateTransform
    {
        /// <summary>
        /// 原始選中的區域座標（WPF邏輯單位）
        /// </summary>
        public Rect SelectedRegion { get; set; }

        /// <summary>
        /// DPI縮放比例
        /// </summary>
        public double DpiScale { get; set; }

        /// <summary>
        /// 虛擬螢幕左邊界偏移
        /// </summary>
        public double VirtualScreenLeft { get; set; }

        /// <summary>
        /// 虛擬螢幕上邊界偏移
        /// </summary>
        public double VirtualScreenTop { get; set; }

        /// <summary>
        /// 將OCR結果中的相對座標轉換為絕對螢幕座標
        /// </summary>
        /// <param name="ocrX">OCR結果中的X座標（相對於截圖圖片）</param>
        /// <param name="ocrY">OCR結果中的Y座標（相對於截圖圖片）</param>
        /// <returns>絕對螢幕座標</returns>
        public Point TransformToScreenCoordinates(double ocrX, double ocrY)
        {
            // OCR座標是相對於縮放後的截圖圖片，需要：
            // 1. 將OCR座標除以DPI縮放比例，轉換回WPF邏輯單位
            // 2. 加上原始選中區域的偏移
            
            var logicalX = ocrX / DpiScale;
            var logicalY = ocrY / DpiScale;
            
            var screenX = SelectedRegion.X + logicalX;
            var screenY = SelectedRegion.Y + logicalY;
            
            return new Point(screenX, screenY);
        }
    }
}
