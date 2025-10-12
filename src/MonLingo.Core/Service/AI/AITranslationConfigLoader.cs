using MonLingo.Core.Model;
using System;
using System.IO;
using System.Text.Json;

namespace MonLingo.Core.Service.AI
{
    /// <summary>
    /// AI翻譯配置加載器
    /// </summary>
    public static class AITranslationConfigLoader
    {
        /// <summary>
        /// 從appsettings.json加載配置
        /// </summary>
        public static AITranslationConfig LoadFromFile(string configFilePath = null)
        {
            // 默認配置文件路徑
            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                configFilePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "appsettings.json");
            }

            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException(
                    $"配置文件不存在: {configFilePath}。請創建appsettings.json文件。");
            }

            try
            {
                var json = File.ReadAllText(configFilePath);
                var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("AITranslation", out var aiSection))
                {
                    throw new InvalidOperationException(
                        "配置文件中缺少AITranslation節點");
                }

                var providerStr = GetStringProperty(aiSection, "Provider", "OpenAI");
                var provider = Enum.TryParse<AIProvider>(providerStr, true, out var parsedProvider)
                    ? parsedProvider
                    : AIProvider.OpenAI;

                var config = new AITranslationConfig
                {
                    Provider = provider,
                    Model = GetStringProperty(aiSection, "Model", GetDefaultModel(provider)),
                    ApiKey = GetStringProperty(aiSection, "ApiKey", null),
                    ApiEndpoint = GetStringProperty(aiSection, "ApiEndpoint", null),
                    TimeoutSeconds = GetIntProperty(aiSection, "TimeoutSeconds", 30),
                    MaxRetries = GetIntProperty(aiSection, "MaxRetries", 3),
                    Temperature = GetDoubleProperty(aiSection, "Temperature", 0.3),
                    MaxTokens = GetIntProperty(aiSection, "MaxTokens", 2000),
                    EnableCache = GetBoolProperty(aiSection, "EnableCache", true),
                    CacheExpirationHours = GetIntProperty(aiSection, "CacheExpirationHours", 24),
                    DailyQuotaLimit = GetIntProperty(aiSection, "DailyQuotaLimit", 1000),
                    EnableVerboseLogging = GetBoolProperty(aiSection, "EnableVerboseLogging", false)
                };

                // 嘗試從環境變數讀取API密鑰(優先級高於配置文件)
                var envApiKey = Environment.GetEnvironmentVariable(config.GetEnvironmentVariableName());
                if (!string.IsNullOrWhiteSpace(envApiKey))
                {
                    config.ApiKey = envApiKey;
                }

                return config;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"加載配置文件失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 創建默認配置
        /// </summary>
        public static AITranslationConfig CreateDefault()
        {
            var config = new AITranslationConfig();
            
            // 嘗試從環境變數讀取API密鑰
            var envApiKey = Environment.GetEnvironmentVariable(config.GetEnvironmentVariableName());
            if (!string.IsNullOrWhiteSpace(envApiKey))
            {
                config.ApiKey = envApiKey;
            }

            return config;
        }

        /// <summary>
        /// 獲取默認模型名稱
        /// </summary>
        private static string GetDefaultModel(AIProvider provider)
        {
            return provider switch
            {
                AIProvider.DeepSeek => "deepseek-chat",
                AIProvider.Gemini => "gemini-1.5-flash",
                AIProvider.OpenAI => "gpt-4o-mini",
                _ => "gpt-4o-mini"
            };
        }

        private static string GetStringProperty(JsonElement element, string propertyName, string defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetString() ?? defaultValue;
            }
            return defaultValue;
        }

        private static int GetIntProperty(JsonElement element, string propertyName, int defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetInt32();
            }
            return defaultValue;
        }

        private static double GetDoubleProperty(JsonElement element, string propertyName, double defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetDouble();
            }
            return defaultValue;
        }

        private static bool GetBoolProperty(JsonElement element, string propertyName, bool defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetBoolean();
            }
            return defaultValue;
        }
    }
}
