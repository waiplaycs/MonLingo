using System;
using System.Collections.Generic;
using System.Linq;
using MonLingo.Core.Service;
using NLog;

namespace MonLingo.Core.Service.DualChannel
{
    /// <summary>
    /// 智能通道選擇器 v4.0
    /// 根據數據品質智能選擇雙峰統計通道或經驗規則通道
    /// </summary>
    public class ChannelSelector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 通道選擇閾值配置
        /// </summary>
        public class Config
        {
            /// <summary>
            /// 最小間距數量閾值 (預設: 8)
            /// </summary>
            public int MinSpacingsCount { get; set; } = 8;
            
            /// <summary>
            /// 最小有效峰值數量 (預設: 2)
            /// </summary>
            public int MinEffectivePeaks { get; set; } = 2;
            
            /// <summary>
            /// 最小峰值區分度 (預設: 1.5)
            /// </summary>
            public double MinPeakSeparationRatio { get; set; } = 1.5;
            
            /// <summary>
            /// 聚類閾值係數 (預設: 0.25)
            /// </summary>
            public double ClusterThresholdFactor { get; set; } = 0.25;
        }

        /// <summary>
        /// 通道選擇統計信息
        /// </summary>
        public class Statistics
        {
            /// <summary>
            /// 總決策次數
            /// </summary>
            public int TotalDecisions { get; set; }
            
            /// <summary>
            /// 雙峰統計通道選擇次數
            /// </summary>
            public int BimodalSelections { get; set; }
            
            /// <summary>
            /// 經驗規則通道選擇次數
            /// </summary>
            public int ExperienceSelections { get; set; }
        }

        /// <summary>
        /// 舊版配置類型 (向後兼容)
        /// </summary>
        [Obsolete("請使用 Config 替代")]
        public class ThresholdConfig : Config
        {
        }

        private readonly Config _config;
        private readonly Statistics _statistics = new Statistics();
        
        /// <summary>
        /// v4.0 調試輸出方法
        /// </summary>
        private void DebugLogV4(string message, bool enableDebug = true)
        {
            if (enableDebug)
            {
                Console.WriteLine($"🎯 [ChannelSelector v4.0] {message}");
                Logger.Debug($"[ChannelSelector v4.0] {message}");
                System.Diagnostics.Debug.WriteLine($"[ChannelSelector v4.0] {message}");
            }
        }
        
        public ChannelSelector(Config config = null)
        {
            _config = config ?? new Config();
        }

