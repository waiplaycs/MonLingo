using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonLingo.Core.Model;
using NLog;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 簡化版版面分析服務 - AI驅動架構
    /// 僅包含階段一(橫向合併)和階段二(智能分欄)
    /// 段落合併交由AI翻譯層處理
    /// </summary>
    public partial class LayoutAnalysisService
    {
        /// <summary>
        /// 分析版面 - AI驅動版本
        /// 返回分欄結果,不進行段落合併
        /// </summary>
        /// <param name="ocrResult">OCR識別結果</param>
        /// <returns>版面分析結果(僅包含欄位信息)</returns>
        public LayoutAnalysisResultV2 AnalyzeLayoutV2(OcrResult ocrResult)
        {
            var startTime = DateTime.UtcNow;

            var msg1 = "🚀 開始版面分析 v2.0 (AI驅動架構)";
            Logger.Info(msg1);
            Console.WriteLine(msg1);
            
            if (ocrResult?.Lines == null || ocrResult.Lines.Length == 0)
            {
                var warnMsg = "⚠️ OCR結果為空，返回空版面";
                Logger.Warn(warnMsg);
                Console.WriteLine(warnMsg);
                return new LayoutAnalysisResultV2
                {
                    Success = false,
                    Columns = new List<Column>(),
                    ProcessingTimeMs = 0
                };
            }

            // 階段一：橫向行合併
            var msg2 = "📝 階段一：開始橫向行合併";
            Logger.Info(msg2);
            Console.WriteLine(msg2);
            var mergedLines = PerformHorizontalLineMerging(ocrResult.Lines);
            var msg3 = $"✅ 階段一完成：{ocrResult.Lines.Length} → {mergedLines.Count} 行（合併 {ocrResult.Lines.Length - mergedLines.Count} 個碎片）";
            Logger.Info(msg3);
            Console.WriteLine(msg3);

            // 階段二：智能分欄
            var msg4 = "📂 階段二：開始智能分欄";
            Logger.Info(msg4);
            Console.WriteLine(msg4);
            var columnLines = PerformIntelligentColumnDetection(mergedLines);
            var msg5 = $"✅ 階段二完成：識別出 {columnLines.Count} 個欄位";
            Logger.Info(msg5);
            Console.WriteLine(msg5);

            // 將List<List<LayoutLine>>轉換為List<Column>
            var columns = new List<Column>();
            for (int i = 0; i < columnLines.Count; i++)
            {
                var lines = columnLines[i];
                var column = new Column
                {
                    ColumnId = $"column_{i + 1}",
                    ColumnIndex = i,
                    Lines = lines,
                    BoundingBox = CalculateColumnBoundingBox(lines),
                    DebugColor = GetColumnColor(i)
                };
                columns.Add(column);

                Logger.Debug($"   欄位 {i + 1}: {lines.Count} 行");
            }

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Logger.Info($"🎯 版面分析v2.0完成，耗時 {processingTime:F1}ms");
            Logger.Info($"📊 結果: {columns.Count}個欄位, 共{mergedLines.Count}行文字");
            Logger.Info($"🔄 下一步: 交由AI翻譯層進行段落合併和翻譯");

            return new LayoutAnalysisResultV2
            {
                Success = true,
                Columns = columns,
                ProcessingTimeMs = processingTime,
                TotalLines = mergedLines.Count,
                OriginalLines = ocrResult.Lines.Length
            };
        }
    }

    /// <summary>
    /// 版面分析結果 v2.0 - AI驅動架構
    /// </summary>
    public class LayoutAnalysisResultV2
    {
        /// <summary>
        /// 分析是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 識別出的欄位列表
        /// </summary>
        public List<Column> Columns { get; set; } = new List<Column>();

        /// <summary>
        /// 處理耗時(毫秒)
        /// </summary>
        public double ProcessingTimeMs { get; set; }

        /// <summary>
        /// 合併後的總行數
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// 原始OCR行數
        /// </summary>
        public int OriginalLines { get; set; }
    }
}
