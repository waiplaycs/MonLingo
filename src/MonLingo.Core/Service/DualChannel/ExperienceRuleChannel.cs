using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NLog;

namespace MonLingo.Core.Service.DualChannel
{
    /// <summary>
    /// 經驗規則通道處理器 v4.0
    /// 基於多策略加減分系統進行段落分割決策
    /// </summary>
    public class ExperienceRuleChannel : IExperienceRuleChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 經驗規則通道配置 v4.3
        /// </summary>
        public class Config
        {
            /// <summary>
            /// 高品質數據合併評分閾值 (有效間距 ≥ 6) [v4.3更新]
            /// </summary>
            public double HighQualityMergeThreshold { get; set; } = 2.0;
            
            /// <summary>
            /// 中品質數據合併評分閾值 (有效間距 4-5) [v4.3更新]
            /// </summary>
            public double MediumQualityMergeThreshold { get; set; } = 1.5;
            
            /// <summary>
            /// 低品質數據合併評分閾值 (有效間距 ≤ 3) [v4.3更新]
            /// </summary>
            public double LowQualityMergeThreshold { get; set; } = 1.0;
            
            /// <summary>
            /// 策略三高權重加分 [v4.3更新]
            /// </summary>
            public double Strategy3HighScore { get; set; } = 3.0;
            
            /// <summary>
            /// 策略三中權重加分 [v4.3更新]
            /// </summary>
            public double Strategy3MediumScore { get; set; } = 2.0;
            
            /// <summary>
            /// 策略三低權重加分 [v4.3更新]
            /// </summary>
            public double Strategy3LowScore { get; set; } = 1.0;
            
            /// <summary>
            /// 策略四智能加分 [v4.4更新]
            /// </summary>
            public double Strategy4SmartBonus { get; set; } = 1.5;
            
            /// <summary>
            /// 全局統計係數 - 分割閾值 (預設: 1.0)
            /// </summary>
            public double GlobalSplitFactor { get; set; } = 1.0;
            
            /// <summary>
            /// 全局統計係數 - 合併閾值 (預設: 0.5)
            /// </summary>
            public double GlobalMergeFactor { get; set; } = 0.5;
            
            /// <summary>
            /// 是否啟用調試日誌
            /// </summary>
            public bool EnableDebugLog { get; set; } = true;
        }

        private readonly Config _config;
        private int _currentProcessingTotalLines; // 當前處理的總行數 [v4.4新增]
        
        /// <summary>
        /// v4.0 調試輸出方法
        /// </summary>
        private void DebugLogV4(string message)
        {
            if (_config?.EnableDebugLog == true)
            {
                Console.WriteLine($"🎯 [Step4] {message}");
                Logger.Debug($"[ExperienceRule v4.0] {message}");
                System.Diagnostics.Debug.WriteLine($"[ExperienceRule v4.0] {message}");
            }
        }
        
        public ExperienceRuleChannel(Config config = null)
        {
            _config = config ?? new Config();
        }

        /// <summary>
        /// 合併閾值屬性 (向後兼容) [v4.3更新]
        /// 實際使用動態閾值，此屬性僅用於接口兼容性
        /// </summary>
        public double MergeThreshold { get; set; } = 2.0; // 預設中等品質閾值
        
        /// <summary>
        /// 根據數據品質計算動態合併評分閾值 [v4.3更新]
        /// </summary>
        private double CalculateDynamicMergeThreshold(int effectiveSpacingCount)
        {
            if (effectiveSpacingCount >= 6)
                return _config.HighQualityMergeThreshold;    // 高品質：2.0分
            else if (effectiveSpacingCount >= 4)
                return _config.MediumQualityMergeThreshold;  // 中品質：1.5分
            else
                return _config.LowQualityMergeThreshold;     // 低品質：1.0分
        }

        /// <summary>
        /// 獲取數據品質等級描述 [v4.3更新]
        /// </summary>
        private string GetQualityLevel(int effectiveSpacingCount)
        {
            if (effectiveSpacingCount >= 6)
                return "高品質";
            else if (effectiveSpacingCount >= 4)
                return "中品質";
            else
                return "低品質";
        }

