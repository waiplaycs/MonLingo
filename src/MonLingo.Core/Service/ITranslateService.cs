using System;
using System.Threading.Tasks;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 翻譯服務介面
    /// 基於 Gaminik.Service.TranslateService 策略模式
    /// </summary>
    public interface ITranslateService
    {
        /// <summary>
        /// 翻譯文字
        /// </summary>
        Task<string> TranslateAsync(string text, string sourceLang, string targetLang);
        
        /// <summary>
        /// 檢測語言
        /// </summary>
        Task<string> DetectLanguageAsync(string text);
        
        /// <summary>
        /// 獲取支援的語言列表
        /// </summary>
        Task<string[]> GetSupportedLanguagesAsync();
        
        /// <summary>
        /// 設定翻譯引擎
        /// </summary>
        void SetTranslationEngine(TranslationEngine engine);
        
        /// <summary>
        /// 獲取當前翻譯引擎
        /// </summary>
        TranslationEngine CurrentEngine { get; }
        
        /// <summary>
        /// 檢查引擎是否可用
        /// </summary>
        Task<bool> IsEngineAvailableAsync(TranslationEngine engine);
    }
    
    /// <summary>
    /// 翻譯引擎類型
    /// </summary>
    public enum TranslationEngine
    {
        Google,
        DeepL,
        Microsoft,
        Baidu,
        GPT35,
        GPT4,
        Local  // 離線翻譯
    }
}
