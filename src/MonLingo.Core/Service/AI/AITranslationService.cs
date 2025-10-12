using MonLingo.Core.Model;
using NLog;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonLingo.Core.Service.AI
{
    /// <summary>
    /// AI智能翻譯服務實現
    /// </summary>
    public class AITranslationService : IAITranslationService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly AITranslationConfig _config;
        private readonly ChatClient _chatClient;
        private int _dailyCallCount = 0;
        private DateTime _lastResetDate = DateTime.Today;

        public AITranslationService(AITranslationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // 驗證配置
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                var envVarName = _config.GetEnvironmentVariableName();
                throw new InvalidOperationException(
                    $"API密鑰未配置。請在appsettings.json中設置AITranslation:ApiKey，" +
                    $"或設置環境變數{envVarName}");
            }

            // 初始化ChatClient (支援多提供商)
            _chatClient = CreateChatClient();

            Logger.Info($"AITranslationService初始化完成 - 提供商: {_config.Provider}, 模型: {_config.Model}");
        }

        /// <summary>
        /// 根據配置創建ChatClient
        /// </summary>
        private ChatClient CreateChatClient()
        {
            var endpoint = _config.GetApiEndpoint();

            // DeepSeek 和其他兼容 OpenAI API 的服務
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                var openAiClientOptions = new OpenAIClientOptions
                {
                    Endpoint = new Uri(endpoint)
                };
                
                var apiKeyCredential = new ApiKeyCredential(_config.ApiKey);
                var client = new OpenAIClient(apiKeyCredential, openAiClientOptions);
                return client.GetChatClient(_config.Model);
            }

            // 默認 OpenAI
            return new ChatClient(
                model: _config.Model,
                apiKey: _config.ApiKey);
        }

        /// <summary>
        /// 智能翻譯單個欄位
        /// </summary>
        public async Task<AITranslationResult> SmartTranslateAsync(
            List<LayoutLine> columnLines,
            string sourceLanguage = "auto",
            string targetLanguage = "zh-TW")
        {
            if (columnLines == null || columnLines.Count == 0)
            {
                return new AITranslationResult
                {
                    Success = false,
                    ErrorMessage = "輸入的文字行列表為空"
                };
            }

            // 檢查每日配額
            if (!CheckDailyQuota())
            {
                return new AITranslationResult
                {
                    Success = false,
                    ErrorMessage = $"已超出每日API調用配額限制({_config.DailyQuotaLimit}次)"
                };
            }

            var stopwatch = Stopwatch.StartNew();
            var retryCount = 0;

            try
            {
                Logger.Debug($"開始AI翻譯: {columnLines.Count}行文字 -> {targetLanguage}");

                // 構建Prompt
                var userPrompt = PromptTemplates.BuildSmartTranslationPrompt(
                    columnLines, sourceLanguage, targetLanguage);

                if (_config.EnableVerboseLogging)
                {
                    Logger.Debug($"User Prompt:\n{userPrompt}");
                }

                // 調用OpenAI API (帶重試)
                var retryResult = await ExecuteWithRetryAsync(
                    async () => await CallOpenAIAsync(userPrompt),
                    _config.MaxRetries);

                var apiResponse = retryResult.result;
                var responseJson = apiResponse.responseJson;
                var tokenUsage = apiResponse.tokenUsage;
                retryCount = retryResult.retryCount;

                stopwatch.Stop();

                // 解析響應
                var result = ParseResponse(responseJson, columnLines);
                
                // 填充元數據
                result.Metadata = new AITranslationMetadata
                {
                    Model = _config.Model,
                    DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                    TokenUsage = tokenUsage,
                    EstimatedCost = CalculateCost(tokenUsage),
                    Timestamp = DateTime.Now,
                    RetryCount = retryCount
                };

                // 增加調用計數
                IncrementDailyCallCount();

                Logger.Info($"AI翻譯完成: {columnLines.Count}行 -> {result.Paragraphs.Count}段落, " +
                           $"耗時{stopwatch.ElapsedMilliseconds}ms, 成本${result.Metadata.EstimatedCost:F6}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.Error($"AI翻譯失敗: {ex.Message}", ex);
                
                return new AITranslationResult
                {
                    Success = false,
                    ErrorMessage = $"AI翻譯失敗: {ex.Message}",
                    Metadata = new AITranslationMetadata
                    {
                        Model = _config.Model,
                        DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                        Timestamp = DateTime.Now,
                        RetryCount = retryCount
                    }
                };
            }
        }

        /// <summary>
        /// 批量智能翻譯
        /// </summary>
        public async Task<List<AITranslationResult>> SmartTranslateBatchAsync(
            List<List<LayoutLine>> columns,
            string sourceLanguage = "auto",
            string targetLanguage = "zh-TW")
        {
            var results = new List<AITranslationResult>();

            foreach (var column in columns)
            {
                var result = await SmartTranslateAsync(column, sourceLanguage, targetLanguage);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 調用OpenAI API
        /// </summary>
        private async Task<(string responseJson, TokenUsage tokenUsage)> CallOpenAIAsync(string userPrompt)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(PromptTemplates.SYSTEM_PROMPT),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = (float)_config.Temperature,
                MaxOutputTokenCount = _config.MaxTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await _chatClient.CompleteChatAsync(messages, options);

            var responseText = response.Value.Content[0].Text;
            
            // 提取Token使用情況
            var tokenUsage = new TokenUsage
            {
                InputTokens = response.Value.Usage.InputTokenCount,
                OutputTokens = response.Value.Usage.OutputTokenCount
            };

            if (_config.EnableVerboseLogging)
            {
                Logger.Debug($"API響應:\n{responseText}");
                Logger.Debug($"Token使用: Input={tokenUsage.InputTokens}, Output={tokenUsage.OutputTokens}");
            }

            return (responseText, tokenUsage);
        }

        /// <summary>
        /// 解析AI響應JSON
        /// </summary>
        private AITranslationResult ParseResponse(string jsonResponse, List<LayoutLine> originalLines)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var jsonDoc = JsonDocument.Parse(jsonResponse);
                var root = jsonDoc.RootElement;

                var result = new AITranslationResult
                {
                    Success = true,
                    DetectedLanguage = root.GetProperty("detectedLanguage").GetString(),
                    Paragraphs = new List<TranslatedParagraph>()
                };

                var paragraphsArray = root.GetProperty("paragraphs");
                foreach (var p in paragraphsArray.EnumerateArray())
                {
                    var lineIndices = p.GetProperty("lineIndices")
                        .EnumerateArray()
                        .Select(i => i.GetInt32())
                        .ToList();

                    var paragraph = new TranslatedParagraph
                    {
                        LineIndices = lineIndices,
                        OriginalText = p.GetProperty("originalText").GetString(),
                        TranslatedText = p.GetProperty("translatedText").GetString(),
                        Type = ParseParagraphType(p.GetProperty("type").GetString()),
                        Confidence = p.GetProperty("confidence").GetDouble(),
                        BoundingBox = CalculateBoundingBox(lineIndices, originalLines)
                    };

                    result.Paragraphs.Add(paragraph);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"解析AI響應失敗: {ex.Message}\n響應內容: {jsonResponse}", ex);
                throw new InvalidOperationException($"解析AI響應失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析段落類型
        /// </summary>
        private ParagraphType ParseParagraphType(string typeStr)
        {
            return typeStr?.ToLower() switch
            {
                "heading" => ParagraphType.Heading,
                "body" => ParagraphType.Body,
                "listitem" => ParagraphType.ListItem,
                "quote" => ParagraphType.Quote,
                _ => ParagraphType.Other
            };
        }

        /// <summary>
        /// 計算段落邊界框
        /// </summary>
        private Rectangle CalculateBoundingBox(List<int> lineIndices, List<LayoutLine> originalLines)
        {
            if (lineIndices == null || lineIndices.Count == 0)
                return Rectangle.Empty;

            var boxes = lineIndices
                .Where(i => i >= 0 && i < originalLines.Count)
                .Select(i => originalLines[i].BoundingBox)
                .ToList();

            if (boxes.Count == 0)
                return Rectangle.Empty;

            int minX = boxes.Min(b => b.Left);
            int minY = boxes.Min(b => b.Top);
            int maxX = boxes.Max(b => b.Right);
            int maxY = boxes.Max(b => b.Bottom);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 計算API調用成本
        /// </summary>
        private double CalculateCost(TokenUsage tokenUsage)
        {
            // 根據不同提供商計算成本
            switch (_config.Provider)
            {
                case AIProvider.DeepSeek:
                    // DeepSeek定價: $0.14/1M input tokens, $0.28/1M output tokens
                    return (tokenUsage.InputTokens / 1_000_000.0) * 0.14 +
                           (tokenUsage.OutputTokens / 1_000_000.0) * 0.28;

                case AIProvider.OpenAI:
                    // GPT-4o-mini定價: $0.150/1M input tokens, $0.600/1M output tokens
                    return (tokenUsage.InputTokens / 1_000_000.0) * 0.150 +
                           (tokenUsage.OutputTokens / 1_000_000.0) * 0.600;

                case AIProvider.Gemini:
                    // Gemini 1.5 Flash定價: $0.075/1M input tokens, $0.30/1M output tokens
                    return (tokenUsage.InputTokens / 1_000_000.0) * 0.075 +
                           (tokenUsage.OutputTokens / 1_000_000.0) * 0.30;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 帶重試的執行
        /// </summary>
        private async Task<(T result, int retryCount)> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            int maxRetries)
        {
            int retryCount = 0;
            Exception lastException = null;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    var result = await action();
                    return (result, retryCount);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount = i;

                    if (i < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, i)); // 指數退避
                        Logger.Warn($"API調用失敗(第{i + 1}次),{delay.TotalSeconds}秒後重試: {ex.Message}");
                        await Task.Delay(delay);
                    }
                }
            }

            throw new InvalidOperationException(
                $"API調用失敗,已達最大重試次數({maxRetries}次)", lastException);
        }

        /// <summary>
        /// 檢查每日配額
        /// </summary>
        private bool CheckDailyQuota()
        {
            // 如果配額為0,表示無限制
            if (_config.DailyQuotaLimit <= 0)
                return true;

            // 檢查是否需要重置計數
            if (DateTime.Today > _lastResetDate)
            {
                _dailyCallCount = 0;
                _lastResetDate = DateTime.Today;
            }

            return _dailyCallCount < _config.DailyQuotaLimit;
        }

        /// <summary>
        /// 增加每日調用計數
        /// </summary>
        private void IncrementDailyCallCount()
        {
            _dailyCallCount++;
            
            if (_config.EnableVerboseLogging)
            {
                Logger.Debug($"每日API調用: {_dailyCallCount}/{_config.DailyQuotaLimit}");
            }
        }
    }
}