        /// <summary>
        /// 選擇合適的分析通道
        /// </summary>
        /// <param name="column">欄位數據</param>
        /// <param name="globalStatistics">全局統計數據 (可選)</param>
        /// <returns>通道選擇決策</returns>
        public ChannelDecision SelectChannel(Column column, ChannelStatistics globalStatistics = null)
        {
            var decision = new ChannelDecision();
            var statistics = new ChannelStatistics();
            
            try
            {
                DebugLogV4($"🔍 開始通道選擇分析: {column.ColumnId}");
                DebugLogV4($"📄 輸入數據: {column.Lines.Count}行文字");
                
                // 1. 計算間距數據
                var spacings = CalculateSpacings(column);
                statistics.TotalSpacings = spacings.Count;
                decision.SpacingsCount = spacings.Count;
                
                DebugLogV4($"📏 間距計算: 檢測到{spacings.Count}個行間距");
                
                Logger.Debug($"Column with {column.Lines.Count} lines, {spacings.Count} spacings");
                
                // 2. 檢查條件1: 樣本數量是否足夠
                if (spacings.Count < _config.MinSpacingsCount)
                {
                    DebugLogV4($"❌ 條件1-樣本數量不足: {spacings.Count} < {_config.MinSpacingsCount}");
                    DebugLogV4($"📝 原因: 統計樣本過少，無法形成有意義的分佈");
                    DebugLogV4($"🎯 決策: 切換到經驗規則通道");
                    
                    decision.SelectedChannel = ChannelType.ExperienceRule;
                    decision.Reason = $"樣本數量不足: {spacings.Count} < {_config.MinSpacingsCount}";
                    decision.Statistics = statistics;
                    decision.Confidence = 0.3; // 低置信度
                    decision.EffectivePeaks = 0;
                    return decision;
                }
                
                DebugLogV4($"✅ 條件1-樣本數量檢查: {spacings.Count} ≥ {_config.MinSpacingsCount} (充足)");
                
                // 3. 進行峰值分析
                DebugLogV4($"🔬 開始峰值分析...");
                var peaks = AnalyzePeaks(spacings, column);
                statistics.EffectivePeaksCount = peaks.Count;
                decision.EffectivePeaks = peaks.Count;
                
                DebugLogV4($"📊 峰值檢測結果: 發現{peaks.Count}個有效峰值");
                if (peaks.Count > 0)
                {
                    DebugLogV4($"   🔹 峰值位置: [{string.Join(", ", peaks.Select(p => $"{p:F1}px"))}]");
                }
                
                // 4. 檢查條件2: 峰值模式是否明顯
                if (peaks.Count < _config.MinEffectivePeaks)
                {
                    DebugLogV4($"❌ 條件2-峰值模式不明顯: {peaks.Count} < {_config.MinEffectivePeaks}");
                    DebugLogV4($"📝 原因: 無法找到「段落內」和「段落間」兩種清晰的間距模式");
                    DebugLogV4($"🎯 決策: 切換到經驗規則通道");
                    
                    decision.SelectedChannel = ChannelType.ExperienceRule;
                    decision.Reason = $"峰值模式不明顯: {peaks.Count} < {_config.MinEffectivePeaks}";
                    decision.Statistics = statistics;
                    decision.Confidence = 0.4; // 低置信度
                    return decision;
                }
                
                DebugLogV4($"✅ 條件2-峰值數量檢查: {peaks.Count} ≥ {_config.MinEffectivePeaks} (充足)");
                
                // 5. 檢查條件3: 峰值區分度
                statistics.PeakMerge = peaks.Min();
                statistics.PeakSplit = peaks.Max();
                statistics.PeakSeparationRatio = statistics.PeakSplit / Math.Max(statistics.PeakMerge, 0.001); // 避免除零
                
                DebugLogV4($"📈 峰值區分度分析:");
                DebugLogV4($"   🔹 段落內峰值(peak_merge): {statistics.PeakMerge:F2}px");
                DebugLogV4($"   🔹 段落間峰值(peak_split): {statistics.PeakSplit:F2}px");
                DebugLogV4($"   🔹 區分度比率: {statistics.PeakSeparationRatio:F2}");
                
                if (statistics.PeakSeparationRatio <= _config.MinPeakSeparationRatio)
                {
                    DebugLogV4($"❌ 條件3-峰值區分度低: {statistics.PeakSeparationRatio:F2} ≤ {_config.MinPeakSeparationRatio}");
                    DebugLogV4($"📝 原因: 兩個峰值距離太近，統計上無法有效區分，強行使用會導致決策模糊");
                    DebugLogV4($"🎯 決策: 切換到經驗規則通道");
                    
                    decision.SelectedChannel = ChannelType.ExperienceRule;
                    decision.Reason = $"峰值區分度過低: {statistics.PeakSeparationRatio:F2} ≤ {_config.MinPeakSeparationRatio}";
                    decision.Statistics = statistics;
                    decision.Confidence = 0.5; // 中等置信度
                    return decision;
                }
                
                DebugLogV4($"✅ 條件3-區分度檢查: {statistics.PeakSeparationRatio:F2} > {_config.MinPeakSeparationRatio} (充足)");
                
                // 5. 條件滿足，使用雙峰統計通道
                decision.SelectedChannel = ChannelType.BimodalStatistical;
                decision.Reason = $"統計條件充足: {spacings.Count}間距, {peaks.Count}峰值, 區分度{statistics.PeakSeparationRatio:F2}";
                decision.Statistics = statistics;
                decision.Confidence = Math.Min(0.9, 0.6 + (statistics.PeakSeparationRatio - _config.MinPeakSeparationRatio) * 0.1); // 高置信度
                
                DebugLogV4($"📊 決策: 選擇雙峰統計通道");
                DebugLogV4($"🎯 置信度: {decision.Confidence:F3}");
                
                // 6. 設定全局統計 (如果有的話)
                if (globalStatistics != null)
                {
                    statistics.GlobalMean = globalStatistics.GlobalMean;
                    statistics.GlobalStdDev = globalStatistics.GlobalStdDev;
                }
                
                Logger.Info($"Channel selected: {decision.SelectedChannel}, Reason: {decision.Reason}");
                
                // 更新統計信息
                _statistics.TotalDecisions++;
                if (decision.SelectedChannel == ChannelType.BimodalStatistical)
                {
                    _statistics.BimodalSelections++;
                }
                else
                {
                    _statistics.ExperienceSelections++;
                }
                
                return decision;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in channel selection, falling back to ExperienceRule");
                decision.SelectedChannel = ChannelType.ExperienceRule;
                decision.Reason = $"選擇過程發生錯誤: {ex.Message}";
                decision.Statistics = statistics;
                return decision;
            }
        }

