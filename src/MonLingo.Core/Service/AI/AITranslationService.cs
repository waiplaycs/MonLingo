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

            var initMsg = $"🤖 AI翻譯服務初始化完成";
            Logger.Info(initMsg);
            Console.WriteLine(initMsg);
            
            var providerMsg = $"   提供商: {_config.Provider}";
            Logger.Info(providerMsg);
            Console.WriteLine(providerMsg);
            
            var modelMsg = $"   模型: {_config.Model}";
            Logger.Info(modelMsg);
            Console.WriteLine(modelMsg);
            
            var endpointMsg = $"   端點: {_config.GetApiEndpoint() ?? "默認"}";
            Logger.Info(endpointMsg);
            Console.WriteLine(endpointMsg);
            
            Console.WriteLine();
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
                var msg1 = $"🤖 開始AI翻譯: {columnLines.Count}行文字 -> {targetLanguage} [{_config.Provider}/{_config.Model}]";
                Logger.Info(msg1);
                Console.WriteLine(msg1);
                
                // 輸出文字框座標範圍
                if (columnLines.Count > 0)
                {
                    var minX = columnLines.Min(l => l.BoundingBox.Left);
                    var maxX = columnLines.Max(l => l.BoundingBox.Right);
                    var minY = columnLines.Min(l => l.BoundingBox.Top);
                    var maxY = columnLines.Max(l => l.BoundingBox.Bottom);
                    var coordMsg = $"📍 文字框座標: X[{minX:F0}, {maxX:F0}] Y[{minY:F0}, {maxY:F0}] " +
                                  $"尺寸: {maxX - minX:F0}×{maxY - minY:F0}px";
                    Logger.Info(coordMsg);
                    Console.WriteLine(coordMsg);
                }
                
                // 輸出行間距分析
                if (columnLines.Count > 1)
                {
                    var avgLineHeight = columnLines.Average(l => l.LineHeight);
                    var msg2 = $"📏 平均行高: {avgLineHeight:F1}px";
                    Logger.Info(msg2);
                    Console.WriteLine(msg2);
                    
                    for (int i = 0; i < columnLines.Count - 1; i++)
                    {
                        var currentLine = columnLines[i];
                        var nextLine = columnLines[i + 1];
                        var verticalGap = nextLine.BoundingBox.Top - currentLine.BoundingBox.Bottom;
                        var gapRatio = verticalGap / avgLineHeight;
                        
                        var gapType = gapRatio > 1.0 ? "大間距🔴" : 
                                     gapRatio > 0.5 ? "中間距🟡" : "正常🟢";
                        
                        var previewText = string.IsNullOrEmpty(currentLine.Text) ? "" : 
                                        currentLine.Text.Substring(0, Math.Min(20, currentLine.Text.Length));
                        
                        var bbox = currentLine.BoundingBox;
                        var gapMsg = $"   行{i}: {gapType} {verticalGap:F0}px ({gapRatio:F2}x) " +
                                   $"座標[{bbox.Left:F0},{bbox.Top:F0},{bbox.Right:F0},{bbox.Bottom:F0}] " +
                                   $"「{previewText}...」";
                        Logger.Debug(gapMsg);
                        Console.WriteLine(gapMsg);
                    }
                    
                    // 輸出最後一行
                    if (columnLines.Count > 0)
                    {
                        var lastLine = columnLines[columnLines.Count - 1];
                        var bbox = lastLine.BoundingBox;
                        var previewText = string.IsNullOrEmpty(lastLine.Text) ? "" : 
                                        lastLine.Text.Substring(0, Math.Min(20, lastLine.Text.Length));
                        var lastMsg = $"   行{columnLines.Count - 1}: (最後行) " +
                                    $"座標[{bbox.Left:F0},{bbox.Top:F0},{bbox.Right:F0},{bbox.Bottom:F0}] " +
                                    $"「{previewText}...」";
                        Logger.Debug(lastMsg);
                        Console.WriteLine(lastMsg);
                    }
                }
                else if (columnLines.Count == 1)
                {
                    // 只有一行時也輸出座標
                    var line = columnLines[0];
                    var bbox = line.BoundingBox;
                    var previewText = string.IsNullOrEmpty(line.Text) ? "" : 
                                    line.Text.Substring(0, Math.Min(20, line.Text.Length));
                    var singleMsg = $"   行0: (單行) " +
                                  $"座標[{bbox.Left:F0},{bbox.Top:F0},{bbox.Right:F0},{bbox.Bottom:F0}] " +
                                  $"「{previewText}...」";
                    Logger.Info(singleMsg);
                    Console.WriteLine(singleMsg);
                }

                // 構建Prompt
                var userPrompt = PromptTemplates.BuildSmartTranslationPrompt(
                    columnLines, sourceLanguage, targetLanguage);

                if (_config.EnableVerboseLogging)
                {
                    Logger.Debug($"📝 User Prompt:\n{userPrompt}");
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
        /// 批量智能翻譯 - 逐個欄位調用(舊方法)
        /// </summary>
        public async Task<List<AITranslationResult>> SmartTranslateBatchAsync(
            List<List<LayoutLine>> columns,
            string sourceLanguage = "auto",
            string targetLanguage = "zh-TW")
        {
            var results = new List<AITranslationResult>();

            var batchMsg = $"📦 批量翻譯開始: 共 {columns.Count} 個欄位 (每個欄位將調用1次API)";
            Logger.Info(batchMsg);
            Console.WriteLine(batchMsg);

            for (int i = 0; i < columns.Count; i++)
            {
                var columnMsg = $"🔄 處理第 {i + 1}/{columns.Count} 個欄位...";
                Logger.Info(columnMsg);
                Console.WriteLine(columnMsg);
                
                var result = await SmartTranslateAsync(columns[i], sourceLanguage, targetLanguage);
                results.Add(result);
                
                var resultMsg = result.Success 
                    ? $"✅ 第 {i + 1} 個欄位翻譯完成" 
                    : $"❌ 第 {i + 1} 個欄位翻譯失敗: {result.ErrorMessage}";
                Logger.Info(resultMsg);
                Console.WriteLine(resultMsg);
            }

            var summaryMsg = $"📦 批量翻譯完成: 成功 {results.Count(r => r.Success)}/{columns.Count} 個欄位";
            Logger.Info(summaryMsg);
            Console.WriteLine(summaryMsg);

            return results;
        }

        /// <summary>
        /// 多欄位智能翻譯 - 一次性API調用(新方法,推薦)
        /// 使用欄位標記系統,只發送行間距信息
        /// </summary>
        /// <param name="columns">多個欄位的文字行列表</param>
        /// <param name="sourceLanguage">源語言</param>
        /// <param name="targetLanguage">目標語言</param>
        /// <returns>每個欄位的AI翻譯結果</returns>
        public async Task<List<AITranslationResult>> SmartTranslateMultiColumnAsync(
            List<List<LayoutLine>> columns,
            string sourceLanguage = "auto",
            string targetLanguage = "zh-TW")
        {
            if (columns == null || columns.Count == 0)
            {
                return new List<AITranslationResult>();
            }

            // 檢查每日配額
            if (!CheckDailyQuota())
            {
                Logger.Warn("已超出每日API調用配額,回退到逐個欄位翻譯");
                return await SmartTranslateBatchAsync(columns, sourceLanguage, targetLanguage);
            }

            var stopwatch = Stopwatch.StartNew();
            var retryCount = 0;

            try
            {
                var msg1 = $"🚀 多欄位一次性翻譯: {columns.Count}個欄位 -> {targetLanguage}";
                Logger.Info(msg1);
                Console.WriteLine(msg1);
                
                var modelMsg = $"   使用模型: {_config.Provider} / {_config.Model}";
                Logger.Info(modelMsg);
                Console.WriteLine(modelMsg);
                
                var strategyMsg = $"   翻譯策略: 欄位標記系統 (1次API調用,節省{columns.Count - 1}次)";
                Logger.Info(strategyMsg);
                Console.WriteLine(strategyMsg);

                // 輸出每個欄位的詳細間距信息
                Console.WriteLine();
                Console.WriteLine("📊 欄位間距分析:");
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    var avgLineHeight = column.Average(l => l.LineHeight);
                    
                    Console.WriteLine($"   【欄位{i + 1}】{column.Count}行文字, 平均行高: {avgLineHeight:F1}px");
                    
                    // 顯示每個間距的詳細數值
                    for (int j = 0; j < column.Count - 1; j++)
                    {
                        var currentLine = column[j];
                        var nextLine = column[j + 1];
                        var verticalGap = nextLine.BoundingBox.Top - currentLine.BoundingBox.Bottom;
                        var gapRatio = verticalGap / avgLineHeight;
                        
                        string gapType;
                        string gapIcon;
                        if (gapRatio > 1.0)
                        {
                            gapType = "大間距";
                            gapIcon = "🔴";
                        }
                        else if (gapRatio > 0.5)
                        {
                            gapType = "中間距";
                            gapIcon = "🟡";
                        }
                        else
                        {
                            gapType = "正常";
                            gapIcon = "🟢";
                        }
                        
                        // 截取文字預覽
                        var textPreview = currentLine.Text.Length > 30 
                            ? currentLine.Text.Substring(0, 30) + "..." 
                            : currentLine.Text;
                        
                        Console.WriteLine($"      行{j}→{j+1}: {gapIcon} {gapType} {verticalGap:F0}px ({gapRatio:F2}x) │ 「{textPreview}」");
                    }
                    
                    Console.WriteLine();
                }
                
                Console.WriteLine("📝 構建多欄位Prompt (使用【欄位X開始/結束】標記 + 行間距比例)...");
                Console.WriteLine();

                // 構建多欄位Prompt(只包含行間距信息)
                var userPrompt = PromptTemplates.BuildMultiColumnTranslationPrompt(
                    columns, sourceLanguage, targetLanguage);

                // 總是輸出Prompt到Terminal,讓用戶看到欄位分隔
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("� 完整Prompt內容 (發送給AI):");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine(userPrompt);
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine();
                
                Logger.Info($"Prompt總長度: {userPrompt.Length} 字符");
                Logger.Debug($"完整Prompt:\n{userPrompt}");

                // 調用OpenAI API (帶重試)
                var retryResult = await ExecuteWithRetryAsync(
                    async () => await CallOpenAIAsync(userPrompt),
                    _config.MaxRetries);

                var apiResponse = retryResult.result;
                var responseJson = apiResponse.responseJson;
                var tokenUsage = apiResponse.tokenUsage;
                retryCount = retryResult.retryCount;

                stopwatch.Stop();

                // 解析多欄位響應
                var results = ParseMultiColumnResponse(responseJson, columns);

                // 填充元數據
                for (int i = 0; i < results.Count; i++)
                {
                    results[i].Metadata = new AITranslationMetadata
                    {
                        Model = _config.Model,
                        DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                        TokenUsage = tokenUsage,
                        EstimatedCost = CalculateCost(tokenUsage),
                        Timestamp = DateTime.Now,
                        RetryCount = retryCount
                    };
                }

                // 增加調用計數(只算1次)
                IncrementDailyCallCount();

                var successCount = results.Count(r => r.Success);
                
                var summaryMsg = $"✅ 多欄位翻譯完成: {successCount}/{columns.Count}個欄位成功, " +
                                $"耗時{stopwatch.ElapsedMilliseconds}ms, Token使用 {tokenUsage.TotalTokens}, " +
                                $"成本${CalculateCost(tokenUsage):F6}, 節省{columns.Count - 1}次API調用";
                Logger.Info(summaryMsg);
                Console.WriteLine(summaryMsg);
                
                // 輸出每個欄位的翻譯結果
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    if (result.Success)
                    {
                        var detailMsg = $"   欄位{i + 1}: 檢測語言={result.DetectedLanguage}, 段落數={result.Paragraphs.Count}";
                        Logger.Info(detailMsg);
                        Console.WriteLine(detailMsg);
                    }
                    else
                    {
                        var errorMsg = $"   欄位{i + 1}: ❌ 失敗 - {result.ErrorMessage}";
                        Logger.Warn(errorMsg);
                        Console.WriteLine(errorMsg);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.Error($"多欄位翻譯失敗: {ex.Message}", ex);

                // Fallback: 回退到逐個欄位翻譯
                Logger.Warn("⚠️ 回退到逐個欄位翻譯模式");
                return await SmartTranslateBatchAsync(columns, sourceLanguage, targetLanguage);
            }
        }

        /// <summary>
        /// 解析多欄位響應JSON
        /// </summary>
        private List<AITranslationResult> ParseMultiColumnResponse(
            string responseJson,
            List<List<LayoutLine>> columns)
        {
            var results = new List<AITranslationResult>();

            try
            {
                var jsonDoc = JsonDocument.Parse(responseJson);
                var root = jsonDoc.RootElement;

                // 獲取檢測到的語言
                var detectedLanguage = root.TryGetProperty("detectedLanguage", out var langProp)
                    ? langProp.GetString() ?? "unknown"
                    : "unknown";

                // 解析columns數組
                if (root.TryGetProperty("columns", out var columnsArray))
                {
                    var columnElements = columnsArray.EnumerateArray().ToList();

                    for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                    {
                        var column = columns[colIdx];
                        var result = new AITranslationResult
                        {
                            Success = true,
                            Paragraphs = new List<TranslatedParagraph>(),
                            DetectedLanguage = detectedLanguage
                        };

                        // 找到對應的欄位元素
                        var columnElement = columnElements.FirstOrDefault(c =>
                            c.TryGetProperty("columnIndex", out var idx) && idx.GetInt32() == colIdx);

                        if (columnElement.ValueKind != JsonValueKind.Undefined &&
                            columnElement.TryGetProperty("paragraphs", out var paragraphs))
                        {
                            foreach (var para in paragraphs.EnumerateArray())
                            {
                                // 解析段落
                                var lineIndices = para.GetProperty("lineIndices")
                                    .EnumerateArray()
                                    .Select(i => i.GetInt32())
                                    .ToList();

                                var paragraph = new TranslatedParagraph
                                {
                                    LineIndices = lineIndices,
                                    OriginalText = para.GetProperty("originalText").GetString(),
                                    TranslatedText = para.GetProperty("translatedText").GetString(),
                                    Type = ParseParagraphType(para.GetProperty("type").GetString()),
                                    Confidence = para.GetProperty("confidence").GetDouble(),
                                    BoundingBox = CalculateBoundingBox(lineIndices, column)
                                };
                                
                                result.Paragraphs.Add(paragraph);
                            }
                        }

                        // 如果這個欄位沒有解析到段落,標記為失敗
                        if (result.Paragraphs.Count == 0)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"欄位{colIdx + 1}沒有解析到段落";
                        }

                        results.Add(result);
                    }
                }
                else
                {
                    // 沒有columns結構,所有欄位標記為失敗
                    for (int i = 0; i < columns.Count; i++)
                    {
                        results.Add(new AITranslationResult
                        {
                            Success = false,
                            ErrorMessage = "響應JSON缺少columns結構",
                            Paragraphs = new List<TranslatedParagraph>()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"解析多欄位響應失敗: {ex.Message}");

                // 所有欄位標記為失敗
                for (int i = 0; i < columns.Count; i++)
                {
                    results.Add(new AITranslationResult
                    {
                        Success = false,
                        ErrorMessage = $"JSON解析錯誤: {ex.Message}",
                        Paragraphs = new List<TranslatedParagraph>()
                    });
                }
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

                case AIProvider.OpenRouter:
                    // OpenRouter Gemini 2.0 Flash (免費): $0/1M tokens
                    // 注意: 免費模型可能有速率限制
                    return 0;

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
