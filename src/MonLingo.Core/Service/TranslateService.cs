using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 翻譯服務實現
    /// 基於 Gaminik.Service.TranslateService 策略模式
    /// </summary>
    public class TranslateService : ITranslateService
    {
        private readonly HttpClient _httpClient;
        private TranslationEngine _currentEngine = TranslationEngine.Google;
        
        public TranslationEngine CurrentEngine => _currentEngine;
        
        public TranslateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }
        
        /// <summary>
        /// 翻譯文字
        /// </summary>
        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
                
            try
            {
                switch (_currentEngine)
                {
                    case TranslationEngine.Google:
                        return await TranslateViaGoogleAsync(text, sourceLang, targetLang);
                    case TranslationEngine.Local:
                        return await TranslateLocallyAsync(text, sourceLang, targetLang);
                    default:
                        // 預設使用 Google 翻譯
                        return await TranslateViaGoogleAsync(text, sourceLang, targetLang);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Translation failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 使用 Google 翻譯 API 翻譯（免費版本）
        /// </summary>
        private async Task<string> TranslateViaGoogleAsync(string text, string sourceLang, string targetLang)
        {
            try
            {
                // 使用 Google 翻譯的公開 API
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";
                
                var response = await _httpClient.GetStringAsync(url);
                
                // 解析 Google 翻譯 API 回應
                var jsonDoc = JsonDocument.Parse(response);
                var translations = jsonDoc.RootElement[0];
                
                var result = new StringBuilder();
                foreach (var translation in translations.EnumerateArray())
                {
                    if (translation.GetArrayLength() > 0)
                    {
                        result.Append(translation[0].GetString());
                    }
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Google translation failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 本地翻譯（暫時返回原文）
        /// </summary>
        private async Task<string> TranslateLocallyAsync(string text, string sourceLang, string targetLang)
        {
            // TODO: 實現本地翻譯引擎
            await Task.Delay(100); // 模擬處理時間
            return $"[本地翻譯] {text}";
        }
        
        /// <summary>
        /// 檢測語言
        /// </summary>
        public async Task<string> DetectLanguageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "unknown";
                
            try
            {
                // 使用 Google 語言檢測 API
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={Uri.EscapeDataString(text)}";
                
                var response = await _httpClient.GetStringAsync(url);
                var jsonDoc = JsonDocument.Parse(response);
                
                // 嘗試從回應中提取檢測到的語言
                if (jsonDoc.RootElement.GetArrayLength() > 2)
                {
                    return jsonDoc.RootElement[2].GetString() ?? "unknown";
                }
                
                return "unknown";
            }
            catch (Exception)
            {
                return "unknown";
            }
        }
        
        /// <summary>
        /// 獲取支援的語言列表
        /// </summary>
        public async Task<string[]> GetSupportedLanguagesAsync()
        {
            // 常見語言代碼
            return new[]
            {
                "auto", "zh", "zh-TW", "zh-CN", "en", "ja", "ko", 
                "es", "fr", "de", "it", "pt", "ru", "ar", "hi"
            };
        }
        
        /// <summary>
        /// 設定翻譯引擎
        /// </summary>
        public void SetTranslationEngine(TranslationEngine engine)
        {
            _currentEngine = engine;
        }
        
        /// <summary>
        /// 檢查引擎是否可用
        /// </summary>
        public async Task<bool> IsEngineAvailableAsync(TranslationEngine engine)
        {
            try
            {
                switch (engine)
                {
                    case TranslationEngine.Google:
                        // 測試 Google 翻譯連線
                        var testResult = await TranslateViaGoogleAsync("test", "en", "zh");
                        return !string.IsNullOrEmpty(testResult);
                        
                    case TranslationEngine.Local:
                        // 本地翻譯總是可用
                        return true;
                        
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