        /// <summary>
        /// 計算欄位內所有相鄰行間距
        /// </summary>
        private List<double> CalculateSpacings(Column column)
        {
            var spacings = new List<double>();
            
            for (int i = 1; i < column.Lines.Count; i++)
            {
                var prevLine = column.Lines[i - 1];
                var currentLine = column.Lines[i];
                
                // 計算垂直間距 (可能為負值，表示重疊)
                double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
                spacings.Add(spacing);
                
                Logger.Trace($"Spacing between line {i-1} and {i}: {spacing:F1}px");
            }
            
            return spacings;
        }

        /// <summary>
        /// 基於密度的一維聚類進行峰值分析
        /// </summary>
        private List<double> AnalyzePeaks(List<double> spacings, Column column)
        {
            if (!spacings.Any()) return new List<double>();
            
            // 計算聚類閾值 - 三層機制
            double avgFontHeight = column.Lines.Average(line => line.BoundingBox.Height);
            var sortedSpacings = spacings.OrderBy(s => s).ToList();
            
            Console.WriteLine($"📊 [Step3] [Step3.1] 🎯 開始聚類閾值計算 | 平均字高={avgFontHeight:F2}px | 間距數量={sortedSpacings.Count}");
            
            double clusterThreshold = CalculateClusterThreshold(sortedSpacings, avgFontHeight);
            
            // 排序間距值已完成
            
            // 聚類分析
            var clusters = new List<List<double>>();
            var currentCluster = new List<double> { sortedSpacings[0] };
            
            for (int i = 1; i < sortedSpacings.Count; i++)
            {
                double diff = sortedSpacings[i] - sortedSpacings[i - 1];
                
                if (diff <= clusterThreshold)
                {
                    // 屬於同一簇
                    currentCluster.Add(sortedSpacings[i]);
                }
                else
                {
                    // 開始新簇
                    clusters.Add(currentCluster);
                    currentCluster = new List<double> { sortedSpacings[i] };
                }
            }
            clusters.Add(currentCluster); // 加入最後一個簇
            
            Console.WriteLine($"📊 [Step3] [Step3.1] 📊 聚類結果 | 總簇數={clusters.Count} | 閾值={clusterThreshold:F2}px");
            
            // 過濾噪音簇並計算峰值
            var peaks = new List<double>();
            foreach (var cluster in clusters)
            {
                if (cluster.Count > 1) // 過濾單點噪音
                {
                    double peak = cluster.Average(); // 簇的均值作為峰值
                    peaks.Add(peak);
                    Console.WriteLine($"📊 [Step3] [Step3.1] ✓ 有效峰值: {peak:F2}px (簇大小: {cluster.Count})");
                }
                else
                {
                    Console.WriteLine($"📊 [Step3] [Step3.1] ✗ 噪音點過濾: {cluster[0]:F2}px");
                }
            }
            
            Console.WriteLine($"📊 [Step3] [Step3.1] ✅ 峰值分析完成 | 有效峰值數={peaks.Count}");
            
            return peaks.OrderBy(p => p).ToList();
        }

