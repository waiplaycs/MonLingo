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

                // Step3.1: v4.1聚類閾值計算 (模擬，因為這部分在ChannelSelector中已經完成)
                DebugLogV4($"[Step3.1] 開始v4.1聚類閾值計算");
                DebugLogV4($"[Step3.1] 雙峰識別結果:");
                DebugLogV4($"[Step3.1] - 合併峰值: {statistics.PeakMerge:F2}, 權重: N/A");
                DebugLogV4($"[Step3.1] - 分割峰值: {statistics.PeakSplit:F2}, 權重: N/A");
                DebugLogV4($"[Step3.1] - 有效峰值數量: {statistics.EffectivePeaksCount}");

                // Step3.2: 定義決策區間
                var zones = DefineDecisionZones(statistics);
                DebugLogV4($"[Step3.2] 決策區間劃分:");
                DebugLogV4($"[Step3.2] - 合併區間: [0, {zones.MergeBoundary:F2}] → 固定高分 4.5");
                DebugLogV4($"[Step3.2] - 分割區間: [{zones.SplitBoundary:F2}, ∞] → 固定否決分 -10.0");
                DebugLogV4($"[Step3.2] - 模糊區間: ({zones.MergeBoundary:F2}, {zones.SplitBoundary:F2}) → 線性遞減評分");
                
                // Step3.4: v4.2智能自適應閾值計算
                DebugLogV4($"[Step3.4] 開始v4.2智能自適應合併閾值計算");
                double adaptiveThreshold = CalculateV42AdaptiveThreshold(column, statistics);
                DebugLogV4($"[Step3.4] v4.2智能自適應閾值: {adaptiveThreshold:F2} (方案4.1-4.5組合)");
                
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
            double tolerance = statistics.PeakMerge * _config.ToleranceFactor;
            
            var zones = new DecisionZones
            {
                MergeBoundary = statistics.PeakMerge + tolerance,
                SplitBoundary = statistics.PeakSplit - tolerance,
                Tolerance = tolerance
            };

            if (_config.EnableDebugLog)
            {
                Logger.Debug($"Decision zones: Merge[0, {zones.MergeBoundary:F1}], " +
                           $"Split[{zones.SplitBoundary:F1}, ∞), " +
                           $"Ambiguity({zones.MergeBoundary:F1}, {zones.SplitBoundary:F1})");
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
                DebugLogV4($"[Step3.3] 行{i-1}-{i}: 分數={decision.Score:F1} vs v4.2閾值={adaptiveThreshold:F1} → {mergeDecision}");

                if (shouldMergeAdaptive)
                {
                    // v4.2合併到當前段落 (使用自適應閾值決策)
                    currentParagraphLines.Add(currentLine);
                }
                else
                {
                    // v4.2創建新段落 (自適應閾值分割決策)
                    var paragraph = CreateParagraph(currentParagraphLines, paragraphs.Count, column.Color, 
                        $"{decision.Detail} [v4.2自適應閾值={adaptiveThreshold:F1}]");
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
            
            // 3. 模糊區間：線性遞減評分
            double zoneWidth = zones.SplitBoundary - zones.MergeBoundary;
            
            if (zoneWidth <= 0)
            {
                return 2.0; // 當兩個區間重疊時的默認分數
            }
            
            // 線性遞減：從合併邊界的4.0分遞減到分割邊界的0.5分
            double distanceFromMerge = spacing - zones.MergeBoundary;
            double normalizedPosition = distanceFromMerge / zoneWidth; // [0, 1]
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
            else
                return "模糊區間";
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
            public double MergeBoundary { get; set; }
            public double SplitBoundary { get; set; }
            public double Tolerance { get; set; }
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
        /// 集成文檔定義的方案4.1-4.5智能閾值系統
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

                // 使用Column中已有的LayoutLine列表 (無需轉換)
                var layoutLines = column.Lines;

                // 構建BiPeakSpacingModel
                var biPeakModel = new LayoutAnalysisService.BiPeakSpacingModel
                {
                    PeakMerge = statistics.PeakMerge,
                    PeakSplit = statistics.PeakSplit,
                    Tolerance = statistics.PeakMerge * _config.ToleranceFactor,
                    ValidPeaks = statistics.EffectivePeaksCount,
                    TotalSpacings = Math.Max(0, layoutLines.Count - 1)
                };

                // 實現v4.2智能自適應閾值計算的核心邏輯
                var adaptiveThreshold = CalculateV42AdaptiveThresholdCore(layoutLines, biPeakModel);

                DebugLogV4($"[Step3.4] v4.2智能閾值完成: {adaptiveThreshold:F2} (替代固定閾值{_config.MergeThreshold})");
                DebugLogV4($"[Step3.4] - 方案4.1-4.5組合: 統計驅動 + 變異調整 + 可信度評分 + 約束範圍 + 異常處理");
                
                return adaptiveThreshold;
            }
            catch (Exception ex)
            {
                DebugLogV4($"[Step3.4] ⚠️ v4.2智能閾值計算異常，回退到固定閾值: {ex.Message}");
                Logger.Warn($"V4.2 adaptive threshold calculation failed: {ex.Message}");
                return _config.MergeThreshold;
            }
        }

        /// <summary>
        /// v4.2智能自適應閾值計算核心邏輯
        /// 實現文檔定義的方案4.1-4.5組合系統
        /// </summary>
        private double CalculateV42AdaptiveThresholdCore(List<LayoutLine> layoutLines, LayoutAnalysisService.BiPeakSpacingModel biPeakModel)
        {
            try
            {
                // 收集間距數據
                var spacings = new List<double>();
                for (int i = 1; i < layoutLines.Count; i++)
                {
                    double spacing = layoutLines[i].BoundingBox.Top - layoutLines[i - 1].BoundingBox.Bottom;
                    spacings.Add(spacing);
                }

                if (spacings.Count < 2)
                    return _config.MergeThreshold;

                // 方案4.1：基於統計分佈的動態基礎閾值
                double baseThreshold = CalculateStatisticalBaseThreshold(spacings, biPeakModel.PeakMerge);
                DebugLogV4($"[Step3.4] - 方案4.1 基礎閾值: {baseThreshold:F2}");

                // 方案4.2：基於變異係數的分離度調整
                double separationFactor = CalculateVariationCoefficientSeparation(spacings, biPeakModel);
                DebugLogV4($"[Step3.4] - 方案4.2 分離調整: {separationFactor:F2}");

                // 方案4.3：連續型可信度評分系統
                double confidenceFactor = CalculateContinuousConfidenceScore(spacings, biPeakModel);
                DebugLogV4($"[Step3.4] - 方案4.3 可信度調整: {confidenceFactor:F2}");

                // 計算初步閾值
                double preliminaryThreshold = baseThreshold + separationFactor + confidenceFactor;

                // 方案4.4：基於文檔類型的動態約束範圍
                double adaptiveThreshold = ApplyDocumentTypeConstraints(preliminaryThreshold, layoutLines, spacings);
                DebugLogV4($"[Step3.4] - 方案4.4 約束後閾值: {adaptiveThreshold:F2}");

                // 方案4.5：多層驗證和異常處理機制
                adaptiveThreshold = ValidateAndHandleAnomalies(adaptiveThreshold, spacings, biPeakModel);
                DebugLogV4($"[Step3.4] - 方案4.5 最終閾值: {adaptiveThreshold:F2}");

                return adaptiveThreshold;
            }
            catch (Exception ex)
            {
                DebugLogV4($"[Step3.4] v4.2核心計算異常: {ex.Message}");
                return _config.MergeThreshold;
            }
        }

        /// <summary>
        /// 方案4.1：基於統計分佈的動態基礎閾值
        /// </summary>
        private double CalculateStatisticalBaseThreshold(List<double> spacings, double peakMerge)
        {
            if (spacings.Count < 2)
                return Math.Max(0.5, peakMerge * 1.2);

            var sortedSpacings = spacings.OrderBy(x => x).ToList();
            double p25 = GetPercentile(sortedSpacings, 0.25);
            double p75 = GetPercentile(sortedSpacings, 0.75);
            double iqr = p75 - p25;

            return Math.Max(0.5, Math.Min(peakMerge + iqr * 0.5, peakMerge * 2.0));
        }

        /// <summary>
        /// 方案4.2：基於變異係數的分離度調整
        /// </summary>
        private double CalculateVariationCoefficientSeparation(List<double> spacings, LayoutAnalysisService.BiPeakSpacingModel biPeakModel)
        {
            double cv = CalculateCoefficientOfVariation(spacings);
            double peakSeparation = biPeakModel.PeakSplit - biPeakModel.PeakMerge;
            double normalizedSeparation = peakSeparation / (biPeakModel.PeakMerge + 1.0);
            
            return Math.Tanh(normalizedSeparation) * cv * 0.4;
        }

        /// <summary>
        /// 方案4.3：連續型可信度評分系統
        /// </summary>
        private double CalculateContinuousConfidenceScore(List<double> spacings, LayoutAnalysisService.BiPeakSpacingModel biPeakModel)
        {
            double sampleScore = Math.Min(1.0, (biPeakModel.TotalSpacings - 2) / 8.0);
            double peakQuality = CalculatePeakSeparationQuality(biPeakModel);
            double confidenceScore = (sampleScore + peakQuality) / 2.0;
            
            return (confidenceScore - 0.5) * 0.6;
        }

        /// <summary>
        /// 方案4.4：基於文檔類型的動態約束範圍
        /// </summary>
        private double ApplyDocumentTypeConstraints(double preliminaryThreshold, List<LayoutLine> layoutLines, List<double> spacings)
        {
            double avgFontSize = layoutLines.Average(line => line.LineHeight);
            double minThreshold = Math.Max(0.2, avgFontSize * 0.05);
            double maxThreshold = Math.Min(10.0, avgFontSize * 0.8);
            
            return Math.Max(minThreshold, Math.Min(preliminaryThreshold, maxThreshold));
        }

        /// <summary>
        /// 方案4.5：多層驗證和異常處理機制
        /// </summary>
        private double ValidateAndHandleAnomalies(double adaptiveThreshold, List<double> spacings, LayoutAnalysisService.BiPeakSpacingModel biPeakModel)
        {
            if (spacings.Count > 0 && adaptiveThreshold > spacings.Max() * 1.5)
            {
                adaptiveThreshold = spacings.Average() * 1.2;
                DebugLogV4($"[Step3.4] 閾值異常調整: 降低到 {adaptiveThreshold:F2}");
            }

            if (adaptiveThreshold < 0.1 || adaptiveThreshold > 15.0)
            {
                adaptiveThreshold = Math.Max(0.5, Math.Min(adaptiveThreshold, 10.0));
                DebugLogV4($"[Step3.4] 極端值約束: 調整到 {adaptiveThreshold:F2}");
            }

            return adaptiveThreshold;
        }

        /// <summary>
        /// 計算百分位數
        /// </summary>
        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0;
            if (sortedValues.Count == 1) return sortedValues[0];
            
            double index = percentile * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);
            
            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];
            
            double weight = index - lowerIndex;
            return sortedValues[lowerIndex] * (1 - weight) + sortedValues[upperIndex] * weight;
        }

        /// <summary>
        /// 計算變異係數
        /// </summary>
        private double CalculateCoefficientOfVariation(List<double> values)
        {
            if (values.Count < 2) return 0;
            
            double mean = values.Average();
            if (Math.Abs(mean) < 1e-10) return 0;
            
            double variance = values.Sum(x => Math.Pow(x - mean, 2)) / values.Count;
            double stdDev = Math.Sqrt(variance);
            
            return stdDev / Math.Abs(mean);
        }

        /// <summary>
        /// 計算峰值分離質量評分
        /// </summary>
        private double CalculatePeakSeparationQuality(LayoutAnalysisService.BiPeakSpacingModel biPeakModel)
        {
            if (biPeakModel.ValidPeaks < 2)
                return 0.0;

            double separation = biPeakModel.PeakSplit - biPeakModel.PeakMerge;
            double relativeSeparation = separation / (biPeakModel.PeakMerge + 1.0);
            
            double separationQuality = Math.Tanh(relativeSeparation / 2.0);
            double peakCountQuality = Math.Min(1.0, biPeakModel.ValidPeaks / 3.0);
            
            return (separationQuality + peakCountQuality) / 2.0;
        }
    }
}