using System;
using System.Threading.Tasks;
using System.Drawing;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// OCR 服務介面
    /// 基於 PRD §2.3.1 Native.dll OCR API 設計
    /// </summary>
    public interface IOcrService
    {
        /// <summary>
        /// 初始化 OCR 引擎
        /// </summary>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// 識別圖像中的文字
        /// </summary>
        Task<OcrResult> RecognizeTextAsync(byte[] imageData, int width, int height);
        
        /// <summary>
        /// 釋放 OCR 資源
        /// </summary>
        void Dispose();
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }
    }
    
    /// <summary>
    /// OCR 識別結果
    /// </summary>
    public class OcrResult
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
        public OcrLine[] Lines { get; set; }
    }
    
    /// <summary>
    /// OCR 行結果
    /// </summary>
    public class OcrLine
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
        public OcrWord[] Words { get; set; }
    }
    
    /// <summary>
    /// OCR 詞結果
    /// </summary>
    public class OcrWord
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
    }
}