        /// <summary>
        /// 計算聚類閾值 - 三層自適應機制
        /// </summary>
        private double CalculateClusterThreshold(List<double> sortedSpacings, double avgFontHeight)
        {
            // 方案1: Gap-based 分析 (尋找最大自然間隙)
            if (sortedSpacings.Count >= 5)
            {
                var gaps = new List<(double gap, int index)>();
                for (int i = 1; i < sortedSpacings.Count; i++)
                {
                    double gap = sortedSpacings[i] - sortedSpacings[i - 1];
                    gaps.Add((gap, i));
                }
                
                var maxGap = gaps.OrderByDescending(g => g.gap).First();
                double gapThreshold = maxGap.gap * 0.5; // 使用最大間隙的50%作為閾值
                
                // 驗證間隙有效性 (最大間隙應大於中位間隙的2倍)
                double medianGap = gaps.Select(g => g.gap).OrderBy(g => g).ElementAt(gaps.Count / 2);
                if (maxGap.gap > medianGap * 2.0)
                {
                    Console.WriteLine($"📊 [Step3] [Step3.1] 🎯 方案1 Gap-based | 最大間隙={maxGap.gap:F2}px(位置{maxGap.index}) | 中位間隙={medianGap:F2}px | 閾值={gapThreshold:F2}px");
                    return gapThreshold;
                }
            }
            
            // 方案2: CV (Coefficient of Variation) 方法
            if (sortedSpacings.Count >= 3)
            {
                double mean = sortedSpacings.Average();
                double variance = sortedSpacings.Select(s => Math.Pow(s - mean, 2)).Average();
                double stdDev = Math.Sqrt(variance);
                double cv = stdDev / mean; // 變異係數
                
                // 根據變異程度選擇係數
                double cvFactor;
                string cvLevel;
                if (cv < 0.5) // 低變異
                {
                    cvFactor = 0.15;
                    cvLevel = "低變異";
                }
                else if (cv < 1.0) // 中等變異
                {
                    cvFactor = 0.25;
                    cvLevel = "中等變異";
                }
                else // 高變異
                {
                    cvFactor = 0.35;
                    cvLevel = "高變異";
                }
                
                double cvThreshold = avgFontHeight * cvFactor;
                Console.WriteLine($"📊 [Step3] [Step3.1] 📊 方案2 CV方法 | CV={cv:F2}({cvLevel}) | 係數={cvFactor:F2} | 閾值={cvThreshold:F2}px");
                return cvThreshold;
            }
            
            // 方案3: 固定後備閾值
            double fallbackThreshold = avgFontHeight * _config.ClusterThresholdFactor;
            Console.WriteLine($"📊 [Step3] [Step3.1] ⚠️ 方案3 固定後備 | 樣本不足(n={sortedSpacings.Count}) | 經典係數={_config.ClusterThresholdFactor:F2} | 閾值={fallbackThreshold:F2}px");
            return fallbackThreshold;
        }

        /// <summary>
        /// 計算全局統計指標
        /// </summary>
        /// <param name="allColumns">所有欄位</param>
        /// <returns>全局統計數據</returns>
        public static ChannelStatistics CalculateGlobalStatistics(IEnumerable<Column> allColumns)
        {
            var allSpacings = new List<double>();
            
            foreach (var column in allColumns)
            {
                for (int i = 1; i < column.Lines.Count; i++)
                {
                    var prevLine = column.Lines[i - 1];
                    var currentLine = column.Lines[i];
                    double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
                    allSpacings.Add(spacing);
                }
            }
            
            if (!allSpacings.Any())
            {
                return new ChannelStatistics();
            }
            
            var mean = allSpacings.Average();
            var variance = allSpacings.Select(s => Math.Pow(s - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            
            Logger.Info($"Global statistics: Mean={mean:F1}px, StdDev={stdDev:F1}px, Samples={allSpacings.Count}");
            
            return new ChannelStatistics
            {
                GlobalMean = mean,
                GlobalStdDev = stdDev,
                TotalSpacings = allSpacings.Count
            };
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
            _statistics.TotalDecisions = 0;
            _statistics.BimodalSelections = 0;
            _statistics.ExperienceSelections = 0;
        }
    }
}