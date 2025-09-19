using System;
using System.Collections.Generic;
using System.Linq;
using MonLingo.Core.Service.DualChannel;
using NLog;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// LayoutAnalysisService 雙通道架構調試擴展 v4.0
    /// 專門為雙通道決策架構提供詳細的終端調試輸出
    /// </summary>
    public partial class LayoutAnalysisService
    {
        /// <summary>
        /// v4.1雙通道調試日誌輸出 (含自適應聚類閾值)
        /// </summary>
        /// <param name="message">調試訊息</param>
        private void DebugLogV4(string message)
        {
            if (EnableDebugMode)
            {
                // 輸出到控制台和日誌
                Console.WriteLine($"[MonLingo v4.1雙通道+自適應閾值] {message}");
                Logger.Debug($"[MonLingo v4.1雙通道+自適應閾值] {message}");
                
                // 同時輸出到系統調試輸出
                System.Diagnostics.Debug.WriteLine($"[MonLingo v4.1雙通道+自適應閾值] {message}");
            }
        }

        /// <summary>
        /// v4.0雙通道調試：通道選擇器決策過程
        /// </summary>
        /// <param name="columnId">欄位ID</param>
        /// <param name="decision">通道決策結果</param>
        private void DebugChannelSelection(string columnId, ChannelDecision decision)
        {
            if (EnableDebugMode)
            {
                string channelIcon = decision.SelectedChannel == ChannelType.BimodalStatistical ? "📊" : "🎯";
                DebugLogV4($"{channelIcon} 通道選擇: {columnId} → {GetChannelDisplayName(decision.SelectedChannel)}");
                DebugLogV4($"   📈 數據品質: 樣本數={decision.SpacingsCount}, 峰值數={decision.EffectivePeaks}, 置信度={decision.Confidence:F2}");
                
                if (decision.Statistics != null)
                {
                    DebugLogV4($"   📏 全局統計: 平均={decision.Statistics.GlobalMean:F1}px, 標準差={decision.Statistics.GlobalStdDev:F1}px");
                }
                
                // 顯示選擇原因
                string reason = GetChannelSelectionReason(decision);
                DebugLogV4($"   🎯 選擇原因: {reason}");
            }
        }

        /// <summary>
        /// v4.0雙通道調試：雙峰統計通道處理過程
        /// </summary>
        /// <param name="columnId">欄位ID</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="spacing">當前行距</param>
        /// <param name="distanceScore">距離得分</param>
        /// <param name="fontPenalty">字體懲罰</param>
        /// <param name="alignmentPenalty">對齊懲罰</param>
        /// <param name="finalScore">最終分數</param>
        /// <param name="threshold">閾值</param>
        /// <param name="decision">是否合併</param>
        /// <param name="zone">所在區域</param>
        private void DebugBimodalProcessing(string columnId, int lineIndex, double spacing, double distanceScore, 
            double fontPenalty, double alignmentPenalty, double finalScore, double threshold, bool decision, string zone)
        {
            if (EnableDebugMode)
            {
                string decisionIcon = decision ? "🔗" : "✂️";
                string decisionText = decision ? "合併" : "分割";
                
                DebugLogV4($"📊 雙峰統計: {columnId} 行{lineIndex} | 間距:{spacing:F1}px [{zone}區域]");
                DebugLogV4($"   🧮 評分詳情: 距離:{distanceScore:F1} - 字體:{fontPenalty:F1} - 對齊:{alignmentPenalty:F1} = {finalScore:F1}/{threshold:F1}");
                DebugLogV4($"   {decisionIcon} 決策結果: {decisionText}");
            }
        }

        /// <summary>
        /// v4.0雙通道調試：經驗規則通道三策略評分
        /// </summary>
        /// <param name="columnId">欄位ID</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="strategy1Score">策略一得分（字體層次）</param>
        /// <param name="strategy2Score">策略二得分（對齊模式）</param>
        /// <param name="strategy3Score">策略三得分（統計自適應）</param>
        /// <param name="finalScore">最終合併分數</param>
        /// <param name="threshold">合併閾值</param>
        /// <param name="decision">是否合併</param>
        /// <param name="isHardSplit">是否硬性分割</param>
        private void DebugExperienceRuleProcessing(string columnId, int lineIndex, double strategy1Score, 
            double strategy2Score, double strategy3Score, double finalScore, 
            double threshold, bool decision, bool isHardSplit)
        {
            if (EnableDebugMode)
            {
                string decisionIcon = decision ? "🔗" : "✂️";
                string decisionText = decision ? "合併" : "分割";
                
                if (isHardSplit)
                {
                    DebugLogV4($"🎯 經驗規則: {columnId} 行{lineIndex} | ⚡硬性分割觸發");
                    return;
                }
                
                DebugLogV4($"🎯 經驗規則: {columnId} 行{lineIndex} | 三策略評分");
                DebugLogV4($"   📝 策略得分: 字體:{strategy1Score:F1} + 對齊:{strategy2Score:F1} + 統計:{strategy3Score:F1} = {finalScore:F1}");
                DebugLogV4($"   {decisionIcon} 決策結果: {decisionText} (閾值:{threshold:F1})");
            }
        }

        /// <summary>
        /// v4.0雙通道調試：峰值分析結果
        /// </summary>
        /// <param name="columnId">欄位ID</param>
        /// <param name="spacings">間距列表</param>
        /// <param name="peaks">檢測到的峰值</param>
        /// <param name="peakMerge">合併峰值</param>
        /// <param name="peakSplit">分割峰值</param>
        /// <param name="tolerance">容差</param>
        private void DebugPeakAnalysis(string columnId, List<double> spacings, List<double> peaks, 
            double peakMerge, double peakSplit, double tolerance)
        {
            if (EnableDebugMode)
            {
                DebugLogV4($"🔬 峰值分析: {columnId} | {spacings.Count}個間距 → {peaks.Count}個峰值");
                
                if (peaks.Count >= 2)
                {
                    DebugLogV4($"   📊 關鍵峰值: 合併={peakMerge:F1}px, 分割={peakSplit:F1}px, 容差={tolerance:F1}px");
                    DebugLogV4($"   🟢 合併區域: [0, {peakMerge + tolerance:F1}px]");
                    DebugLogV4($"   🔴 分割區域: [{peakSplit - tolerance:F1}px, ∞)");
                    DebugLogV4($"   🟡 模糊區域: ({peakMerge + tolerance:F1}px, {peakSplit - tolerance:F1}px)");
                }
                else
                {
                    DebugLogV4($"   ⚠️ 峰值不足: 僅檢測到{peaks.Count}個有效峰值，將切換到經驗規則通道");
                }
                
                if (spacings.Count > 0)
                {
                    DebugLogV4($"   📈 間距分佈: 範圍=[{spacings.Min():F1}, {spacings.Max():F1}]px, 平均={spacings.Average():F1}px");
                }
            }
        }

        /// <summary>
        /// v4.0雙通道調試：處理階段摘要
        /// </summary>
        /// <param name="stageName">階段名稱</param>
        /// <param name="inputCount">輸入數量</param>
        /// <param name="outputCount">輸出數量</param>
        /// <param name="processingTimeMs">處理時間</param>
        /// <param name="channelStats">通道統計</param>
        private void DebugStageV4Summary(string stageName, int inputCount, int outputCount, 
            double processingTimeMs, Dictionary<ChannelType, int> channelStats = null)
        {
            if (EnableDebugMode)
            {
                DebugLogV4($"⚡ v4.0階段完成: {stageName}");
                DebugLogV4($"   📊 處理統計: {inputCount}→{outputCount} | {processingTimeMs:F2}ms");
                
                if (channelStats != null && channelStats.Any())
                {
                    var bimodalCount = channelStats.ContainsKey(ChannelType.BimodalStatistical) ? channelStats[ChannelType.BimodalStatistical] : 0;
                    var experienceCount = channelStats.ContainsKey(ChannelType.ExperienceRule) ? channelStats[ChannelType.ExperienceRule] : 0;
                    
                    DebugLogV4($"   🎯 通道分佈: 雙峰統計={bimodalCount}, 經驗規則={experienceCount}");
                    
                    if (bimodalCount > 0 && experienceCount > 0)
                    {
                        var bimodalRatio = (double)bimodalCount / (bimodalCount + experienceCount) * 100;
                        DebugLogV4($"   📈 雙峰統計佔比: {bimodalRatio:F1}%");
                    }
                }
            }
        }

        /// <summary>
        /// v4.0雙通道調試：統計信息摘要
        /// </summary>
        /// <param name="statistics">處理統計信息</param>
        private void DebugDualChannelStatistics(ProcessingStatistics statistics)
        {
            if (EnableDebugMode && statistics != null)
            {
                DebugLogV4("📊 === 雙通道架構統計摘要 ===");
                
                if (statistics.ChannelSelectorStats != null)
                {
                    var selectorStats = statistics.ChannelSelectorStats;
                    DebugLogV4($"🎯 通道選擇器: 總決策={selectorStats.TotalDecisions}");
                    DebugLogV4($"   📊 雙峰統計選擇: {selectorStats.BimodalSelections}次");
                    DebugLogV4($"   🎯 經驗規則選擇: {selectorStats.ExperienceSelections}次");
                    
                    if (selectorStats.TotalDecisions > 0)
                    {
                        var bimodalRatio = (double)selectorStats.BimodalSelections / selectorStats.TotalDecisions * 100;
                        DebugLogV4($"   📈 雙峰統計使用率: {bimodalRatio:F1}%");
                    }
                }
                
                if (statistics.BimodalChannelStats != null)
                {
                    var bimodalStats = statistics.BimodalChannelStats;
                    DebugLogV4($"📊 雙峰統計通道: 處理={bimodalStats.TotalProcessed}次");
                    DebugLogV4($"   🔗 合併決策: {bimodalStats.MergeDecisions}次");
                    DebugLogV4($"   ✂️ 分割決策: {bimodalStats.SplitDecisions}次");
                    DebugLogV4($"   🎯 平均得分: {bimodalStats.AverageScore:F2}");
                }
                
                DebugLogV4("📊 ========================");
            }
        }

        /// <summary>
        /// v4.0雙通道調試：錯誤回退情況
        /// </summary>
        /// <param name="columnId">欄位ID</param>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <param name="fallbackMethod">回退方法</param>
        private void DebugDualChannelFallback(string columnId, string errorMessage, string fallbackMethod)
        {
            if (EnableDebugMode)
            {
                DebugLogV4($"⚠️ 雙通道錯誤: {columnId}");
                DebugLogV4($"   ❌ 錯誤: {errorMessage}");
                DebugLogV4($"   🔄 回退: 使用{fallbackMethod}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// 獲取通道顯示名稱
        /// </summary>
        private string GetChannelDisplayName(ChannelType channelType)
        {
            return channelType switch
            {
                ChannelType.BimodalStatistical => "雙峰統計通道",
                ChannelType.ExperienceRule => "經驗規則通道",
                _ => "未知通道"
            };
        }

        /// <summary>
        /// 獲取通道選擇原因
        /// </summary>
        private string GetChannelSelectionReason(ChannelDecision decision)
        {
            if (decision.SelectedChannel == ChannelType.BimodalStatistical)
            {
                return $"統計充足 (樣本≥8, 峰值≥2, 區分度足夠)";
            }
            else
            {
                var reasons = new List<string>();
                
                if (decision.SpacingsCount < 8)
                    reasons.Add($"樣本不足({decision.SpacingsCount}<8)");
                    
                if (decision.EffectivePeaks < 2)
                    reasons.Add($"峰值不足({decision.EffectivePeaks}<2)");
                
                // 這裡可以添加更多具體的觸發原因
                
                return reasons.Any() ? string.Join(", ", reasons) : "其他統計條件不滿足";
            }
        }

        #endregion
    }
}

namespace MonLingo.Core.Service.DualChannel
{
    /// <summary>
    /// 雙通道架構組件的調試擴展
    /// </summary>
    public static class DualChannelDebugExtensions
    {
        /// <summary>
        /// 為雙通道控制器添加詳細調試輸出
        /// </summary>
        /// <param name="controller">雙通道控制器</param>
        /// <param name="message">調試訊息</param>
        /// <param name="enableDebug">是否啟用調試</param>
        public static void DebugLog(this DualChannelController controller, string message, bool enableDebug = true)
        {
            if (enableDebug)
            {
                Console.WriteLine($"[雙通道控制器] {message}");
                LogManager.GetCurrentClassLogger().Debug($"[雙通道控制器] {message}");
                System.Diagnostics.Debug.WriteLine($"[雙通道控制器] {message}");
            }
        }

        /// <summary>
        /// 為通道選擇器添加詳細調試輸出
        /// </summary>
        /// <param name="selector">通道選擇器</param>
        /// <param name="message">調試訊息</param>
        /// <param name="enableDebug">是否啟用調試</param>
        public static void DebugLog(this ChannelSelector selector, string message, bool enableDebug = true)
        {
            if (enableDebug)
            {
                Console.WriteLine($"[通道選擇器] {message}");
                LogManager.GetCurrentClassLogger().Debug($"[通道選擇器] {message}");
                System.Diagnostics.Debug.WriteLine($"[通道選擇器] {message}");
            }
        }

        /// <summary>
        /// 為雙峰統計通道添加詳細調試輸出
        /// </summary>
        /// <param name="channel">雙峰統計通道</param>
        /// <param name="message">調試訊息</param>
        /// <param name="enableDebug">是否啟用調試</param>
        public static void DebugLog(this BimodalStatisticalChannel channel, string message, bool enableDebug = true)
        {
            if (enableDebug)
            {
                Console.WriteLine($"[雙峰統計通道] {message}");
                LogManager.GetCurrentClassLogger().Debug($"[雙峰統計通道] {message}");
                System.Diagnostics.Debug.WriteLine($"[雙峰統計通道] {message}");
            }
        }

        /// <summary>
        /// 為經驗規則通道添加詳細調試輸出
        /// </summary>
        /// <param name="channel">經驗規則通道</param>
        /// <param name="message">調試訊息</param>
        /// <param name="enableDebug">是否啟用調試</param>
        public static void DebugLog(this ExperienceRuleChannel channel, string message, bool enableDebug = true)
        {
            if (enableDebug)
            {
                Console.WriteLine($"[經驗規則通道] {message}");
                LogManager.GetCurrentClassLogger().Debug($"[經驗規則通道] {message}");
                System.Diagnostics.Debug.WriteLine($"[經驗規則通道] {message}");
            }
        }
    }
}