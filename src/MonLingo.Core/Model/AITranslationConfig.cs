namespace MonLingo.Core.Model
{
    /// <summary>
    /// AI提供商類型
    /// </summary>
    public enum AIProvider
    {
        OpenAI,
        DeepSeek,
        Gemini,
        GoogleAI,
        OpenRouter
    }

    /// <summary>
    /// AI翻譯服務配置
    /// </summary>
    public class AITranslationConfig
    {
        /// <summary>
        /// AI提供商
        /// </summary>
        public AIProvider Provider { get; set; } = AIProvider.OpenAI;

        /// <summary>
        /// 使用的模型名稱
        /// 推薦模型:
        /// - OpenAI: gpt-4o-mini, gpt-4o
        /// - DeepSeek: deepseek-chat
        /// - Gemini: gemini-1.5-flash, gemini-1.5-pro
        /// - GoogleAI: gemini-2.0-flash-latest (直連 Google AI Studio,推薦!)
        /// - OpenRouter: google/gemini-2.0-flash-exp:free, anthropic/claude-3.5-sonnet
        /// </summary>
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// API密鑰
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// API端點(可選)
        /// 自動配置:
        /// - OpenAI: https://api.openai.com/v1 (默認)
        /// - DeepSeek: https://api.deepseek.com
        /// - GoogleAI: https://generativelanguage.googleapis.com/v1beta
        /// - OpenRouter: https://openrouter.ai/api/v1
        /// - Gemini: 通過 Google SDK
        /// </summary>
        public string ApiEndpoint { get; set; }

        /// <summary>
        /// 獲取API端點(自動根據Provider設置)
        /// </summary>
        public string GetApiEndpoint()
        {
            if (!string.IsNullOrWhiteSpace(ApiEndpoint))
                return ApiEndpoint;

            return Provider switch
            {
                AIProvider.DeepSeek => "https://api.deepseek.com",
                AIProvider.OpenAI => "https://api.openai.com/v1",
                AIProvider.GoogleAI => "https://generativelanguage.googleapis.com/v1beta",
                AIProvider.OpenRouter => "https://openrouter.ai/api/v1",
                _ => null
            };
        }

        /// <summary>
        /// 獲取環境變數名稱(用於API密鑰)
        /// </summary>
        public string GetEnvironmentVariableName()
        {
            return Provider switch
            {
                AIProvider.DeepSeek => "MONLINGO_DEEPSEEK_API_KEY",
                AIProvider.OpenAI => "MONLINGO_OPENAI_API_KEY",
                AIProvider.Gemini => "MONLINGO_GEMINI_API_KEY",
                AIProvider.GoogleAI => "MONLINGO_GOOGLEAI_API_KEY",
                AIProvider.OpenRouter => "MONLINGO_OPENROUTER_API_KEY",
                _ => "MONLINGO_API_KEY"
            };
        }

        /// <summary>
        /// 請求超時時間(秒)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 最大重試次數
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 溫度參數(0.0-2.0，控制創造性)
        /// </summary>
        public double Temperature { get; set; } = 0.3;

        /// <summary>
        /// 最大輸出tokens
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// 是否啟用緩存
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 緩存過期時間(小時)
        /// </summary>
        public int CacheExpirationHours { get; set; } = 24;

        /// <summary>
        /// 每日API調用配額限制(0表示無限制)
        /// </summary>
        public int DailyQuotaLimit { get; set; } = 1000;

        /// <summary>
        /// 是否啟用詳細日誌
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;
    }
}
