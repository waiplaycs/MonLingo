using MonLingo.Core.ViewModel;

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
        /// 顯示翻譯結果
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="translatedText">譯文</param>
        void Show(string originalText, string translatedText);
    }
}
