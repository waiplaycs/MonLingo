using MonLingo.Core.ViewModel;
using MonLingo.ViewModel;
using System.Windows;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 顯示服務介面
    /// </summary>
    public interface IDisplayService
    {
        /// <summary>
        /// 設置字幕 ViewModel 引用
        /// </summary>
        /// <param name="subtitleViewModel">字幕 ViewModel</param>
        void SetSubtitleViewModel(SubtitleViewModel subtitleViewModel);
        
        /// <summary>
        /// 設置主工具條 ViewModel 引用
        /// </summary>
        /// <param name="mainBarViewModel">主工具條 ViewModel</param>
        void SetMainBarViewModel(WorkingMainBarWindowViewModel mainBarViewModel);
        
        /// <summary>
        /// 設置 OCR 結果和區域資訊（用於覆蓋模式）
        /// </summary>
        /// <param name="ocrResult">OCR 結果</param>
        /// <param name="region">區域資訊</param>
        void SetOcrContext(OcrResult ocrResult, Rect region);
        
        /// <summary>
        /// 顯示翻譯結果
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="translatedText">譯文</param>
        void Show(string originalText, string translatedText);

        /// <summary>
        /// 開始新的顯示回合（在一次完整的識別→翻譯開始前呼叫，用於清空舊內容）
        /// </summary>
        void StartNewRound();
    }
}
