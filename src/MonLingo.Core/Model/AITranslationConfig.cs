namespace MonLingo.Core.Model
{
    /// <summary>
    /// AI翻譯服務配置
    /// </summary>
    public class AITranslationConfig
    {
        /// <summary>
        /// AI提供商(OpenAI, Azure, Claude等)
        /// </summary>
        public string Provider { get; set; } = "OpenAI";

        /// <summary>
        /// 使用的模型名稱
        /// </summary>
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// API密鑰
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// API端點(可選，用於Azure OpenAI等)
        /// </summary>
        public string ApiEndpoint { get; set; }

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
