using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace MonLingo.Core.Service.DualChannel
{
    /// <summary>
    /// 雙通道架構主控制器 v4.0
    /// 將雙通道架構整合到 LayoutAnalysisService 中
    /// </summary>
    public class DualChannelController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private readonly ChannelSelector _channelSelector;
        private readonly BimodalStatisticalChannel _bimodalChannel;
        private readonly ExperienceRuleChannel _experienceChannel;
        private readonly Config _config;
        
        /// <summary>
        /// 內容類型枚舉
        /// </summary>
        private enum ContentType
        {
            SingleLine,      // 單行欄位
            ListItems,       // 列表項目
            ContinuousText   // 連續文本
        }
        
        /// <summary>
        /// v4.0 調試輸出方法
        /// </summary>
        private void DebugLogV4(string message)
        {
            if (_config?.EnableDebugLog == true)
            {
                Console.WriteLine($"🔧 [DualChannel v4.0] {message}");
                Logger.Debug($"[DualChannel v4.0] {message}");
                System.Diagnostics.Debug.WriteLine($"[DualChannel v4.0] {message}");
            }
        }
        
        /// <summary>
        /// 雙通道配置
        /// </summary>
        public class Config
        {
            public ChannelSelector.Config ChannelSelectorConfig { get; set; } = new ChannelSelector.Config();
            public BimodalStatisticalChannel.Config BimodalConfig { get; set; } = new BimodalStatisticalChannel.Config();
            public ExperienceRuleChannel.Config ExperienceConfig { get; set; } = new ExperienceRuleChannel.Config();
            public bool EnableDebugLog { get; set; } = true; // v4.0 默認啟用調試
        }

        public DualChannelController(Config config = null)
        {
            _config = config ?? new Config();
            
            _channelSelector = new ChannelSelector(_config.ChannelSelectorConfig);
            _bimodalChannel = new BimodalStatisticalChannel(_config.BimodalConfig);
            _experienceChannel = new ExperienceRuleChannel(_config.ExperienceConfig);
            
            DebugLogV4("🚀 雙通道架構 v4.0 初始化完成");
            DebugLogV4($"   📊 調試模式: {(_config.EnableDebugLog ? "已啟用" : "已關閉")}");
            Logger.Info("DualChannelController initialized with v4.0 architecture");
        }

        /// <summary>
        /// 處理單個欄位的段落分割
        /// </summary>
        public List<Paragraph> ProcessColumn(Column column)
        {
            var startTime = DateTime.Now;
            
            try
            {
                DebugLogV4($"===============================================");
                DebugLogV4($"🎯 開始處理欄位: {column.ColumnId}");
                DebugLogV4($"📄 輸入行數: {column.Lines.Count}行");
                
                Logger.Info($"Starting dual-channel processing for column with {column.Lines.Count} lines");

                // 步驟1：內容類型預檢查 (Content Type Pre-analysis)
                DebugLogV4($"🔍 階段1: 內容類型預檢查");
                var contentType = AnalyzeContentType(column);
                
                // 快速處理路徑
                if (contentType == ContentType.SingleLine)
                {
                    DebugLogV4($"[Step1] 快速處理模式啟動");
                    var singleLineParagraphs = CreateSingleLineParagraphs(column);
                    var singleProcessingTime = (DateTime.Now - startTime).TotalMilliseconds;
                    
                    DebugLogV4($"[Step1] 創建段落: 共{singleLineParagraphs.Count}個段落");
                    DebugLogV4($"[Step1] 處理完成，跳過通道選擇");
                    DebugLogV4($"✅ 單行處理完成! 耗時: {singleProcessingTime:F2}ms");
                    DebugLogV4($"📊 結果統計: {column.Lines.Count}行 → {singleLineParagraphs.Count}段落");
                    DebugLogV4($"🎯 使用路徑: 單行快速處理");
                    DebugLogV4($"===============================================");
                    DebugLogV4($"");
                    
                    return singleLineParagraphs;
                }
                
                if (contentType == ContentType.ListItems)
                {
                    DebugLogV4($"[Step1] 快速處理模式啟動");
                    var listParagraphs = CreateListParagraphs(column);
                    var listProcessingTime = (DateTime.Now - startTime).TotalMilliseconds;
                    
                    DebugLogV4($"[Step1] 創建段落: 共{listParagraphs.Count}個段落");
                    DebugLogV4($"[Step1] 處理完成，跳過通道選擇");
                    DebugLogV4($"✅ 列表處理完成! 耗時: {listProcessingTime:F2}ms");
                    DebugLogV4($"📊 結果統計: {column.Lines.Count}行 → {listParagraphs.Count}段落");
                    DebugLogV4($"🎯 使用路徑: 列表快速處理");
                    DebugLogV4($"===============================================");
                    DebugLogV4($"");
                    
                    return listParagraphs;
                }

                // 步驟2：智能通道選擇器決策 (僅用於連續文本)
                DebugLogV4($"[Step2] 開始智能通道選擇");
                var decision = _channelSelector.SelectChannel(column);
                
                DebugLogV4($"[Step2] 數據質量評估: 行數={column.Lines.Count}, 有效間距數={decision.SpacingsCount}");
                
                // v4.0 詳細通道選擇調試輸出
                string channelIcon = decision.SelectedChannel == ChannelType.BimodalStatistical ? "📊" : "🎯";
                string channelName = decision.SelectedChannel == ChannelType.BimodalStatistical ? "BimodalStatisticalChannel" : "ExperienceRuleChannel";
                string displayChannelName = decision.SelectedChannel == ChannelType.BimodalStatistical ? "雙峰統計通道" : "經驗規則通道";
                
                DebugLogV4($"[Step2] 通道選擇結果: {channelName}");
                
                // 獲取選擇原因
                string selectionReason = GetChannelSelectionReason(decision);
                DebugLogV4($"[Step2] 選擇原因: {selectionReason}");
                
                if (decision.SelectedChannel == ChannelType.BimodalStatistical)
                {
                    DebugLogV4($"[Step2] → 進入雙峰統計通道 (Step3)");
                }
                else
                {
                    DebugLogV4($"[Step2] → 進入經驗規則通道 (Step4)");
                }
                
                DebugLogV4($"");
                DebugLogV4($"┌─ 【通道選擇決策結果】 ─────────────────");
                DebugLogV4($"│ {channelIcon} 選擇通道: {displayChannelName}");
                DebugLogV4($"│ 📈 數據品質指標:");
                DebugLogV4($"│   • 樣本數量: {decision.SpacingsCount}");
                DebugLogV4($"│   • 有效峰值: {decision.EffectivePeaks}");
                DebugLogV4($"│   • 置信度: {decision.Confidence:F3}");
                
                if (decision.Statistics != null)
                {
                    DebugLogV4($"│ 📏 統計參數:");
                    DebugLogV4($"│   • 全局平均: {decision.Statistics.GlobalMean:F2}px");
                    DebugLogV4($"│   • 標準差: {decision.Statistics.GlobalStdDev:F2}px");
                }
                
                DebugLogV4($"│ 🎯 選擇原因: {selectionReason}");
                DebugLogV4($"└─────────────────────────────────────");
                DebugLogV4($"");

                Logger.Info($"Channel decision: {decision.SelectedChannel}, Confidence: {decision.Confidence:F2}, " +
                           $"SpacingsCount: {decision.SpacingsCount}, EffectivePeaks: {decision.EffectivePeaks}");

                // 步驟3或4：根據決策選擇處理通道
                List<Paragraph> paragraphs;
                
                if (decision.SelectedChannel == ChannelType.BimodalStatistical)
                {
                    DebugLogV4($"⚙️ 執行雙峰統計算法...");
                    paragraphs = _bimodalChannel.Process(column, decision.Statistics);
                }
                else
                {
                    DebugLogV4($"⚙️ 執行經驗規則算法...");
                    paragraphs = _experienceChannel.Process(column, decision.Statistics);
                }

                // 步驟4：後處理驗證和優化
                DebugLogV4($"🔧 階段4: 後處理與驗證");
                paragraphs = PostProcessParagraphs(paragraphs, column, decision);

                var processingTime = (DateTime.Now - startTime).TotalMilliseconds;
                string usedChannelName = decision.SelectedChannel == ChannelType.BimodalStatistical ? "雙峰統計通道" : "經驗規則通道";
                
                DebugLogV4($"");
                DebugLogV4($"✅ 處理完成! 耗時: {processingTime:F2}ms");
                DebugLogV4($"📊 結果統計: {column.Lines.Count}行 → {paragraphs.Count}段落");
                DebugLogV4($"🎯 使用通道: {usedChannelName}");
                DebugLogV4($"===============================================");
                DebugLogV4($"");

                Logger.Info($"Dual-channel processing completed: {paragraphs.Count} paragraphs created");
                return paragraphs;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in dual-channel processing for column");
                
                // 回退到簡單段落創建
                return new List<Paragraph>
                {
                    new Paragraph
                    {
                        ParagraphId = "P1_FALLBACK",
                        Lines = new List<LayoutLine>(column.Lines),
                        BoundingBox = column.BoundingBox,
                        ColumnColor = column.Color,
                        CreatedByChannel = ChannelType.ExperienceRule
                    }
                };
            }
        }

        /// <summary>
        /// 批量處理多個欄位
        /// </summary>
        public Dictionary<string, List<Paragraph>> ProcessColumns(List<Column> columns)
        {
            var results = new Dictionary<string, List<Paragraph>>();
            
            Logger.Info($"Starting batch dual-channel processing for {columns.Count} columns");

            foreach (var column in columns)
            {
                try
                {
                    var paragraphs = ProcessColumn(column);
                    results[column.ColumnId] = paragraphs;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error processing column {column.ColumnId}");
                    
                    // 添加錯誤回退段落
                    results[column.ColumnId] = new List<Paragraph>
                    {
                        new Paragraph
                        {
                            ParagraphId = $"{column.ColumnId}_ERROR",
                            Lines = new List<LayoutLine>(column.Lines),
                            BoundingBox = column.BoundingBox,
                            ColumnColor = column.Color,
                            CreatedByChannel = ChannelType.ExperienceRule
                        }
                    };
                }
            }

            Logger.Info($"Batch processing completed: {results.Count} columns processed");
            return results;
        }

        /// <summary>
        /// 後處理段落優化和驗證
        /// </summary>
        private List<Paragraph> PostProcessParagraphs(List<Paragraph> paragraphs, Column column, ChannelDecision decision)
        {
            // 1. 驗證段落完整性
            ValidateParagraphIntegrity(paragraphs, column);
            
            // 2. 處理空段落
            paragraphs = RemoveEmptyParagraphs(paragraphs);
            
            // 3. 重新編號段落ID
            for (int i = 0; i < paragraphs.Count; i++)
            {
                paragraphs[i].ParagraphId = $"P{i + 1}";
            }
            
            // 4. 添加處理元數據
            foreach (var paragraph in paragraphs)
            {
                paragraph.ChannelConfidence = decision.Confidence;
                paragraph.ProcessingNote = $"Processed by {decision.SelectedChannel} channel";
            }

            Logger.Debug($"Post-processing completed: {paragraphs.Count} paragraphs validated and optimized");
            return paragraphs;
        }

        /// <summary>
        /// 驗證段落完整性
        /// </summary>
        private void ValidateParagraphIntegrity(List<Paragraph> paragraphs, Column column)
        {
            int totalInputLines = column.Lines.Count;
            int totalOutputLines = paragraphs.SelectMany(p => p.Lines).Count();
            
            if (totalInputLines != totalOutputLines)
            {
                Logger.Warn($"Paragraph integrity check failed: Input={totalInputLines}, Output={totalOutputLines}");
            }
            
            // 檢查是否有重複行
            var allLineIds = paragraphs.SelectMany(p => p.Lines.Select(l => l.LineId)).ToList();
            var duplicates = allLineIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            
            if (duplicates.Any())
            {
                Logger.Warn($"Found duplicate lines in paragraphs: {string.Join(", ", duplicates)}");
            }
        }

        /// <summary>
        /// 移除空段落
        /// </summary>
        private List<Paragraph> RemoveEmptyParagraphs(List<Paragraph> paragraphs)
        {
            var validParagraphs = paragraphs.Where(p => p.Lines?.Any() == true).ToList();
            
            if (validParagraphs.Count != paragraphs.Count)
            {
                Logger.Debug($"Removed {paragraphs.Count - validParagraphs.Count} empty paragraphs");
            }
            
            return validParagraphs;
        }

        /// <summary>
        /// 獲取通道選擇原因的詳細說明 (v4.0)
        /// </summary>
        private string GetChannelSelectionReason(ChannelDecision decision)
        {
            if (decision.SelectedChannel == ChannelType.BimodalStatistical)
            {
                return $"統計條件充足 (樣本數≥8, 有效峰值≥2, 置信度{decision.Confidence:F2})";
            }
            else
            {
                var reasons = new List<string>();
                
                if (decision.SpacingsCount < 8)
                    reasons.Add($"樣本不足({decision.SpacingsCount}<8)");
                    
                if (decision.EffectivePeaks < 2)
                    reasons.Add($"峰值不足({decision.EffectivePeaks}<2)");
                
                if (decision.Confidence < 0.5)
                    reasons.Add($"置信度低({decision.Confidence:F2}<0.5)");
                
                return reasons.Any() ? string.Join(", ", reasons) : "其他統計條件不滿足";
            }
        }

        /// <summary>
        /// 獲取處理統計信息
        /// </summary>
        public ProcessingStatistics GetStatistics()
        {
            return new ProcessingStatistics
            {
                ChannelSelectorStats = _channelSelector.GetStatistics(),
                BimodalChannelStats = _bimodalChannel.GetStatistics(),
                // ExperienceChannel 沒有統計 - 根據需要可以添加
            };
        }

        /// <summary>
        /// 重置所有組件統計信息
        /// </summary>
        public void ResetStatistics()
        {
            _channelSelector.ResetStatistics();
            _bimodalChannel.ResetStatistics();
            Logger.Info("All dual-channel statistics reset");
        }
        
        /// <summary>
        /// 內容類型預檢查 (Content Type Pre-analysis)
        /// </summary>
        private ContentType AnalyzeContentType(Column column)
        {
            DebugLogV4($"[Step1] 開始內容類型預分析");
            DebugLogV4($"[Step1] 欄位信息: 行數={column.Lines.Count}, 平均字符數={column.Lines.Average(l => l.Text?.Length ?? 0):F1}");
            
            // 1. 單行欄位捷徑 (Single-Line Shortcut)
            if (column.Lines.Count == 1)
            {
                DebugLogV4($"[Step1] 內容類型判定結果: SingleLine");
                DebugLogV4($"[Step1] 檢測到單行內容，使用快速處理模式");
                Logger.Debug("v4內容分析：單行欄位捷徑");
                return ContentType.SingleLine;
            }

            // 2. 列表項目識別 (List Item Detection)
            int listIndicatorCount = 0;
            foreach (var line in column.Lines)
            {
                var trimmedText = line.Text.Trim();
                if (IsListIndicator(trimmedText))
                {
                    listIndicatorCount++;
                }
            }

            // 如果超過50%的行具有列表特徵，認為是列表
            double listRatio = (double)listIndicatorCount / column.Lines.Count;
            if (listRatio >= 0.5)
            {
                DebugLogV4($"[Step1] 內容類型判定結果: ListItems");
                DebugLogV4($"[Step1] 檢測到列表項目內容，使用快速處理模式");
                Logger.Debug($"v4內容分析：列表項目識別 (列表比例:{listRatio:F2})");
                return ContentType.ListItems;
            }

            // 3. 連續文本處理 (Continuous Text Handling)
            DebugLogV4($"[Step1] 內容類型判定結果: ContinuousText");
            DebugLogV4($"[Step1] 檢測到連續文本內容，需進行通道選擇");
            Logger.Debug("v4內容分析：連續文本處理");
            return ContentType.ContinuousText;
        }
        
        /// <summary>
        /// 檢查文本是否為列表指示符
        /// </summary>
        private bool IsListIndicator(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            // 常見列表符號
            var listPrefixes = new[] { "•", "·", "▪", "▫", "◦", "‣", "⁃", "*", "-", "+" };
            foreach (var prefix in listPrefixes)
            {
                if (text.StartsWith(prefix)) return true;
            }
            
            // 數字編號 (1. 2. 3. 等)
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+\.\s+"))
                return true;
                
            // 字母編號 (a) b) c) 等)
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^[a-zA-Z]\)\s+"))
                return true;
                
            return false;
        }
        
        /// <summary>
        /// 創建單行段落
        /// </summary>
        private List<Paragraph> CreateSingleLineParagraphs(Column column)
        {
            var paragraph = new Paragraph
            {
                ParagraphId = "P1_SINGLE",
                Lines = new List<LayoutLine>(column.Lines),
                BoundingBox = column.BoundingBox,
                ColumnColor = column.Color,
                CreatedByChannel = ChannelType.ExperienceRule,
                ChannelConfidence = 1.0
            };
            
            return new List<Paragraph> { paragraph };
        }
        
        /// <summary>
        /// 創建列表段落（每行一個段落）
        /// </summary>
        private List<Paragraph> CreateListParagraphs(Column column)
        {
            var paragraphs = new List<Paragraph>();
            
            for (int i = 0; i < column.Lines.Count; i++)
            {
                var line = column.Lines[i];
                var paragraph = new Paragraph
                {
                    ParagraphId = $"P{i + 1}_LIST",
                    Lines = new List<LayoutLine> { line },
                    BoundingBox = line.BoundingBox,
                    ColumnColor = column.Color,
                    CreatedByChannel = ChannelType.ExperienceRule,
                    ChannelConfidence = 0.9
                };
                paragraphs.Add(paragraph);
            }
            
            return paragraphs;
        }
    }

    /// <summary>
    /// 雙通道處理統計信息
    /// </summary>
    public class ProcessingStatistics
    {
        public ChannelSelector.Statistics ChannelSelectorStats { get; set; }
        public BimodalStatisticalChannel.Statistics BimodalChannelStats { get; set; }
        
        /// <summary>
        /// 格式化統計信息為可讀字符串
        /// </summary>
        public string FormatSummary()
        {
            var lines = new List<string>
            {
                "=== Dual-Channel Processing Statistics ===",
                "",
                "Channel Selector:",
                $"  Total Decisions: {ChannelSelectorStats?.TotalDecisions ?? 0}",
                $"  Bimodal Selections: {ChannelSelectorStats?.BimodalSelections ?? 0}",
                $"  Experience Selections: {ChannelSelectorStats?.ExperienceSelections ?? 0}",
                "",
                "Bimodal Statistical Channel:",
                $"  Total Processed: {BimodalChannelStats?.TotalProcessed ?? 0}",
                $"  Merge Decisions: {BimodalChannelStats?.MergeDecisions ?? 0}",
                $"  Split Decisions: {BimodalChannelStats?.SplitDecisions ?? 0}",
                $"  Average Score: {BimodalChannelStats?.AverageScore ?? 0:F2}"
            };

            return string.Join(Environment.NewLine, lines);
        }
    }
}