using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NLog;
using LayoutLine = MonLingo.Core.Service.LayoutLine;

namespace MonLingo.Core.Service.DualChannel
{
    /// <summary>
    /// 雙峰統計通道處理器 v4.0
    /// 基於雙峰分佈模型進行段落分割決策
    /// </summary>
    public class BimodalStatisticalChannel : IBimodalStatisticalChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 雙峰統計通道配置
        /// </summary>
        public class Config
        {
            /// <summary>
            /// 容差係數 (預設: 0.2)
            /// </summary>
            public double ToleranceFactor { get; set; } = 0.2;
            
            /// <summary>
            /// 合併閾值 (預設: 2.5) - v4.2後將被智能自適應閾值替代
            /// </summary>
            public double MergeThreshold { get; set; } = 2.5;
            
            /// <summary>
            /// 是否啟用v4.2智能自適應閾值系統
            /// </summary>
            public bool EnableV42AdaptiveThreshold { get; set; } = true;
            
            /// <summary>
            /// 是否啟用調試日誌
            /// </summary>
            public bool EnableDebugLog { get; set; } = true;
        }

        /// <summary>
        /// 雙峰統計通道統計信息
        /// </summary>
        public class Statistics
        {
            /// <summary>
            /// 總處理次數
            /// </summary>
            public int TotalProcessed { get; set; }
            
            /// <summary>
            /// 合併決策次數
            /// </summary>
            public int MergeDecisions { get; set; }
            
            /// <summary>
            /// 分割決策次數
            /// </summary>
            public int SplitDecisions { get; set; }
            
            /// <summary>
            /// 平均得分
            /// </summary>
            public double AverageScore { get; set; }
        }

        private readonly Config _config;
        private readonly Statistics _statistics = new Statistics();
        private readonly LayoutAnalysisService _layoutAnalysisService;

        /// <summary>
        /// v4.0 調試輸出方法
        /// </summary>
        private void DebugLogV4(string message)
        {
            if (_config?.EnableDebugLog == true)
            {
                Console.WriteLine($"📊 [Step3] {message}");
                Logger.Debug($"[BimodalStatistical v4.0] {message}");
                System.Diagnostics.Debug.WriteLine($"[BimodalStatistical v4.0] {message}");
            }
        }

        public BimodalStatisticalChannel(Config config = null, LayoutAnalysisService layoutAnalysisService = null)
        {
            _config = config ?? new Config();
            _layoutAnalysisService = layoutAnalysisService ?? new LayoutAnalysisService();
        }

