using System;
using System.Collections.Generic;
using System.Linq;
using MonLingo.Core.Service.DualChannel;
using NLog;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// LayoutAnalysisService 雙通道架構擴展 v4.0
    /// 將雙通道決策架構無縫整合到現有版面分析流程中
    /// </summary>
    public partial class LayoutAnalysisService
    {
        private static readonly Logger DualChannelLogger = LogManager.GetLogger("DualChannel");
        
        /// <summary>
        /// 雙通道控制器
        /// </summary>
        private DualChannelController _dualChannelController;
        
        /// <summary>
        /// 是否啟用雙通道架構 (預設為 true)
        /// </summary>
        public bool EnableDualChannel { get; set; } = true;
        
        /// <summary>
        /// 雙通道配置
        /// </summary>
        public DualChannelController.Config DualChannelConfig { get; set; } = new DualChannelController.Config();

        /// <summary>
        /// 初始化雙通道架構
        /// </summary>
        private void InitializeDualChannel()
        {
            if (_dualChannelController == null)
            {
                _dualChannelController = new DualChannelController(DualChannelConfig);
                DualChannelLogger.Info("Dual-channel architecture initialized for LayoutAnalysisService");
            }
        }

        /// <summary>
        /// v4.0 段落分割 - 使用雙通道決策架構
        /// 替代原有的 PerformParagraphSegmentation 方法
        /// </summary>
        /// <param name="columns">欄位列表</param>
        /// <returns>段落分割結果</returns>
        public Dictionary<string, List<LayoutParagraph>> PerformParagraphSegmentationV4(List<List<LayoutLine>> columns)
        {
            if (!EnableDualChannel)
            {
                // 回退到原有 v3 實現
                DualChannelLogger.Info("Dual-channel disabled, falling back to v3 implementation");
                return PerformParagraphSegmentation(columns);
            }

            var startTime = DateTime.UtcNow;
            var layoutResult = new Dictionary<string, List<LayoutParagraph>>();

            try
            {
                InitializeDualChannel();
                
                DualChannelLogger.Info($"Starting v4.0 dual-channel paragraph segmentation for {columns.Count} columns");
                DebugLog("📑 v4階段三：開始雙通道段落檢測");

                // 轉換欄位格式為雙通道架構所需的格式
                var dualChannelColumns = ConvertToColumnFormat(columns);
                
                // 使用雙通道架構處理所有欄位
                var dualChannelResults = _dualChannelController.ProcessColumns(dualChannelColumns);
                
                // 轉換結果格式回原有的 LayoutParagraph 格式
                layoutResult = ConvertFromDualChannelResults(dualChannelResults);
                
                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                DualChannelLogger.Info($"v4.0 dual-channel segmentation completed in {processingTime:F2}ms: " +
                                     $"{layoutResult.Count} columns, {layoutResult.Values.SelectMany(p => p).Count()} paragraphs");
                
                DebugLog($"✅ v4階段三完成：生成 {layoutResult.Count} 個欄位的段落結構 (雙通道架構)");
                
                // 輸出統計信息
                LogDualChannelStatistics();
                
                return layoutResult;
            }
            catch (Exception ex)
            {
                DualChannelLogger.Error(ex, "Error in v4.0 dual-channel paragraph segmentation");
                
                // 發生錯誤時回退到 v3 實現
                DualChannelLogger.Info("Falling back to v3 implementation due to error");
                DebugLog("⚠️ v4雙通道處理錯誤，回退到v3實現");
                
                return PerformParagraphSegmentation(columns);
            }
        }

        /// <summary>
        /// 轉換欄位格式為雙通道架構格式
        /// </summary>
        private List<Column> ConvertToColumnFormat(List<List<LayoutLine>> columns)
        {
            var dualChannelColumns = new List<Column>();
            
            for (int i = 0; i < columns.Count; i++)
            {
                var columnLines = columns[i];
                if (!columnLines.Any()) continue;
                
                // 計算欄位邊界框
                var boundingBox = CalculateDualChannelColumnBoundingBox(columnLines);
                
                var column = new Column
                {
                    ColumnId = $"C{i + 1}",
                    Lines = columnLines,
                    BoundingBox = boundingBox,
                    Color = GetColumnColor(i)
                };
                
                dualChannelColumns.Add(column);
                
                DualChannelLogger.Debug($"Converted column {i + 1}: {columnLines.Count} lines, bounds={boundingBox}");
            }
            
            return dualChannelColumns;
        }

        /// <summary>
        /// 計算欄位邊界框（雙通道版本）
        /// </summary>
        private System.Drawing.Rectangle CalculateDualChannelColumnBoundingBox(List<LayoutLine> lines)
        {
            if (!lines.Any()) return System.Drawing.Rectangle.Empty;
            
            int left = lines.Min(l => l.BoundingBox.Left);
            int top = lines.Min(l => l.BoundingBox.Top);
            int right = lines.Max(l => l.BoundingBox.Right);
            int bottom = lines.Max(l => l.BoundingBox.Bottom);
            
            return new System.Drawing.Rectangle(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// 轉換雙通道結果回原有格式
        /// </summary>
        private Dictionary<string, List<LayoutParagraph>> ConvertFromDualChannelResults(
            Dictionary<string, List<Paragraph>> dualChannelResults)
        {
            var layoutResult = new Dictionary<string, List<LayoutParagraph>>();
            
            foreach (var kvp in dualChannelResults)
            {
                var columnKey = kvp.Key;
                var paragraphs = kvp.Value;
                
                var layoutParagraphs = new List<LayoutParagraph>();
                
                foreach (var paragraph in paragraphs)
                {
                    var layoutParagraph = new LayoutParagraph
                    {
                        ParagraphId = paragraph.ParagraphId,
                        Lines = paragraph.Lines,
                        BoundingBox = paragraph.BoundingBox,
                        // 添加雙通道特有的元數據
                        ProcessingNote = $"Processed by {paragraph.CreatedByChannel} channel" + 
                                       (paragraph.ChannelConfidence.HasValue ? $" (confidence: {paragraph.ChannelConfidence:F2})" : ""),
                        CreatedByChannel = paragraph.CreatedByChannel,
                        ChannelConfidence = paragraph.ChannelConfidence
                    };
                    
                    layoutParagraphs.Add(layoutParagraph);
                }
                
                layoutResult[columnKey] = layoutParagraphs;
                
                DualChannelLogger.Debug($"Converted column {columnKey}: {paragraphs.Count} paragraphs");
            }
            
            return layoutResult;
        }

        /// <summary>
        /// 輸出雙通道統計信息
        /// </summary>
        private void LogDualChannelStatistics()
        {
            if (_dualChannelController == null) return;
            
            try
            {
                var statistics = _dualChannelController.GetStatistics();
                var summary = statistics.FormatSummary();
                
                DualChannelLogger.Info("Dual-channel processing statistics:");
                DualChannelLogger.Info(summary);
                
                if (EnableDebugMode)
                {
                    DebugLog("📊 v4雙通道統計信息:");
                    DebugLog(summary);
                }
            }
            catch (Exception ex)
            {
                DualChannelLogger.Warn(ex, "Failed to retrieve dual-channel statistics");
            }
        }

        /// <summary>
        /// 重置雙通道統計信息
        /// </summary>
        public void ResetDualChannelStatistics()
        {
            _dualChannelController?.ResetStatistics();
            DualChannelLogger.Info("Dual-channel statistics reset");
        }

        /// <summary>
        /// 獲取雙通道統計信息
        /// </summary>
        public ProcessingStatistics GetDualChannelStatistics()
        {
            return _dualChannelController?.GetStatistics();
        }

        /// <summary>
        /// 更新主要分析入口以使用v4.0雙通道架構
        /// </summary>
        public LayoutAnalysisResult AnalyzeLayoutV4(OcrResult ocrResult)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                Logger.Info("🔍 開始執行v4.0高級版面分析 (雙通道架構)");
                DebugLog("=== MonLingo v4.0 版面分析開始 (雙通道架構) ===");
                
                if (ocrResult?.Lines == null || ocrResult.Lines.Length == 0)
                {
                    Logger.Warn("⚠️ OCR結果為空，返回空版面");
                    DebugLog("⚠️ 輸入OCR結果為空");
                    return new LayoutAnalysisResult
                    {
                        Success = false,
                        Layout = new Dictionary<string, List<LayoutParagraph>>(),
                        ProcessingTimeMs = 0
                    };
                }

                // 階段一：橫向行合併 (保持不變)
                Logger.Info("📝 階段一：開始橫向行合併");
                DebugLog($"📝 階段一：輸入 {ocrResult.Lines.Length} 行文字");
                var mergedLines = PerformHorizontalLineMerging(ocrResult.Lines);
                Logger.Info($"✅ 階段一完成：{ocrResult.Lines.Length} → {mergedLines.Count} 行");
                DebugLog($"✅ 階段一結果：合併了 {ocrResult.Lines.Length - mergedLines.Count} 個文字碎片");

                // 階段二：智能分欄 (保持不變)
                Logger.Info("📂 階段二：開始智能分欄");
                DebugLog($"📂 階段二：對 {mergedLines.Count} 行執行分欄檢測");
                var columns = PerformIntelligentColumnDetection(mergedLines);
                Logger.Info($"✅ 階段二完成：識別出 {columns.Count} 個欄位");
                DebugLog($"✅ 階段二結果：識別出 {columns.Count} 個欄位");

                // 階段三：段落分段 - 使用v4.0雙通道架構
                Logger.Info("📑 v4階段三：開始雙通道段落檢測");
                DebugLog($"📑 v4階段三：對 {columns.Count} 個欄位執行雙通道段落分割");
                var layoutResult = PerformParagraphSegmentationV4(columns);
                Logger.Info($"✅ v4階段三完成：生成 {layoutResult.Count} 個欄位的段落結構");
                DebugLog($"✅ v4階段三完成：生成 {layoutResult.Count} 個欄位的段落結構 (雙通道架構)");

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                // 生成調試信息
                var debugInfo = GenerateDebugInfo(layoutResult);
                
                Logger.Info($"🎉 v4.0版面分析完成，耗時 {processingTime:F2}ms");
                DebugLog($"🎉 v4.0版面分析完成，耗時 {processingTime:F2}ms");
                DebugLog("=== MonLingo v4.0 版面分析結束 ===");

                return new LayoutAnalysisResult
                {
                    Success = true,
                    Layout = layoutResult,
                    ProcessingTimeMs = processingTime,
                    DebugInfo = debugInfo,
                    Version = "4.0-DualChannel"
                };
            }
            catch (Exception ex)
            {
                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Logger.Error(ex, $"❌ v4.0版面分析失敗，耗時 {processingTime:F2}ms");
                DebugLog($"❌ v4.0版面分析錯誤: {ex.Message}");
                
                return new LayoutAnalysisResult
                {
                    Success = false,
                    Layout = new Dictionary<string, List<LayoutParagraph>>(),
                    ProcessingTimeMs = processingTime,
                    ErrorMessage = ex.Message,
                    Version = "4.0-DualChannel-Error"
                };
            }
        }
    }
}

namespace MonLingo.Core.Service
{
    /// <summary>
    /// LayoutParagraph 擴展以支持雙通道元數據
    /// </summary>
    public partial class LayoutParagraph
    {
        /// <summary>
        /// 處理通道的置信度
        /// </summary>
        public double? ChannelConfidence { get; set; }
        
        /// <summary>
        /// 處理註釋（包含使用的通道信息）
        /// </summary>
        public string ProcessingNote { get; set; }
        
        /// <summary>
        /// 創建此段落的通道類型
        /// </summary>
        public ChannelType? CreatedByChannel { get; set; }
    }

    /// <summary>
    /// LayoutAnalysisResult 擴展以支持版本標識
    /// </summary>
    public partial class LayoutAnalysisResult
    {
        /// <summary>
        /// 處理版本標識
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}