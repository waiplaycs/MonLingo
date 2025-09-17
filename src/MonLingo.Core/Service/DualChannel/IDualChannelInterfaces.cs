using System;
using System.Collections.Generic;
using System.Drawing;
using MonLingo.Core.Service; // 引用主服務命名空間

namespace MonLingo.Core.Service.DualChannel
{
    /// <summary>
    /// 雙通道決策架構 v4.0 - 核心介面定義
    /// 根據數據品質智能選擇分析通道：雙峰統計通道 vs 經驗規則通道
    /// </summary>
    
    /// <summary>
    /// 欄位數據結構 (Column) - 用於雙通道分析
    /// </summary>
    public class Column
    {
        /// <summary>
        /// 欄位內的文字行列表 (使用主服務的LayoutLine)
        /// </summary>
        public List<MonLingo.Core.Service.LayoutLine> Lines { get; set; } = new List<MonLingo.Core.Service.LayoutLine>();
        
        /// <summary>
        /// 欄位邊界框
        /// </summary>
        public Rectangle BoundingBox { get; set; }
        
        /// <summary>
        /// 欄位標識符
        /// </summary>
        public string ColumnId { get; set; }
        
        /// <summary>
        /// 欄位顏色 (用於調試視覺化)
        /// </summary>
        public Color Color { get; set; }
    }

    /// <summary>
    /// 段落數據結構 (Paragraph) - 雙通道分析輸出
    /// </summary>
    public class Paragraph
    {
        /// <summary>
        /// 段落標識符
        /// </summary>
        public string ParagraphId { get; set; }
        
        /// <summary>
        /// 行標識符 (for debugging purposes)
        /// </summary>
        public string LineId { get; set; }
        
        /// <summary>
        /// 段落內的文字行 (使用主服務的LayoutLine)
        /// </summary>
        public List<MonLingo.Core.Service.LayoutLine> Lines { get; set; } = new List<MonLingo.Core.Service.LayoutLine>();
        
        /// <summary>
        /// 段落邊界框
        /// </summary>
        public Rectangle BoundingBox { get; set; }
        
        /// <summary>
        /// 所屬欄位顏色
        /// </summary>
        public Color ColumnColor { get; set; }
        
        /// <summary>
        /// 用於創建段落的通道類型
        /// </summary>
        public ChannelType CreatedByChannel { get; set; }
        
        /// <summary>
        /// 通道處理的置信度
        /// </summary>
        public double? ChannelConfidence { get; set; }
        
        /// <summary>
        /// 處理註釋
        /// </summary>
        public string ProcessingNote { get; set; }
    }
    
    /// <summary>
    /// 通道選擇結果
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        /// 雙峰統計通道 - 用於統計樣本充足的高可信度數據
        /// </summary>
        BimodalStatistical,
        
        /// <summary>
        /// 經驗規則通道 - 用於統計樣本不足的低可信度數據
        /// </summary>
        ExperienceRule
    }

    /// <summary>
    /// 通道選擇決策結果
    /// </summary>
    public class ChannelDecision
    {
        /// <summary>
        /// 選擇的通道類型
        /// </summary>
        public ChannelType SelectedChannel { get; set; }
        
        /// <summary>
        /// 決策原因
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// 統計指標
        /// </summary>
        public ChannelStatistics Statistics { get; set; }
        
        /// <summary>
        /// 決策置信度 (0.0 - 1.0)
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// 間距樣本數量
        /// </summary>
        public int SpacingsCount { get; set; }
        
        /// <summary>
        /// 有效峰值數量
        /// </summary>
        public int EffectivePeaks { get; set; }
    }

    /// <summary>
    /// 通道選擇統計指標
    /// </summary>
    public class ChannelStatistics
    {
        /// <summary>
        /// 總間距數量
        /// </summary>
        public int TotalSpacings { get; set; }
        
        /// <summary>
        /// 有效峰值數量
        /// </summary>
        public int EffectivePeaksCount { get; set; }
        
        /// <summary>
        /// 峰值區分度 (peak_split / peak_merge)
        /// </summary>
        public double PeakSeparationRatio { get; set; }
        
        /// <summary>
        /// 合併峰值 (最小峰值)
        /// </summary>
        public double PeakMerge { get; set; }
        
        /// <summary>
        /// 分割峰值 (最大峰值)
        /// </summary>
        public double PeakSplit { get; set; }
        
        /// <summary>
        /// 全局統計平均值
        /// </summary>
        public double GlobalMean { get; set; }
        
        /// <summary>
        /// 全局統計標準差
        /// </summary>
        public double GlobalStdDev { get; set; }
    }

    /// <summary>
    /// 段落分割決策結果
    /// </summary>
    public class ParagraphDecision
    {
        /// <summary>
        /// 是否應該合併到同一段落
        /// </summary>
        public bool ShouldMerge { get; set; }
        
        /// <summary>
        /// 最終合併分數
        /// </summary>
        public double FinalScore { get; set; }
        
        /// <summary>
        /// 使用的通道
        /// </summary>
        public ChannelType UsedChannel { get; set; }
        
        /// <summary>
        /// 決策明細 (用於調試)
        /// </summary>
        public string DecisionDetail { get; set; }
        
        /// <summary>
        /// 是否為硬性分割
        /// </summary>
        public bool IsHardSplit { get; set; }
    }

    /// <summary>
    /// 雙通道段落分析器介面
    /// </summary>
    public interface IDualChannelParagraphAnalyzer
    {
        /// <summary>
        /// 分析欄位並選擇合適的處理通道
        /// </summary>
        /// <param name="column">欄位數據</param>
        /// <returns>通道選擇決策</returns>
        ChannelDecision SelectChannel(Column column);
        
        /// <summary>
        /// 進行段落分割分析
        /// </summary>
        /// <param name="column">欄位數據</param>
        /// <param name="channelDecision">通道選擇結果</param>
        /// <returns>段落分割結果</returns>
        List<Paragraph> AnalyzeParagraphs(Column column, ChannelDecision channelDecision);
    }

    /// <summary>
    /// 雙峰統計通道處理器介面
    /// </summary>
    public interface IBimodalStatisticalChannel
    {
        /// <summary>
        /// 雙峰統計分析
        /// </summary>
        /// <param name="column">欄位數據</param>
        /// <param name="statistics">統計指標</param>
        /// <returns>段落分割結果</returns>
        List<Paragraph> Process(Column column, ChannelStatistics statistics);
    }

    /// <summary>
    /// 經驗規則通道處理器介面
    /// </summary>
    public interface IExperienceRuleChannel
    {
        /// <summary>
        /// 經驗規則分析
        /// </summary>
        /// <param name="column">欄位數據</param>
        /// <param name="globalStatistics">全局統計數據</param>
        /// <returns>段落分割結果</returns>
        List<Paragraph> Process(Column column, ChannelStatistics globalStatistics);
        
        /// <summary>
        /// 設定合併閾值
        /// </summary>
        double MergeThreshold { get; set; }
    }
}