        /// <summary>
        /// 處理雙峰統計通道的段落分割
        /// </summary>
        public List<Paragraph> Process(Column column, ChannelStatistics statistics)
        {
            try
            {
                DebugLogV4($"雙峰統計通道處理完成");
                Logger.Info($"Starting BimodalStatistical processing for column with {column.Lines.Count} lines");
                
                if (column.Lines.Count <= 1)
                {
                    var singleResult = CreateSingleParagraph(column, "單行欄位");
                    DebugLogV4($"創建段落數: {singleResult.Count}");
                    return singleResult;
                }

                // Step3.1: v4.1聚類閾值計算 (已在ChannelSelector中完成並輸出調試訊息)
                
                // Step3.2: 定義決策區間
                var zones = DefineDecisionZones(statistics);
                DebugLogV4($"[Step3.2] 決策區間劃分:");
                
                // 顯示合併峰值是否為負(重疊情況)
                if (statistics.PeakMerge < 0)
                {
                    DebugLogV4($"[Step3.2] - 合併峰值: {statistics.PeakMerge:F2}px (負值=文字重疊)");
                }
                
                DebugLogV4($"[Step3.2] - 合併區間: [{zones.MergeStart:F2}, {zones.MergeBoundary:F2}] → 固定高分 4.5");
                DebugLogV4($"[Step3.2] - 分割區間: [{zones.SplitBoundary:F2}, ∞] → 固定否決分 -10.0");
                DebugLogV4($"[Step3.2] - 模糊區間: ({zones.AmbiguityStart:F2}, {zones.SplitBoundary:F2}) → 線性遞減評分");
                
                // Step3.4: v4.2智能自適應閾值計算
                DebugLogV4($"[Step3.4] 開始v4.2智能自適應合併閾值計算");
                double adaptiveThreshold = CalculateV42AdaptiveThreshold(column, statistics);
                DebugLogV4($"[Step3.4] v4.2智能自適應閾值: {adaptiveThreshold:F2} (模糊區間中點策略)");
                
                // Step3.3: 進行多指標加權決策 (使用自適應閾值)
                DebugLogV4($"[Step3.3] 開始逐行合併分數計算 (使用v4.2自適應閾值)");
                var paragraphs = PerformMultiCriteriaDecision(column, zones, statistics, adaptiveThreshold);
                
                DebugLogV4($"雙峰統計通道處理完成");
                DebugLogV4($"創建段落數: {paragraphs.Count}");
                DebugLogV4($"平均段落行數: {(paragraphs.Count > 0 ? column.Lines.Count / (double)paragraphs.Count : 0):F1}");
                
                Logger.Info($"BimodalStatistical completed: {paragraphs.Count} paragraphs created");
                return paragraphs;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in BimodalStatistical processing");
                return CreateSingleParagraph(column, $"處理錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 定義決策區間
        /// </summary>
        private DecisionZones DefineDecisionZones(ChannelStatistics statistics)
        {
            // 容差必須為正數 (基於峰值絕對值計算)
            double tolerance = Math.Abs(statistics.PeakMerge) * _config.ToleranceFactor;
            
            // 合併區間、模糊區間計算:
            // 正常情況 (PeakMerge ≥ 0):
            //   - 合併區間: [0, PeakMerge + tolerance]
            //   - 模糊區間: (PeakMerge + tolerance, PeakSplit - tolerance)
            // 重疊情況 (PeakMerge < 0):
            //   - 合併區間: [PeakMerge, 0 + tolerance]
            //   - 模糊區間: (0 + tolerance, PeakSplit - tolerance)
            double mergeStart, mergeBoundary, ambiguityStart;
            
            if (statistics.PeakMerge >= 0)
            {
                // 正常情況: 從0開始
                mergeStart = 0;
                mergeBoundary = statistics.PeakMerge + tolerance;
                ambiguityStart = mergeBoundary; // 模糊區間從合併邊界開始
            }
            else
            {
                // 重疊情況: 從負值開始到0+容差
                mergeStart = statistics.PeakMerge;
                mergeBoundary = 0 + tolerance;
                ambiguityStart = 0 + tolerance; // 模糊區間從0+容差開始
            }
            
            var zones = new DecisionZones
            {
                MergeStart = mergeStart,
                MergeBoundary = mergeBoundary,
                AmbiguityStart = ambiguityStart,
                SplitBoundary = statistics.PeakSplit - tolerance,
                Tolerance = tolerance
            };

            if (_config.EnableDebugLog)
            {
                Logger.Debug($"Decision zones: Merge[{zones.MergeStart:F1}, {zones.MergeBoundary:F1}], " +
                           $"Split[{zones.SplitBoundary:F1}, ∞), " +
                           $"Ambiguity({zones.AmbiguityStart:F1}, {zones.SplitBoundary:F1})");
            }

            return zones;
        }

        /// <summary>
        /// 進行多指標加權決策 (v4.2版本 - 使用自適應閾值)
        /// </summary>
        private List<Paragraph> PerformMultiCriteriaDecision(Column column, DecisionZones zones, ChannelStatistics statistics, double adaptiveThreshold)
        {
            var paragraphs = new List<Paragraph>();
            var currentParagraphLines = new List<LayoutLine> { column.Lines[0] };
            var decisionCount = 0;

            for (int i = 1; i < column.Lines.Count; i++)
            {
                var prevLine = column.Lines[i - 1];
                var currentLine = column.Lines[i];
                
                // 計算合併分數
                var decision = CalculateMergeScore(prevLine, currentLine, zones);
                decisionCount++;
                
                // 輸出詳細的分數計算過程
                double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
                DebugLogV4($"[Step3.3] 行{i-1}-{i}: 間距={spacing:F1}");
                
                // 獲取分數組成部分
                double distanceScore = CalculateDistanceScore(spacing, zones);
                double fontPenalty = CalculateFontHeightPenalty(prevLine, currentLine);
                double alignmentPenalty = CalculateAlignmentPenalty(prevLine, currentLine);
                
                string intervalType = GetIntervalType(spacing, zones);
                double heightDiff = Math.Abs(prevLine.LineHeight - currentLine.LineHeight) / Math.Min(prevLine.LineHeight, currentLine.LineHeight) * 100;
                
                DebugLogV4($"[Step3.3] - 雙峰距離得分: {distanceScore:F1} (區間: {intervalType})");
                DebugLogV4($"[Step3.3] - 字體高度懲罰: {fontPenalty:F1} (差異: {heightDiff:F1}%)");
                DebugLogV4($"[Step3.3] - 對齊風格懲罰: {alignmentPenalty:F1}");
                DebugLogV4($"[Step3.3] - 最終合併分數: {decision.Score:F1}");

                // v4.2決策執行 - 使用自適應閾值
                bool shouldMergeAdaptive = decision.Score >= adaptiveThreshold && !decision.IsHardSplit;
                string mergeDecision = shouldMergeAdaptive ? "合併" : "分割";
                DebugLogV4($"[Step3.3] 行{i-1}-{i}: 分數={decision.Score:F2} vs v4.2閾值={adaptiveThreshold:F2} → {mergeDecision}");

                if (shouldMergeAdaptive)
                {
                    // v4.2合併到當前段落 (使用自適應閾值決策)
                    currentParagraphLines.Add(currentLine);
                }
                else
                {
                    // v4.2創建新段落 (自適應閾值分割決策)
                    var paragraph = CreateParagraph(currentParagraphLines, paragraphs.Count, column.Color, 
                        $"{decision.Detail} [v4.2自適應閾值={adaptiveThreshold:F2}]");
                    paragraphs.Add(paragraph);
                    
                    // 開始新段落
                    currentParagraphLines = new List<LayoutLine> { currentLine };
                }
                
                if (_config.EnableDebugLog)
                {
                    Logger.Debug($"Line {i}: Score={decision.Score:F2}, Decision={decision.ShouldMerge}, Detail={decision.Detail}");
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
        /// 計算合併分數
        /// </summary>
        private MergeDecision CalculateMergeScore(LayoutLine prevLine, LayoutLine currentLine, DecisionZones zones)
        {
            // 計算垂直間距
            double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
            
            // 1. 雙峰驅動距離得分
            double distanceScore = CalculateDistanceScore(spacing, zones);
            
            // 2. 字體高度懲罰
            double fontPenalty = CalculateFontHeightPenalty(prevLine, currentLine);
            
            // 3. 對齊風格懲罰
            double alignmentPenalty = CalculateAlignmentPenalty(prevLine, currentLine);
            
            // 計算最終分數
            double finalScore = distanceScore - fontPenalty - alignmentPenalty;
            
            var decision = new MergeDecision
            {
                Score = finalScore,
                ShouldMerge = finalScore >= _config.MergeThreshold,
                IsHardSplit = distanceScore == -10.0, // 強制分割信號
                Detail = $"距離:{distanceScore:F1} - 字體:{fontPenalty:F1} - 對齊:{alignmentPenalty:F1} = {finalScore:F1}"
            };

            return decision;
        }

        /// <summary>
        /// 雙峰驅動距離得分計算
        /// </summary>
        private double CalculateDistanceScore(double spacing, DecisionZones zones)
        {
            // 1. 強分割信號：落入分割區間
            if (spacing >= zones.SplitBoundary)
            {
                return -10.0; // 固定否決分，強制分割
            }
            
            // 2. 強合併信號：落入合併區間（包含負值重疊）
            if (spacing <= zones.MergeBoundary)
            {
                return 4.5; // 固定高分，強烈合併信號
            }
            
            // 3. 模糊區間：線性遞減評分 (從AmbiguityStart到SplitBoundary)
            double zoneWidth = zones.SplitBoundary - zones.AmbiguityStart;
            
            if (zoneWidth <= 0)
            {
                return 2.0; // 當兩個區間重疊時的默認分數
            }
            
            // 線性遞減：從模糊區間起點的4.0分遞減到分割邊界的0.5分
            double distanceFromAmbiguityStart = spacing - zones.AmbiguityStart;
            double normalizedPosition = distanceFromAmbiguityStart / zoneWidth; // [0, 1]
            double score = 4.0 - (normalizedPosition * 3.5); // 4.0 → 0.5 線性遞減
            
            return score;
        }

        /// <summary>
        /// 字體高度懲罰計算
        /// </summary>
        private double CalculateFontHeightPenalty(LayoutLine prevLine, LayoutLine currentLine)
        {
            double height1 = prevLine.LineHeight;
            double height2 = currentLine.LineHeight;
            
            // 計算相對高度差異百分比
            double heightDiff = Math.Abs(height1 - height2) / Math.Min(height1, height2) * 100;
            
            if (heightDiff <= 10.0)
                return 0.0;        // OCR 微小誤差容錯範圍
            else if (heightDiff <= 25.0)
                return 0.1;        // 輕微差異，可能是同字體的 OCR 變異
            else if (heightDiff <= 50.0)
                return 0.4;        // 中等差異，可能是相鄰字體級別
            else if (heightDiff <= 80.0)
                return 0.8;        // 顯著差異，不同字體級別
            else if (heightDiff <= 120.0)
                return 1.2;        // 大幅差異，標題與正文級別
            else
                return 2.0;        // 極大差異，強制分割級別
        }

        /// <summary>
        /// 對齊風格懲罰計算
        /// </summary>
        private double CalculateAlignmentPenalty(LayoutLine prevLine, LayoutLine currentLine)
        {
            const int ALIGNMENT_TOLERANCE = 10; // 對齊容差（像素）
            
            int leftDiff = Math.Abs(prevLine.BoundingBox.Left - currentLine.BoundingBox.Left);
            
            return leftDiff <= ALIGNMENT_TOLERANCE ? 0.0 : 0.2;
        }

        /// <summary>
        /// 獲取間距所屬的區間類型
        /// </summary>
        private string GetIntervalType(double spacing, DecisionZones zones)
        {
            if (spacing >= zones.SplitBoundary)
                return "分割區間";
            else if (spacing <= zones.MergeBoundary)
                return "合併區間";
            else if (spacing > zones.AmbiguityStart)
                return "模糊區間";
            else
                return "合併區間"; // 介於MergeBoundary和AmbiguityStart之間 (重疊情況下不存在)
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
                CreatedByChannel = ChannelType.BimodalStatistical
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
        /// 決策區間定義
        /// </summary>
        private class DecisionZones
        {
            public double MergeStart { get; set; }      // 合併區間起點 (可能為負值)
            public double MergeBoundary { get; set; }   // 合併區間終點
            public double AmbiguityStart { get; set; }  // 模糊區間起點
            public double SplitBoundary { get; set; }   // 分割區間起點
            public double Tolerance { get; set; }       // 容差值
        }

        /// <summary>
        /// 合併決策結果
        /// </summary>
        private class MergeDecision
        {
            public double Score { get; set; }
            public bool ShouldMerge { get; set; }
            public bool IsHardSplit { get; set; }
            public string Detail { get; set; }
        }

        /// <summary>
        /// 獲取統計信息
        /// </summary>
        public Statistics GetStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// 重置統計信息
        /// </summary>
        public void ResetStatistics()
        {
            _statistics.TotalProcessed = 0;
            _statistics.MergeDecisions = 0;
            _statistics.SplitDecisions = 0;
            _statistics.AverageScore = 0.0;
        }

        /// <summary>
        /// v4.2智能自適應合併閾值計算
        /// <summary>
        /// v4.2智能自適應合併閾值計算
        /// 核心策略：返回模糊區間中點對應的評分值(2.25)作為合併閾值
        /// 
        /// 設計理念：
        /// 步驟3.3的模糊區間評分系統：score = 4.0 - (normalized_position × 3.5)
        /// - 合併邊界(0%位置) → 評分4.0
        /// - 分割邊界(100%位置) → 評分0.5
        /// - 中點(50%位置) → 評分2.25 ← 這就是v4.2閾值
        /// 
        /// 當間距位於模糊區間50%位置時：
        /// normalized_position = 0.5
        /// score = 4.0 - (0.5 × 3.5) = 2.25
        /// 
        /// 決策邏輯：
        /// - 評分 ≥ 2.25 → 更接近合併邊界 → 執行合併
        /// - 評分 < 2.25 → 更接近分割邊界 → 執行分割
        /// </summary>
        private double CalculateV42AdaptiveThreshold(Column column, ChannelStatistics statistics)
        {
            try
            {
                if (!_config.EnableV42AdaptiveThreshold)
                {
                    DebugLogV4($"[Step3.4] v4.2自適應閾值已禁用，使用固定閾值: {_config.MergeThreshold}");
                    return _config.MergeThreshold;
                }

                // v4.2核心策略：模糊區間中點評分閾值
                // 基於步驟3.3的線性評分公式：score = 4.0 - (normalized_position × 3.5)
                // 當 normalized_position = 0.5 (模糊區間中點):
                // score = 4.0 - (0.5 × 3.5) = 4.0 - 1.75 = 2.25
                const double AMBIGUITY_ZONE_MIDPOINT_SCORE = 2.25;

                DebugLogV4($"[Step3.4] v4.2合併閾值: {AMBIGUITY_ZONE_MIDPOINT_SCORE} (模糊區間中點評分)");
                DebugLogV4($"[Step3.4] - 說明: 模糊區間評分範圍[4.0, 0.5]的中點值");
                DebugLogV4($"[Step3.4] - 決策邏輯: 評分≥{AMBIGUITY_ZONE_MIDPOINT_SCORE}→合併 | 評分<{AMBIGUITY_ZONE_MIDPOINT_SCORE}→分割");
                
                return AMBIGUITY_ZONE_MIDPOINT_SCORE;
            }
            catch (Exception ex)
            {
                DebugLogV4($"[Step3.4] ⚠️ v4.2智能閾值計算異常，回退到固定閾值: {ex.Message}");
                Logger.Warn($"V4.2 adaptive threshold calculation failed: {ex.Message}");
                return _config.MergeThreshold;
            }
        }
    }
}