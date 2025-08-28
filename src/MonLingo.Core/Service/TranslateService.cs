using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 翻譯服務實現
    /// 基於 Gaminik.Service.TranslateService 策略模式
    /// 支援用戶語言配置，記住輸入輸出語言設定
    /// </summary>
    public class TranslateService : ITranslateService
    {
        private readonly HttpClient _httpClient;
        private TranslationEngine _currentEngine = TranslationEngine.Google;
        private ILanguageConfigService _languageConfigService;
        private string _lastUsedSourceLanguage = "auto";
        private string _lastUsedTargetLanguage = "zh-tw";
        
        public TranslationEngine CurrentEngine => _currentEngine;

        public TranslateService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            EnsureLanguageConfigService();
        }

        /// <summary>
        /// 確保語言配置服務已初始化
        /// </summary>
        private void EnsureLanguageConfigService()
        {
            if (_languageConfigService == null)
            {
                try
                {
                    _languageConfigService = Phase5ServiceContainer.GetService<ILanguageConfigService>();
                    if (_languageConfigService != null)
                    {
                        // 監聽語言配置變更
                        _languageConfigService.LanguageConfigChanged += OnLanguageConfigChanged;
                    }
                }
                catch (Exception ex)
                {
                    // 記錄錯誤但不影響翻譯功能
                    System.Diagnostics.Debug.WriteLine($"[TranslateService] 無法獲取語言配置服務: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 處理語言配置變更
        /// </summary>
        private void OnLanguageConfigChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[TranslateService] 語言配置已變更，將在下次翻譯時使用新設定");
        }

        /// <summary>
        /// 使用用戶配置的語言進行翻譯 (記住語言設定)
        /// </summary>
        /// <param name="text">要翻譯的文字</param>
        /// <returns>翻譯結果</returns>
        public async Task<string> TranslateWithConfigAsync(string text)
        {
            EnsureLanguageConfigService();
            
            string sourceLanguage = "auto";
            string targetLanguage = "zh-tw";
            
            if (_languageConfigService != null)
            {
                try
                {
                    sourceLanguage = await _languageConfigService.GetSourceLanguageAsync() ?? "auto";
                    targetLanguage = await _languageConfigService.GetTargetLanguageAsync() ?? "zh-tw";
                    
                    // 記住最後使用的語言設定
                    _lastUsedSourceLanguage = sourceLanguage;
                    _lastUsedTargetLanguage = targetLanguage;
                    
                    System.Diagnostics.Debug.WriteLine($"[TranslateService] 使用語言設定: {sourceLanguage} -> {targetLanguage}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TranslateService] 獲取語言配置失敗: {ex.Message}");
                    // 使用最後記住的設定
                    sourceLanguage = _lastUsedSourceLanguage;
                    targetLanguage = _lastUsedTargetLanguage;
                }
            }
            
            return await TranslateAsync(text, sourceLanguage, targetLanguage);
        }

        public void SetEngine(TranslationEngine engine)
        {
            _currentEngine = engine;
        }

        public void SetTranslationEngine(TranslationEngine engine)
        {
            SetEngine(engine);
        }

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
                        return await TranslateViaGoogleAsync(text, sourceLang, targetLang);
                }
            }
            catch (Exception ex)
            {
                throw new TranslationException($"翻譯失敗: {ex.Message}", ex);
            }
        }

        private async Task<string> TranslateViaGoogleAsync(string text, string sourceLang, string targetLang)
        {
            try
            {
                // Google Translate API 調用邏輯
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";
                
                var response = await _httpClient.GetStringAsync(url);
                
                // 解析Google Translate響應
                var jsonDocument = JsonDocument.Parse(response);
                var root = jsonDocument.RootElement;
                
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var translations = root[0];
                    if (translations.ValueKind == JsonValueKind.Array)
                    {
                        var result = new StringBuilder();
                        foreach (var translation in translations.EnumerateArray())
                        {
                            if (translation.ValueKind == JsonValueKind.Array && translation.GetArrayLength() > 0)
                            {
                                result.Append(translation[0].GetString());
                            }
                        }
                        return result.ToString();
                    }
                }
                
                return text; // 如果解析失敗，返回原文
            }
            catch (Exception ex)
            {
                throw new TranslationException($"Google翻譯失敗: {ex.Message}", ex);
            }
        }

        private async Task<string> TranslateLocallyAsync(string text, string sourceLang, string targetLang)
        {
            // 本地翻譯邏輯 (待實現)
            await Task.Delay(100); // 模擬異步操作
            
            // 這裡可以實現本地翻譯引擎
            return $"[本地翻譯] {text}";
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "unknown";

            try
            {
                // 使用Google Translate API進行語言檢測
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={Uri.EscapeDataString(text)}";
                
                var response = await _httpClient.GetStringAsync(url);
                var jsonDocument = JsonDocument.Parse(response);
                var root = jsonDocument.RootElement;
                
                // 嘗試從響應中提取檢測到的語言
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 2)
                {
                    var detectedLang = root[2];
                    if (detectedLang.ValueKind == JsonValueKind.String)
                    {
                        return detectedLang.GetString() ?? "unknown";
                    }
                }
                
                return "unknown";
            }
            catch (Exception ex)
            {
                throw new TranslationException($"語言檢測失敗: {ex.Message}", ex);
            }
        }

        public async Task<string[]> GetSupportedLanguagesAsync()
        {
            // 返回支援的語言列表
            await Task.CompletedTask; // 修復編譯警告
            
            return new string[]
            {
                "auto", "zh", "zh-cn", "zh-tw", "en", "ja", "ko", "fr", "de", "es", "it", "pt", "ru", 
                "ar", "hi", "th", "vi", "id", "ms", "tl", "tr", "pl", "nl", "sv", "da", "no", "fi",
                "cs", "sk", "hu", "ro", "bg", "hr", "sl", "et", "lv", "lt", "mt", "ga", "cy", "is",
                "fa", "ur", "bn", "gu", "ta", "te", "kn", "ml", "si", "my", "lo", "km", "ka", "am",
                "sw", "zu", "af", "sq", "eu", "be", "bs", "ca", "co", "eo", "gl", "haw", "he", "ig",
                "jw", "kk", "ky", "la", "lb", "mk", "mg", "mi", "mn", "ne", "ny", "ps", "sm", "gd",
                "sn", "so", "st", "su", "tg", "tt", "uz", "xh", "yi", "yo"
            };
        }

        public async Task<bool> IsEngineAvailableAsync(TranslationEngine engine)
        {
            try
            {
                switch (engine)
                {
                    case TranslationEngine.Google:
                        // 測試Google Translate可用性
                        var testResponse = await _httpClient.GetAsync("https://translate.googleapis.com");
                        return testResponse.IsSuccessStatusCode;
                    
                    case TranslationEngine.Local:
                        // 檢查本地翻譯引擎
                        return true; // 假設本地引擎總是可用
                    
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            
            // 取消訂閱語言配置變更事件
            if (_languageConfigService != null)
            {
                _languageConfigService.LanguageConfigChanged -= OnLanguageConfigChanged;
            }
        }
    }

    /// <summary>
    /// 翻譯引擎類型
    /// <summary>
    /// 翻譯異常
    /// </summary>
    public class TranslationException : Exception
    {
        public TranslationException(string message) : base(message) { }
        public TranslationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
