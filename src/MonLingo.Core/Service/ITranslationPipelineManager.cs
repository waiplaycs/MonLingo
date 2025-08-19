using System;
using System.Threading.Tasks;
using System.Drawing;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 翻譯管線管理器介面
    /// 基於 Gaminik.Core.TranslationPipelineManager 設計（PRD §11.3）
    /// </summary>
    public interface ITranslationPipelineManager
    {
        /// <summary>
        /// 啟動完整的擷取翻譯會話
        /// </summary>
        Task StartCaptureSessionAsync(IntPtr targetWindow);
        
        /// <summary>
        /// 停止擷取會話
        /// </summary>
        void StopCaptureSession();
        
        /// <summary>
        /// 檢查是否正在擷取
        /// </summary>
        bool IsCapturing { get; }
        
        /// <summary>
        /// 翻譯完成事件
        /// </summary>
        event Action<TranslationResult> TranslationCompleted;
        
        /// <summary>
        /// 錯誤發生事件
        /// </summary>
        event Action<string> ErrorOccurred;
    }
    
    /// <summary>
    /// 翻譯結果資料結構（PRD §11.3）
    /// </summary>
    public class TranslationResult
    {
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public DateTime Timestamp { get; set; }
        public Rectangle BoundingBox { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public double Confidence { get; set; }
    }
}
