using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
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
        /// 經驗規則通道配置
        /// </summary>
        public class Config
        {
            /// <summary>
            /// 合併閾值 T_merge (預設: 3.0)
            /// </summary>
            public double MergeThreshold { get; set; } = 3.0;
            
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
            public bool EnableDebugLog { get; set; } = false;
        }

        private readonly Config _config;
        
        /// <summary>
        /// 列表標識符正則表達式
        /// </summary>
        private static readonly Regex ListIndicatorRegex = new Regex(
            @"^\s*([1-9]\d*[\.\)]\s|[a-zA-Z][\.\)]\s|[•\-\*■□○●▪▫‣⁃]\s)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public ExperienceRuleChannel(Config config = null)
        {
            _config = config ?? new Config();
        }

        /// <summary>
        /// 合併閾值屬性
        /// </summary>
        public double MergeThreshold 
        { 
            get => _config.MergeThreshold; 
            set => _config.MergeThreshold = value; 
        }

        /// <summary>
        /// 處理經驗規則通道的段落分割
        /// </summary>
        public List<Paragraph> Process(Column column, ChannelStatistics globalStatistics)
        {
            try
            {
                Logger.Info($"Starting ExperienceRule processing for column with {column.Lines.Count} lines");
                
                if (column.Lines.Count <= 1)
                {
                    return CreateSingleParagraph(column, "單行欄位");
                }

                // 計算全局統計閾值
                var thresholds = CalculateGlobalThresholds(globalStatistics);
                
                // 進行四策略加減分評分
                var paragraphs = PerformFourStrategyScoring(column, thresholds);
                
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

            if (_config.EnableDebugLog)
            {
                Logger.Debug($"Global thresholds: Split={thresholds.SplitThreshold:F1}px, Merge={thresholds.MergeThreshold:F1}px");
            }

            return thresholds;
        }

        /// <summary>
        /// 進行四策略加減分評分
        /// </summary>
        private List<Paragraph> PerformFourStrategyScoring(Column column, GlobalThresholds thresholds)
        {
            var paragraphs = new List<Paragraph>();
            var currentParagraphLines = new List<LayoutLine> { column.Lines[0] };

            for (int i = 1; i < column.Lines.Count; i++)
            {
                var prevLine = column.Lines[i - 1];
                var currentLine = column.Lines[i];
                
                // 進行四策略評分
                var decision = EvaluateFourStrategies(prevLine, currentLine, thresholds);
                
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
        /// 評估四個策略並計算最終分數
        /// </summary>
        private ExperienceRuleDecision EvaluateFourStrategies(LayoutLine prevLine, LayoutLine currentLine, GlobalThresholds thresholds)
        {
            var decision = new ExperienceRuleDecision();
            var details = new List<string>();

            // 策略一：基於字體層次的扣分
            double strategy1Score = EvaluateStrategy1FontHierarchy(prevLine, currentLine);
            details.Add($"字體:{strategy1Score:F1}");
            
            // 策略二：基於對齊模式的扣分
            double strategy2Score = EvaluateStrategy2Alignment(prevLine, currentLine);
            details.Add($"對齊:{strategy2Score:F1}");
            
            // 策略三：基於內容語義的硬性分割檢查
            var strategy3Result = EvaluateStrategy3Semantic(currentLine);
            if (strategy3Result.IsHardSplit)
            {
                decision.IsHardSplit = true;
                decision.ShouldMerge = false;
                decision.Detail = $"硬性分割: {strategy3Result.Reason}";
                return decision;
            }
            double strategy3Score = strategy3Result.Score;
            details.Add($"語義:{strategy3Score:F1}");
            
            // 策略四：基於統計自適應的合併評分
            var strategy4Result = EvaluateStrategy4Statistical(prevLine, currentLine, thresholds);
            if (strategy4Result.IsHardSplit)
            {
                decision.IsHardSplit = true;
                decision.ShouldMerge = false;
                decision.Detail = $"硬性分割: {strategy4Result.Reason}";
                return decision;
            }
            double strategy4Score = strategy4Result.Score;
            details.Add($"統計:{strategy4Score:F1}");

            // 計算最終合併分數
            decision.FinalScore = strategy1Score + strategy2Score + strategy3Score + strategy4Score;
            decision.ShouldMerge = decision.FinalScore >= _config.MergeThreshold;
            decision.Detail = $"{string.Join(" ", details)} = {decision.FinalScore:F1} (閾值:{_config.MergeThreshold})";

            return decision;
        }

        /// <summary>
        /// 策略一：基於字體層次的扣分
        /// </summary>
        private double EvaluateStrategy1FontHierarchy(LayoutLine prevLine, LayoutLine currentLine)
        {
            double height1 = prevLine.LineHeight;
            double height2 = currentLine.LineHeight;
            
            // 計算相對高度差異百分比
            double heightDiff = Math.Abs(height1 - height2) / Math.Min(height1, height2) * 100;
            
            if (heightDiff <= 5.0)
                return 0.0;     // 無扣分
            else if (heightDiff <= 15.0)
                return -1.0;    // 輕度扣分
            else
                return -2.0;    // 重度扣分
        }

        /// <summary>
        /// 策略二：基於對齊模式的扣分
        /// </summary>
        private double EvaluateStrategy2Alignment(LayoutLine prevLine, LayoutLine currentLine)
        {
            const double CHARACTER_WIDTH = 12.0; // 估計字符寬度
            
            double leftDiff = Math.Abs(prevLine.BoundingBox.Left - currentLine.BoundingBox.Left);
            
            if (leftDiff < CHARACTER_WIDTH)
                return 0.0;     // 無扣分 - 對齊方式一致
            else if (leftDiff < CHARACTER_WIDTH * 2)
                return -1.0;    // 輕度扣分 - 輕微邊界抖動
            else
                return -2.0;    // 重度扣分 - 新的縮排或對齊方式切換
        }

        /// <summary>
        /// 策略三：基於內容語義的硬性分割檢查
        /// </summary>
        private StrategyResult EvaluateStrategy3Semantic(LayoutLine currentLine)
        {
            var result = new StrategyResult();
            
            if (ListIndicatorRegex.IsMatch(currentLine.Text))
            {
                result.IsHardSplit = true;
                result.Reason = $"列表標識符: {currentLine.Text.Substring(0, Math.Min(10, currentLine.Text.Length))}...";
            }
            else
            {
                result.Score = 0.0; // 未觸發時視為 0
                result.Reason = "無語義線索";
            }
            
            return result;
        }

        /// <summary>
        /// 策略四：基於統計自適應的合併評分
        /// </summary>
        private StrategyResult EvaluateStrategy4Statistical(LayoutLine prevLine, LayoutLine currentLine, GlobalThresholds thresholds)
        {
            var result = new StrategyResult();
            
            // 計算當前行距
            double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
            
            if (spacing > thresholds.SplitThreshold)
            {
                // 硬性規則：直接分割
                result.IsHardSplit = true;
                result.Reason = $"間距過大: {spacing:F1}px > {thresholds.SplitThreshold:F1}px";
            }
            else if (spacing < thresholds.MergeThreshold)
            {
                // 合併加分
                result.Score = 1.0;
                result.Reason = $"間距偏小: {spacing:F1}px < {thresholds.MergeThreshold:F1}px";
            }
            else
            {
                // 模糊區間：無加分
                result.Score = 0.0;
                result.Reason = $"模糊區間: {spacing:F1}px";
            }
            
            return result;
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