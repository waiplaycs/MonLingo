using System;
using System.Threading.Tasks;
using System.Drawing;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 螢幕擷取服務介面
    /// 基於 PRD §2.3.1 的完整螢幕擷取系統
    /// </summary>
    public interface IScreenCaptureService
    {
        /// <summary>
        /// 開始擷取指定視窗
        /// </summary>
        bool StartCapture(IntPtr hwnd);
        
        /// <summary>
        /// 讀取最新擷取的幀
        /// </summary>
        CaptureFrame ReadFrame();
        
        /// <summary>
        /// 停止擷取
        /// </summary>
        void StopCapture();
        
        /// <summary>
        /// 是否正在擷取
        /// </summary>
        bool IsCapturing { get; }
    }
    
    /// <summary>
    /// 擷取幀資料結構
    /// </summary>
    public class CaptureFrame
    {
        public byte[] ImageData { get; set; }
        public int Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