        /// <summary>
        /// 處理經驗規則通道的段落分割 [v4.3更新]
        /// </summary>
        public List<Paragraph> Process(Column column, ChannelStatistics globalStatistics)
        {
            try
            {
                DebugLogV4($"經驗規則通道啟動 [v4.3版本]");
                DebugLogV4($"🚀 v4.3核心改進:");
                DebugLogV4($"  - 動態合併評分閾值 (根據數據品質調整)");
                DebugLogV4($"  - 策略三多級加分機制 (+1.0~+3.0分)");
                DebugLogV4($"  - 平衡的評分系統確保合併決策可達成性");
                
                // 根據實際數據分析觸發條件
                DebugLogV4($"觸發條件分析: 雙峰統計模型失效");
                
                // 計算間距數量進行條件1檢查
                int spacingsCount = Math.Max(0, column.Lines.Count - 1);
                bool condition1_SampleShortage = spacingsCount < 8;
                
                // 檢查條件2: 峰值模式不明顯 
                bool condition2_InsufficientPeaks = (globalStatistics?.EffectivePeaksCount ?? 0) < 2;
                
                // 檢查條件3: 峰值區分度低 (需要計算)
                bool condition3_LowSeparation = false;
                double peakRatio = 0.0;
                if (globalStatistics != null && globalStatistics.PeakMerge > 0.001)
                {
                    peakRatio = globalStatistics.PeakSplit / globalStatistics.PeakMerge;
                    condition3_LowSeparation = peakRatio <= 1.5;
                }
                
                DebugLogV4($"具體觸發條件檢測:");
                DebugLogV4($"- 條件1-樣本數量不足: {condition1_SampleShortage} (間距數:{spacingsCount}, 閾值:8)");
                DebugLogV4($"- 條件2-峰值模式不明顯: {condition2_InsufficientPeaks} (有效峰值:{globalStatistics?.EffectivePeaksCount ?? 0}, 閾值:2)");
                DebugLogV4($"- 條件3-峰值區分度低: {condition3_LowSeparation} (區分度:{peakRatio:F2}, 閾值:1.5)");
                
                // 顯示主要觸發原因
                if (condition1_SampleShortage)
                {
                    DebugLogV4($"💡 主要原因: 統計樣本過少，無法形成有意義的分佈");
                }
                else if (condition2_InsufficientPeaks) 
                {
                    DebugLogV4($"💡 主要原因: 無法找到「段落內」和「段落間」兩種清晰的間距模式");
                }
                else if (condition3_LowSeparation)
                {
                    DebugLogV4($"💡 主要原因: 兩個峰值距離太近，統計上無法有效區分");
                }
                
                DebugLogV4($"開始經驗規則評分");
                Logger.Info($"Starting ExperienceRule processing for column with {column.Lines.Count} lines");
                
                if (column.Lines.Count <= 1)
                {
                    var singleResult = CreateSingleParagraph(column, "單行欄位");
                    DebugLogV4($"經驗規則通道處理完成");
                    DebugLogV4($"創建段落數: {singleResult.Count}");
                    return singleResult;
                }

                // 設置當前處理的總行數 [v4.4更新]
                _currentProcessingTotalLines = column.Lines.Count - 1; // 間距數 = 行數 - 1
                
                // 計算全局統計閾值
                var thresholds = CalculateGlobalThresholds(globalStatistics);
                
                // 進行三策略加減分評分
                var paragraphs = PerformThreeStrategyScoring(column, thresholds, globalStatistics);
                
                DebugLogV4($"經驗規則通道處理完成 [v4.3版本]");
                DebugLogV4($"✅ v4.3成果總結:");
                DebugLogV4($"  - 使用動態合併評分閾值: {CalculateDynamicMergeThreshold(column.Lines.Count - 1):F1}分");
                DebugLogV4($"  - 策略三多級加分機制已應用");
                DebugLogV4($"  - 創建段落數: {paragraphs.Count}");
                DebugLogV4($"  - 平衡評分系統確保決策可達成性");
                
                Logger.Info($"ExperienceRule completed: {paragraphs.Count} paragraphs created");
                return paragraphs;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in ExperienceRule processing");
                return CreateSingleParagraph(column, $"處理錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 計算全局統計閾值
        /// </summary>
        private GlobalThresholds CalculateGlobalThresholds(ChannelStatistics globalStatistics)
        {
            var thresholds = new GlobalThresholds
            {
                SplitThreshold = globalStatistics.GlobalMean + globalStatistics.GlobalStdDev * _config.GlobalSplitFactor,
                MergeThreshold = globalStatistics.GlobalMean - globalStatistics.GlobalStdDev * _config.GlobalMergeFactor
            };

            // 根據文檔輸出詳細的全局統計信息
            DebugLogV4($"全局統計分析:");
            DebugLogV4($"- 平均行距: {globalStatistics.GlobalMean:F1}px");
            DebugLogV4($"- 標準差: {globalStatistics.GlobalStdDev:F1}px (衡量行距一致性程度)");
            DebugLogV4($"閾值計算結果:");
            DebugLogV4($"- 分割閾值: {thresholds.SplitThreshold:F1}px (μ + {_config.GlobalSplitFactor}σ)");
            DebugLogV4($"- 合併閾值: {thresholds.MergeThreshold:F1}px (μ - {_config.GlobalMergeFactor}σ)");

            if (_config.EnableDebugLog)
            {
                Logger.Debug($"Global thresholds: Split={thresholds.SplitThreshold:F1}px, Merge={thresholds.MergeThreshold:F1}px");
            }

            return thresholds;
        }

        /// <summary>
        /// 進行三策略加減分評分 [v4.4更新]
        /// </summary>
        private List<Paragraph> PerformThreeStrategyScoring(Column column, GlobalThresholds thresholds, ChannelStatistics globalStatistics)
        {
            var paragraphs = new List<Paragraph>();
            var currentParagraphLines = new List<LayoutLine> { column.Lines[0] };

            for (int i = 1; i < column.Lines.Count; i++)
            {
                var prevLine = column.Lines[i - 1];
                var currentLine = column.Lines[i];
                
                // 進行三策略評分 - 這裡會輸出詳細的調試信息 [v4.4更新]
                var decision = EvaluateThreeStrategies(prevLine, currentLine, thresholds, i - 1, i, column.Lines.Count, globalStatistics);
                
                if (_config.EnableDebugLog)
                {
                    Logger.Debug($"Line {i}: Score={decision.FinalScore:F2}, Decision={decision.ShouldMerge}, " +
                               $"HardSplit={decision.IsHardSplit}, Detail={decision.Detail}");
                }

                if (decision.ShouldMerge && !decision.IsHardSplit)
                {
                    // 合併到當前段落
                    currentParagraphLines.Add(currentLine);
                }
                else
                {
                    // 創建新段落
                    var paragraph = CreateParagraph(currentParagraphLines, paragraphs.Count, column.Color, decision.Detail);
                    paragraphs.Add(paragraph);
                    
                    // 開始新段落
                    currentParagraphLines = new List<LayoutLine> { currentLine };
                }
            }

            // 處理最後一個段落
            if (currentParagraphLines.Any())
            {
                var lastParagraph = CreateParagraph(currentParagraphLines, paragraphs.Count, column.Color, "最後段落");
                paragraphs.Add(lastParagraph);
            }

            return paragraphs;
        }

        /// <summary>
        /// 評估三個策略並計算最終分數 [v4.4更新]
        /// </summary>
        private ExperienceRuleDecision EvaluateThreeStrategies(LayoutLine prevLine, LayoutLine currentLine, GlobalThresholds thresholds, int prevLineIndex, int currentLineIndex, int totalLineCount, ChannelStatistics globalStatistics)
        {
            var decision = new ExperienceRuleDecision();
            var details = new List<string>();

            // 計算有效間距數量（總行數-1）
            int effectiveSpacingCount = totalLineCount - 1;
            
            // 計算動態合併評分閾值 [v4.3更新]
            double dynamicMergeThreshold = CalculateDynamicMergeThreshold(effectiveSpacingCount);

            // 截取行內容用於顯示（最多20個字符）
            string prevLineText = prevLine.Text?.Length > 20 ? prevLine.Text.Substring(0, 17) + "..." : prevLine.Text ?? "";
            string currentLineText = currentLine.Text?.Length > 20 ? currentLine.Text.Substring(0, 17) + "..." : currentLine.Text ?? "";

            DebugLogV4($"策略評估開始 (行{prevLineIndex}-{currentLineIndex}):");
            DebugLogV4($"- 上行: 「{prevLineText}」");
            DebugLogV4($"- 當行: 「{currentLineText}」");
            DebugLogV4($"- 資料品質: {GetQualityLevel(effectiveSpacingCount)} (有效間距:{effectiveSpacingCount})"); // [v4.3更新]
            DebugLogV4($"- 動態合併評分閾值: {dynamicMergeThreshold:F1}分 [v4.3機制]"); // [v4.3更新]
            
            // 策略一：基於字體層次的扣分
            double strategy1Score = EvaluateStrategy1FontHierarchy(prevLine, currentLine);
            details.Add($"字體:{strategy1Score:F1}");
            
            // 策略二：基於對齊模式的扣分
            double strategy2Score = EvaluateStrategy2Alignment(prevLine, currentLine);
            details.Add($"對齊:{strategy2Score:F1}");
            
            // 策略三：基於統計自適應的合併評分 [v4.4更新]
            var strategy3Result = EvaluateStrategy3Statistical(prevLine, currentLine, thresholds, globalStatistics);
            if (strategy3Result.IsHardSplit)
            {
                decision.IsHardSplit = true;
                decision.ShouldMerge = false;
                decision.Detail = $"硬性分割: {strategy3Result.Reason}";
                DebugLogV4($"- 硬性分割決策: {strategy3Result.Reason}");
                return decision;
            }
            double strategy3Score = strategy3Result.Score;
            details.Add($"統計:{strategy3Score:F1}");

            // 策略四：全局平均智能加分 [v4.4更新]
            double strategy4Score = EvaluateStrategy4GlobalAverage(prevLine, currentLine, strategy3Score, globalStatistics);
            if (strategy4Score > 0)
            {
                details.Add($"智能+{strategy4Score:F1}");
            }

            // 計算最終合併分數 [v4.4更新]
            decision.FinalScore = strategy1Score + strategy2Score + strategy3Score + strategy4Score;
            decision.ShouldMerge = decision.FinalScore >= dynamicMergeThreshold;
            decision.Detail = $"{string.Join(" ", details)} = {decision.FinalScore:F1} (動態閾值:{dynamicMergeThreshold:F1}) [v4.4]";

            DebugLogV4($"- 最終評分: {decision.FinalScore:F1} (動態閾值: {dynamicMergeThreshold:F1}) [v4.4機制]");
            DebugLogV4($"- 決策結果: {(decision.ShouldMerge ? "合併" : "分割")} [四策略評分系統]");

            return decision;
        }

        /// <summary>
        /// 策略一：基於字體層次的扣分 - 漸進式容錯模型
        /// 參考雙峰通道的字體高度懲罰機制，考慮OCR框架誤差
        /// </summary>
        private double EvaluateStrategy1FontHierarchy(LayoutLine prevLine, LayoutLine currentLine)
        {
            double height1 = prevLine.LineHeight;
            double height2 = currentLine.LineHeight;
            
            // 計算相對高度差異百分比
            double heightDiff = Math.Abs(height1 - height2) / Math.Min(height1, height2) * 100;
            
            double score;
            string reason;
            
            // 漸進式懲罰階梯 - 參考雙峰通道設計，但調整為負分
            if (heightDiff <= 10.0)
            {
                score = 0.0;     // OCR微小誤差容錯範圍，無懲罰
                reason = "OCR容錯範圍";
            }
            else if (heightDiff <= 25.0)
            {
                score = -0.1;    // 輕微差異，可能是同字體的OCR變異
                reason = "同字體變異";
            }
            else if (heightDiff <= 50.0)
            {
                score = -0.4;    // 中等差異，可能是相鄰字體級別
                reason = "相鄰字體級別";
            }
            else if (heightDiff <= 80.0)
            {
                score = -0.8;    // 顯著差異，不同字體級別
                reason = "不同字體級別";
            }
            else if (heightDiff <= 120.0)
            {
                score = -1.2;    // 大幅差異，標題與正文級別
                reason = "標題與正文級別";
            }
            else
            {
                score = -2.0;    // 極大差異，強制分割級別
                reason = "極大差異";
            }
            
            DebugLogV4($"- 策略1(字體層次): {height1:F1}px vs {height2:F1}px, 差異:{heightDiff:F1}%, {reason}, 扣分:{score:F1}");
            
            return score;
        }

        /// <summary>
        /// 策略二：基於對齊模式的扣分
        /// </summary>
        private double EvaluateStrategy2Alignment(LayoutLine prevLine, LayoutLine currentLine)
        {
            const double CHARACTER_WIDTH = 12.0; // 估計字符寬度
            
            double leftDiff = Math.Abs(prevLine.BoundingBox.Left - currentLine.BoundingBox.Left);
            
            double score;
            string reason;
            
            if (leftDiff < CHARACTER_WIDTH)
            {
                score = 0.0;     // 無扣分 - 對齊方式一致
                reason = "對齊一致";
            }
            else if (leftDiff < CHARACTER_WIDTH * 2)
            {
                score = -1.0;    // 輕度扣分 - 輕微邊界抖動
                reason = "輕微偏移";
            }
            else
            {
                score = -2.0;    // 重度扣分 - 新的縮排或對齊方式切換
                reason = "對齊模式變化";
            }
            
            DebugLogV4($"- 策略2(對齊模式): 左邊界差異:{leftDiff:F1}px, {reason}, 扣分:{score:F1}");
            
            return score;
        }

        /// <summary>
        /// 策略三：基於統計自適應的多級加分機制 [v4.4更新]
        /// </summary>
        private StrategyResult EvaluateStrategy3Statistical(LayoutLine prevLine, LayoutLine currentLine, GlobalThresholds thresholds, ChannelStatistics globalStatistics)
        {
            var result = new StrategyResult();
            
            // 計算當前行距
            double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
            
            // 計算關鍵閾值點 [v4.3更新]
            double highWeightThreshold = thresholds.MergeThreshold * 0.8;  // 高權重加分邊界
            double splitThresholdReduced = thresholds.SplitThreshold * 0.6; // 分割閾值縮減邊界
            double globalMean = globalStatistics.GlobalMean; // 全局平均值 [v4.4新增]
            
            DebugLogV4($"- 策略3(統計自適應v4.3): 行距:{spacing:F1}px");
            DebugLogV4($"  - 全局平均行距: {globalMean:F1}px (策略四參考基準) [v4.4新增]");
            DebugLogV4($"  - 統計合併間距閾值: {thresholds.MergeThreshold:F1}px (用於判斷行距大小)");
            DebugLogV4($"  - 統計分割間距閾值: {thresholds.SplitThreshold:F1}px (硬性分割邊界)");
            DebugLogV4($"  - 高權重閾值: {highWeightThreshold:F1}px (×0.8)");
            DebugLogV4($"  - 低權重閾值: {splitThresholdReduced:F1}px (×0.6)");
            
            if (spacing > thresholds.SplitThreshold)
            {
                // 硬性規則：直接分割
                result.IsHardSplit = true;
                result.Reason = $"間距過大: {spacing:F1}px > {thresholds.SplitThreshold:F1}px";
                DebugLogV4($"  - 結果: 硬性分割 ({result.Reason})");
            }
            else if (spacing < highWeightThreshold)
            {
                // 高權重合併加分 (+3.0分) [v4.3更新]
                result.Score = _config.Strategy3HighScore;
                result.Reason = $"間距極小: {spacing:F1}px < {highWeightThreshold:F1}px";
                DebugLogV4($"  - 結果: 高權重合併加分+{result.Score:F1} (強烈支援合併) [v4.3]");
            }
            else if (spacing < thresholds.MergeThreshold)
            {
                // 中權重合併加分 (+2.0分) [v4.3更新]
                result.Score = _config.Strategy3MediumScore;
                result.Reason = $"間距偏小: {spacing:F1}px < {thresholds.MergeThreshold:F1}px";
                DebugLogV4($"  - 結果: 中權重合併加分+{result.Score:F1} (適度支援合併) [v4.3]");
            }
            else if (spacing <= splitThresholdReduced)
            {
                // 低權重合併加分 (+1.0分) [v4.3更新]  
                result.Score = _config.Strategy3LowScore;
                result.Reason = $"模糊區間偏向合併: {spacing:F1}px ≤ {splitThresholdReduced:F1}px";
                DebugLogV4($"  - 結果: 低權重合併加分+{result.Score:F1} (模糊區間偏向合併) [v4.3]");
            }
            else
            {
                // 完全模糊區間：無加分 [v4.3更新]
                result.Score = 0.0;
                result.Reason = $"完全模糊區間: {spacing:F1}px";
                DebugLogV4($"  - 結果: 完全模糊區間，無加分 [v4.3]");
            }
            
            return result;
        }

        /// <summary>
        /// 策略四：全局平均智能加分機制 [v4.4更新]
        /// 當策略三給出低權重合併加分(+1.0)且間距≤全局平均時，額外提供+1.5分支持
        /// </summary>
        private double EvaluateStrategy4GlobalAverage(LayoutLine prevLine, LayoutLine currentLine, double strategy3Score, ChannelStatistics globalStatistics)
        {
            // 檢查策略四觸發條件
            if (Math.Abs(strategy3Score - _config.Strategy3LowScore) < 0.001) // strategy3Score == 1.0
            {
                double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
                double globalMean = globalStatistics.GlobalMean;
                
                if (spacing <= globalMean)
                {
                    double strategy4Score = _config.Strategy4SmartBonus;
                    DebugLogV4($"- 策略4(全局平均智能加分)+{strategy4Score:F1} (輔助合併支持) [v4.4新增]");
                    DebugLogV4($"  - 觸發條件: 策略3低權重(+{strategy3Score:F1}) + 間距({spacing:F1}px) ≤ 全局平均({globalMean:F1}px)");
                    return strategy4Score;
                }
            }
            
            return 0.0; // 不滿足條件時不加分
        }

        /// <summary>
        /// 創建單個段落
        /// </summary>
        private List<Paragraph> CreateSingleParagraph(Column column, string reason)
        {
            var paragraph = CreateParagraph(column.Lines, 0, column.Color, reason);
            return new List<Paragraph> { paragraph };
        }

        /// <summary>
        /// 創建段落對象
        /// </summary>
        private Paragraph CreateParagraph(List<LayoutLine> lines, int index, Color columnColor, string detail)
        {
            var boundingBox = CalculateBoundingBox(lines);
            
            return new Paragraph
            {
                ParagraphId = $"P{index + 1}",
                Lines = new List<LayoutLine>(lines),
                BoundingBox = boundingBox,
                ColumnColor = columnColor,
                CreatedByChannel = ChannelType.ExperienceRule
            };
        }

        /// <summary>
        /// 計算邊界框
        /// </summary>
        private Rectangle CalculateBoundingBox(List<LayoutLine> lines)
        {
            if (!lines.Any()) return Rectangle.Empty;
            
            int left = lines.Min(l => l.BoundingBox.Left);
            int top = lines.Min(l => l.BoundingBox.Top);
            int right = lines.Max(l => l.BoundingBox.Right);
            int bottom = lines.Max(l => l.BoundingBox.Bottom);
            
            return new Rectangle(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// 全局統計閾值
        /// </summary>
        private class GlobalThresholds
        {
            public double SplitThreshold { get; set; }
            public double MergeThreshold { get; set; }
        }

        /// <summary>
        /// 策略評估結果
        /// </summary>
        private class StrategyResult
        {
            public double Score { get; set; } = 0.0;
            public bool IsHardSplit { get; set; } = false;
            public string Reason { get; set; } = "";
        }

        /// <summary>
        /// 經驗規則決策結果
        /// </summary>
        private class ExperienceRuleDecision
        {
            public double FinalScore { get; set; }
            public bool ShouldMerge { get; set; }
            public bool IsHardSplit { get; set; }
            public string Detail { get; set; }
        }
    }
}