using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonLingo.Core.Service;
using NLog;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 高級版面分析服務
    /// 基於 PRD v5.0 規格實作三階段版面分析：
    /// 階段一：橫向行合併 (Horizontal Line Merging)
    /// 階段二：智能分欄 (Intelligent Column Detection) 
    /// 階段三：段落分段 (Paragraph Segmentation)
    /// </summary>
    public class LayoutAnalysisService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 欄位顏色配置（用於調試視覺化）
        /// </summary>
        private static readonly Color[] ColumnColors = new Color[]
        {
            Color.FromArgb(100, 255, 0, 0),    // 紅色 - Column 1
            Color.FromArgb(100, 0, 255, 0),    // 綠色 - Column 2  
            Color.FromArgb(100, 0, 0, 255),    // 藍色 - Column 3
            Color.FromArgb(100, 255, 165, 0),  // 橙色 - Column 4
            Color.FromArgb(100, 128, 0, 128),  // 紫色 - Column 5
            Color.FromArgb(100, 255, 192, 203), // 粉紅色 - Column 6
            Color.FromArgb(100, 0, 255, 255),  // 青色 - Column 7
            Color.FromArgb(100, 255, 255, 0),  // 黃色 - Column 8
        };

        /// <summary>
        /// 是否啟用調試模式
        /// </summary>
        public bool EnableDebugMode { get; set; } = false;

        /// <summary>
        /// 調試日誌輸出
        /// </summary>
        /// <param name="message">調試訊息</param>
        private void DebugLog(string message)
        {
            if (EnableDebugMode)
            {
                // 輸出到控制台和日誌
                Console.WriteLine($"[MonLingo v3版面分析] {message}");
                Logger.Debug($"[MonLingo v3版面分析] {message}");
                
                // 同時輸出到系統調試輸出
                System.Diagnostics.Debug.WriteLine($"[MonLingo v3版面分析] {message}");
            }
        }

        /// <summary>
        /// v3階段一調試：迭代式順序合併演算法
        /// </summary>
        /// <param name="boxA">文字框A</param>
        /// <param name="boxB">文字框B</param>
        /// <param name="stage">處理階段</param>
        /// <param name="result">處理結果</param>
        private void DebugStage1V3(Rectangle boxA, Rectangle boxB, string stage, string result)
        {
            if (EnableDebugMode)
            {
                DebugLog($"🔍 v3階段一-{stage}: 框A({boxA.X},{boxA.Y},{boxA.Width}×{boxA.Height}) + 框B({boxB.X},{boxB.Y},{boxB.Width}×{boxB.Height}) → {result}");
            }
        }

        /// <summary>
        /// v3階段一調試：詳細合併過程追蹤
        /// </summary>
        /// <param name="mainPtr">主指針位置</param>
        /// <param name="lookAheadPtr">前瞻指針位置</param>
        /// <param name="overlapRatio">垂直重疊率</param>
        /// <param name="horizontalGap">水平間距</param>
        /// <param name="avgHeight">平均高度</param>
        /// <param name="result">合併結果</param>
        private void DebugStage1MergeDetail(int mainPtr, int lookAheadPtr, double overlapRatio, int horizontalGap, double avgHeight, string result)
        {
            if (EnableDebugMode)
            {
                DebugLog($"   🧮 v3合併詳情: 主{mainPtr}+候選{lookAheadPtr} | 重疊率:{overlapRatio:F2} | 間距:{horizontalGap}px/{avgHeight:F1}px | 結果:{result}");
            }
        }

        /// <summary>
        /// v3階段二調試：單次遍歷有序聚類演算法
        /// </summary>
        /// <param name="lineIndex">行索引</param>
        /// <param name="line">文字行</param>
        /// <param name="columnIndex">分配的欄位索引</param>
        /// <param name="reason">分配原因</param>
        private void DebugStage2V3(int lineIndex, LayoutLine line, int columnIndex, string reason)
        {
            if (EnableDebugMode)
            {
                DebugLog($"📂 v3階段二-分欄: 行{lineIndex} 「{line.Text.Substring(0, Math.Min(20, line.Text.Length))}...」 → 欄位{columnIndex} ({reason})");
            }
        }

        /// <summary>
        /// v3階段二調試：歸屬判斷詳細過程
        /// </summary>
        /// <param name="lineIndex">行索引</param>
        /// <param name="columnIndex">目標欄位索引</param>
        /// <param name="minVerticalDist">最小垂直距離</param>
        /// <param name="verticalThreshold">垂直閾值</param>
        /// <param name="overlapRatio">水平重疊比例</param>
        /// <param name="overlapThreshold">重疊閾值</param>
        /// <param name="passed">是否通過檢查</param>
        private void DebugStage2OwnershipDetail(int lineIndex, int columnIndex, double minVerticalDist, double verticalThreshold, 
            double overlapRatio, double overlapThreshold, bool passed)
        {
            if (EnableDebugMode)
            {
                string status = passed ? "✅通過" : "❌失敗";
                DebugLog($"   🔍 v3歸屬檢查: 行{lineIndex} → 欄位{columnIndex} | 垂直:{minVerticalDist:F1}/{verticalThreshold:F1}px | 重疊:{overlapRatio:F2}/{overlapThreshold:F2} | {status}");
            }
        }

        /// <summary>
        /// v3階段二調試：活躍欄位修剪過程
        /// </summary>
        /// <param name="currentLineIndex">當前行索引</param>
        /// <param name="columnIndex">被修剪的欄位索引</param>
        /// <param name="columnSize">欄位大小</param>
        /// <param name="verticalDistance">垂直距離</param>
        /// <param name="threshold">修剪閾值</param>
        private void DebugStage2Pruning(int currentLineIndex, int columnIndex, int columnSize, double verticalDistance, double threshold)
        {
            if (EnableDebugMode)
            {
                DebugLog($"   ✂️ v3欄位修剪: 行{currentLineIndex}觸發 | 欄位{columnIndex}({columnSize}行) | 距離:{verticalDistance:F1}px > 閾值:{threshold:F1}px");
            }
        }

        /// <summary>
        /// v3階段三調試：混合模式段落檢測
        /// </summary>
        /// <param name="columnKey">欄位鍵值</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="line">文字行</param>
        /// <param name="paragraphIndex">段落索引</param>
        /// <param name="action">執行動作</param>
        private void DebugStage3V3(string columnKey, int lineIndex, LayoutLine line, int paragraphIndex, string action)
        {
            if (EnableDebugMode)
            {
                DebugLog($"📑 v3階段三-段落: {columnKey} 行{lineIndex} 「{line.Text.Substring(0, Math.Min(15, line.Text.Length))}...」 → 段落{paragraphIndex} ({action})");
            }
        }

        /// <summary>
        /// v3階段三調試：內容類型預檢查結果
        /// </summary>
        /// <param name="columnKey">欄位鍵值</param>
        /// <param name="contentType">內容類型</param>
        /// <param name="lineCount">行數</param>
        /// <param name="listIndicatorCount">列表指示符數量</param>
        private void DebugStage3ContentType(string columnKey, ContentType contentType, int lineCount, int? listIndicatorCount = null)
        {
            if (EnableDebugMode)
            {
                string details = contentType == ContentType.ListItems 
                    ? $" (列表符號:{listIndicatorCount}/{lineCount})" 
                    : "";
                DebugLog($"   🎯 v3內容分析: {columnKey} | 類型:{contentType} | {lineCount}行{details}");
            }
        }

        /// <summary>
        /// v3階段三調試：標準行距計算結果
        /// </summary>
        /// <param name="columnKey">欄位鍵值</param>
        /// <param name="spacingCount">間距數量</param>
        /// <param name="medianSpacing">中位數間距</param>
        /// <param name="minSpacing">最小間距</param>
        /// <param name="maxSpacing">最大間距</param>
        private void DebugStage3StandardSpacing(string columnKey, int spacingCount, double medianSpacing, double minSpacing, double maxSpacing)
        {
            if (EnableDebugMode)
            {
                DebugLog($"   📏 v3標準行距: {columnKey} | {spacingCount}個間距值 | 中位數:{medianSpacing:F1}px | 範圍:[{minSpacing:F1}, {maxSpacing:F1}]px");
            }
        }

        /// <summary>
        /// v3階段三調試：多指標加權決策系統詳細分析
        /// </summary>
        /// <param name="lineIndex">行索引</param>
        /// <param name="relativeDistanceScore">相對距離得分</param>
        /// <param name="fontHeightPenalty">字體高度懲罰</param>
        /// <param name="alignmentPenalty">對齊風格懲罰</param>
        /// <param name="overlapBonus">重疊獎勵得分</param>
        /// <param name="totalScore">總分</param>
        /// <param name="threshold">合併閾值</param>
        /// <param name="willMerge">是否合併</param>
        private void DebugStage3WeightedScore(int lineIndex, double relativeDistanceScore, double fontHeightPenalty, 
            double alignmentPenalty, double overlapBonus, double totalScore, double threshold, bool willMerge)
        {
            if (EnableDebugMode)
            {
                string decision = willMerge ? "🔗合併" : "✂️分割";
                DebugLog($"   🧮 v3加權分析: 行{lineIndex} | 距離:{relativeDistanceScore:F2} | 字體懲罰:-{fontHeightPenalty:F2} | 對齊懲罰:-{alignmentPenalty:F2} | 重疊:+{overlapBonus:F1} | 總分:{totalScore:F2}/{threshold} | {decision}");
            }
        }

        /// <summary>
        /// v3性能統計調試
        /// </summary>
        /// <param name="stage">階段名稱</param>
        /// <param name="inputCount">輸入數量</param>
        /// <param name="outputCount">輸出數量</param>
        /// <param name="processingTimeMs">處理時間(毫秒)</param>
        /// <param name="complexity">算法複雜度</param>
        private void DebugStagePerformance(string stage, int inputCount, int outputCount, double processingTimeMs, string complexity)
        {
            if (EnableDebugMode)
            {
                DebugLog($"⚡ v3性能統計: {stage} | {inputCount}→{outputCount} | {processingTimeMs:F2}ms | 複雜度:{complexity}");
            }
        }

        /// <summary>
        /// 向後兼容的舊版調試方法 (已標記為過時)
        /// </summary>
        [Obsolete("請使用 DebugStage1V3 替代此方法")]
        private void DebugStage1(Rectangle boxA, Rectangle boxB, string stage, string result)
        {
            DebugStage1V3(boxA, boxB, stage, result);
        }

        /// <summary>
        /// 向後兼容的舊版調試方法 (已標記為過時)
        /// </summary>
        [Obsolete("請使用 DebugStage2V3 替代此方法")]
        private void DebugStage2(int lineIndex, LayoutLine line, int columnIndex, string reason)
        {
            DebugStage2V3(lineIndex, line, columnIndex, reason);
        }

        /// <summary>
        /// 向後兼容的舊版調試方法 (已標記為過時)
        /// </summary>
        [Obsolete("請使用 DebugStage3V3 替代此方法")]
        private void DebugStage3(string columnKey, int lineIndex, LayoutLine line, int paragraphIndex, string action)
        {
            DebugStage3V3(columnKey, lineIndex, line, paragraphIndex, action);
        }

        /// <summary>
        /// 獲取欄位顏色
        /// </summary>
        /// <param name="columnIndex">欄位索引</param>
        /// <returns>對應的顏色</returns>
        private Color GetColumnColor(int columnIndex)
        {
            return ColumnColors[columnIndex % ColumnColors.Length];
        }

        /// <summary>
        /// 生成調試信息
        /// </summary>
        /// <param name="layoutResult">版面分析結果</param>
        /// <returns>調試信息</returns>
        private LayoutDebugInfo GenerateDebugInfo(Dictionary<string, List<LayoutParagraph>> layoutResult)
        {
            var debugInfo = new LayoutDebugInfo();
            
            int columnIndex = 0;
            foreach (var column in layoutResult)
            {
                var columnColor = GetColumnColor(columnIndex);
                debugInfo.ColumnColors[column.Key] = columnColor;
                
                foreach (var paragraph in column.Value)
                {
                    var paragraphDebugInfo = new ParagraphDebugInfo
                    {
                        ColumnKey = column.Key,
                        ParagraphId = paragraph.ParagraphId,
                        BoundingBox = paragraph.BoundingBox,
                        Color = columnColor
                    };
                    
                    // 收集原始行邊界框和合併資訊
                    foreach (var line in paragraph.Lines)
                    {
                        paragraphDebugInfo.OriginalLineBounds.Add(line.BoundingBox);
                        
                        // 如果這行是合併而來的，記錄所有合併來源的索引
                        if (line.IsMerged)
                        {
                            foreach (var index in line.MergedFromIndices)
                            {
                                debugInfo.MergedOriginalIndices.Add(index);
                            }
                        }
                    }
                    
                    debugInfo.ParagraphBounds.Add(paragraphDebugInfo);
                }
                
                columnIndex++;
            }
            
            return debugInfo;
        }

        /// <summary>
        /// 完整版面分析入口
        /// 執行三階段處理流程，返回結構化版面數據
        /// </summary>
        /// <param name="ocrResult">原始OCR識別結果</param>
        /// <returns>結構化版面分析結果</returns>
        public LayoutAnalysisResult AnalyzeLayout(OcrResult ocrResult)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                Logger.Info("🔍 開始執行高級版面分析");
                DebugLog("=== MonLingo 版面分析開始 ===");
                
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

                // 階段一：橫向行合併
                Logger.Info("📝 階段一：開始橫向行合併");
                DebugLog($"📝 階段一：輸入 {ocrResult.Lines.Length} 行文字");
                var mergedLines = PerformHorizontalLineMerging(ocrResult.Lines);
                Logger.Info($"✅ 階段一完成：{ocrResult.Lines.Length} → {mergedLines.Count} 行（合併 {ocrResult.Lines.Length - mergedLines.Count} 個碎片）");
                DebugLog($"✅ 階段一結果：合併了 {ocrResult.Lines.Length - mergedLines.Count} 個文字碎片");

                // 階段二：智能分欄
                Logger.Info("📂 階段二：開始智能分欄");
                DebugLog($"📂 階段二：對 {mergedLines.Count} 行執行分欄檢測");
                var columns = PerformIntelligentColumnDetection(mergedLines);
                Logger.Info($"✅ 階段二完成：識別出 {columns.Count} 個欄位");
                DebugLog($"✅ 階段二結果：識別出 {columns.Count} 個欄位");
                
                // 顯示欄位詳細信息
                for (int i = 0; i < columns.Count; i++)
                {
                    DebugLog($"   欄位 {i + 1}: {columns[i].Count} 行，顏色 #{GetColumnColor(i).Name}");
                }

                // 階段三：段落分段
                Logger.Info("📑 v3階段三：開始混合模式段落檢測");
                Console.WriteLine("📑 v3階段三：開始混合模式段落檢測");
                DebugLog($"📑 v3階段三：對 {columns.Count} 個欄位執行段落分割");
                var layoutResult = PerformParagraphSegmentation(columns);
                Logger.Info($"✅ v3階段三完成：生成 {layoutResult.Count} 個欄位的段落結構");
                Console.WriteLine($"✅ v3階段三完成：生成 {layoutResult.Count} 個欄位的段落結構");
                
                // 顯示段落詳細信息
                foreach (var column in layoutResult)
                {
                    DebugLog($"   {column.Key}: {column.Value.Count} 個段落");
                    for (int p = 0; p < column.Value.Count; p++)
                    {
                        var paragraph = column.Value[p];
                        DebugLog($"     段落 {p + 1}: {paragraph.Lines.Count} 行，範圍 ({paragraph.BoundingBox.X},{paragraph.BoundingBox.Y},{paragraph.BoundingBox.Width},{paragraph.BoundingBox.Height})");
                    }
                }
                
                // 返回最終的結構化版面數據
                var tempLayout = layoutResult;

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Logger.Info($"🎯 版面分析完成，耗時 {processingTime:F1}ms");
                DebugLog($"🎯 版面分析完成，總耗時 {processingTime:F1}ms");

                // 生成調試信息
                var debugInfo = GenerateDebugInfo(layoutResult);

                return new LayoutAnalysisResult
                {
                    Success = true,
                    Layout = tempLayout,
                    ProcessingTimeMs = processingTime,
                    DebugInfo = EnableDebugMode ? debugInfo : null
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ 版面分析失敗");
                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                return new LayoutAnalysisResult
                {
                    Success = false,
                    Layout = new Dictionary<string, List<LayoutParagraph>>(),
                    ProcessingTimeMs = processingTime,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 階段一：橫向行合併 (v3版本)
        /// 將因OCR辨識而產生的、在同一水平線上的文字碎片，拼接成語義上完整的單行文字
        /// 採用迭代式順序合併演算法 (O(n)優化版本)
        /// </summary>
        /// <param name="ocrLines">原始OCR行結果</param>
        /// <returns>合併後的完整文字行列表</returns>
        private List<LayoutLine> PerformHorizontalLineMerging(OcrLine[] ocrLines)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Logger.Debug($"🔄 開始橫向行合併 (v3算法)，輸入 {ocrLines.Length} 個OCR行");
            Logger.Debug($"📊 v3預排序：按Y座標升序(從上到下)，Y相同時按X座標升序(從左到右)");

            // v3規格：全局預排序 - 先按Y座標升序，Y座標相同時再按X座標升序
            // 確保處理順序符合「從上到下、從左到右」的閱讀直覺
            var layoutLines = ocrLines.Select((line, index) => new LayoutLine
            {
                Text = line.Text,
                Confidence = line.Confidence,
                BoundingBox = line.BoundingBox,
                LineHeight = line.BoundingBox.Height,
                OriginalIndex = index,
                MergedFromIndices = new List<int> { index } // 初始狀態，每行對應自己的索引
            }).OrderBy(line => line.BoundingBox.Top)      // 主要按Y座標排序（從上到下）
              .ThenBy(line => line.BoundingBox.Left)      // 次要按X座標排序（從左到右）
              .ToList();

            var result = new List<LayoutLine>();
            var processed = new bool[layoutLines.Count]; // 已處理標記

            // v3迭代式順序合併：主指針遍歷，前瞻指針尋找候選
            for (int mainPtr = 0; mainPtr < layoutLines.Count; mainPtr++)
            {
                if (processed[mainPtr]) continue; // 已處理則跳過

                var currentLine = layoutLines[mainPtr];
                var mergedLine = new LayoutLine
                {
                    Text = currentLine.Text,
                    Confidence = currentLine.Confidence,
                    BoundingBox = currentLine.BoundingBox,
                    LineHeight = currentLine.LineHeight,
                    OriginalIndex = currentLine.OriginalIndex,
                    MergedFromIndices = new List<int>(currentLine.MergedFromIndices)
                };

                Logger.Debug($"📍 主指針 {mainPtr}: 開始處理「{currentLine.Text}」");
                
                // 🔥 修復：v3迭代式順序合併 - 只檢查緊鄰的下一個候選行
                // 一旦合併失敗就停止，移動到下一個主指針
                bool hasMerged = false;
                
                for (int lookAheadPtr = mainPtr + 1; lookAheadPtr < layoutLines.Count; lookAheadPtr++)
                {
                    if (processed[lookAheadPtr]) continue; // 已處理則跳過

                    var candidateLine = layoutLines[lookAheadPtr];

                    DebugStage1V3(mergedLine.BoundingBox, candidateLine.BoundingBox, "迭代檢測", 
                        $"主{mainPtr}與候選{lookAheadPtr}");

                    // v3標準：檢查垂直重疊率（60%固定閾值）
                    if (!CheckVerticalOverlapV3(mergedLine.BoundingBox, candidateLine.BoundingBox))
                    {
                        // 計算調試需要的詳細數據
                        int overlapTop = Math.Max(mergedLine.BoundingBox.Top, candidateLine.BoundingBox.Top);
                        int overlapBottom = Math.Min(mergedLine.BoundingBox.Bottom, candidateLine.BoundingBox.Bottom);
                        int overlapHeight = Math.Max(0, overlapBottom - overlapTop);
                        int smallerHeight = Math.Min(mergedLine.BoundingBox.Height, candidateLine.BoundingBox.Height);
                        double overlapRatio = smallerHeight > 0 ? (double)overlapHeight / smallerHeight : 0;
                        int horizontalGap = CalculateHorizontalDistance(mergedLine.BoundingBox, candidateLine.BoundingBox);
                        double avgHeight = (mergedLine.BoundingBox.Height + candidateLine.BoundingBox.Height) / 2.0;
                        
                        DebugStage1MergeDetail(mainPtr, lookAheadPtr, overlapRatio, horizontalGap, avgHeight, "垂直重疊率<60%");
                        DebugStage1V3(mergedLine.BoundingBox, candidateLine.BoundingBox, "垂直檢查", "未通過-垂直重疊率<60%");
                        
                        // 🔥 關鍵修復：一旦垂直重疊失敗，停止當前主指針的探索
                        break;
                    }

                    // v3標準：檢查水平間距（1.5x平均高度動態閾值）
                    if (!CheckHorizontalSpacingV3(mergedLine.BoundingBox, candidateLine.BoundingBox))
                    {
                        int horizontalGap = CalculateHorizontalDistance(mergedLine.BoundingBox, candidateLine.BoundingBox);
                        double avgHeight = (mergedLine.BoundingBox.Height + candidateLine.BoundingBox.Height) / 2.0;
                        double threshold = avgHeight * 1.5;
                        
                        DebugStage1MergeDetail(mainPtr, lookAheadPtr, 0, horizontalGap, avgHeight, $"水平間距過大:{horizontalGap}px>{threshold:F1}px");
                        DebugStage1V3(mergedLine.BoundingBox, candidateLine.BoundingBox, "水平檢查", "未通過-水平間距過大");
                        
                        // 🔥 關鍵修復：一旦水平間距失敗，停止當前主指針的探索
                        break;
                    }

                    // 通過所有檢查，執行合併
                    var newMergedLine = MergeTwoLinesV3(mergedLine, candidateLine);
                    int oldLength = mergedLine.Text.Length;
                    mergedLine = newMergedLine;
                    processed[lookAheadPtr] = true; // 標記候選行已處理
                    hasMerged = true;

                    DebugStage1V3(mergedLine.BoundingBox, candidateLine.BoundingBox, "合併成功", 
                        $"長度{oldLength}→{mergedLine.Text.Length}字元");

                    Logger.Debug($"✅ v3合併：主行「{currentLine.Text}」+ 候選「{candidateLine.Text}」");
                    
                    // 🔥 關鍵修復：合併成功後，用新的mergedLine繼續探索下一個候選
                    // 但不break，而是繼續迭代，實現真正的"迭代式順序合併"
                }

                // 將最終合併結果加入結果列表
                result.Add(mergedLine);
                processed[mainPtr] = true; // 標記主行已處理

                if (hasMerged)
                {
                    Logger.Debug($"🎯 主{mainPtr}完成：合併鏈結束，最終文字「{mergedLine.Text.Substring(0, Math.Min(50, mergedLine.Text.Length))}...」");
                }
                else
                {
                    Logger.Debug($"🎯 主{mainPtr}完成：無合併，保持原文字「{mergedLine.Text.Substring(0, Math.Min(50, mergedLine.Text.Length))}...」");
                }
            }

            Logger.Info($"🎯 v3橫向行合併完成：{ocrLines.Length} → {result.Count} 行 (算法複雜度: O(n))");
            
            stopwatch.Stop();
            DebugStagePerformance("v3階段一-迭代式順序合併", ocrLines.Length, result.Count, stopwatch.Elapsed.TotalMilliseconds, "O(n)");
            
            return result;
        }

        /// <summary>
        /// 關卡一：檢查垂直重疊率
        /// 只有當垂直重疊率超過50%時才可能合併
        /// </summary>
        /// <param name="boxA">文字框A</param>
        /// <param name="boxB">文字框B</param>
        /// <returns>是否通過垂直重疊率檢查</returns>
        private bool CheckVerticalOverlap(Rectangle boxA, Rectangle boxB)
        {
            // 計算垂直重疊區域
            int overlapTop = Math.Max(boxA.Top, boxB.Top);
            int overlapBottom = Math.Min(boxA.Bottom, boxB.Bottom);
            int overlapHeight = Math.Max(0, overlapBottom - overlapTop);

            // 計算較小框的高度
            int smallerHeight = Math.Min(boxA.Height, boxB.Height);
            if (smallerHeight <= 0) return false;

            // 計算重疊率
            double overlapRatio = (double)overlapHeight / smallerHeight;

            Logger.Debug($"📏 垂直重疊率檢查：{overlapRatio:F2} (閾值=0.5)");
            return overlapRatio > 0.5;
        }

        /// <summary>
        /// v3版本：檢查垂直重疊率
        /// v3標準：使用60%閾值（垂直重疊率必須嚴格大於60%）
        /// </summary>
        /// <param name="boxA">文字框A</param>
        /// <param name="boxB">文字框B</param>
        /// <returns>是否通過v3垂直重疊率檢查</returns>
        private bool CheckVerticalOverlapV3(Rectangle boxA, Rectangle boxB)
        {
            // 計算垂直重疊區域
            int overlapTop = Math.Max(boxA.Top, boxB.Top);
            int overlapBottom = Math.Min(boxA.Bottom, boxB.Bottom);
            int overlapHeight = Math.Max(0, overlapBottom - overlapTop);

            // 計算較小框的高度
            int smallerHeight = Math.Min(boxA.Height, boxB.Height);
            if (smallerHeight <= 0) return false;

            // v3標準：計算重疊率（60%閾值）
            double overlapRatio = (double)overlapHeight / smallerHeight;
            const double V3_VERTICAL_THRESHOLD = 0.6; // v3固定閾值

            Logger.Debug($"📏 v3垂直重疊率檢查：{overlapRatio:F2} (v3閾值>{V3_VERTICAL_THRESHOLD})");
            return overlapRatio > V3_VERTICAL_THRESHOLD; // v3規格：嚴格大於60%
        }

        /// <summary>
        /// 關卡二：檢查相對水平間距
        /// 只有當水平間距小於平均字高的1.5倍時才可能合併
        /// </summary>
        /// <param name="boxA">文字框A</param>
        /// <param name="boxB">文字框B</param>
        /// <returns>是否通過水平間距檢查</returns>
        private bool CheckHorizontalSpacing(Rectangle boxA, Rectangle boxB)
        {
            // 計算水平間距
            int horizontalGap = CalculateHorizontalDistance(boxA, boxB);

            // 計算平均字元高度（使用兩框的平均高度作為參考）
            double avgCharHeight = (boxA.Height + boxB.Height) / 2.0;

            // 檢查間距是否在合理範圍內（1.5倍字高）
            double threshold = avgCharHeight * 1.5;

            Logger.Debug($"📏 水平間距檢查：{horizontalGap}px vs 閾值{threshold:F1}px（平均字高={avgCharHeight:F1}px）");
            return horizontalGap < threshold;
        }

        /// <summary>
        /// v3版本：檢查相對水平間距
        /// v3標準：使用1.5x平均高度動態閾值（與原版相同但更明確的算法標註）
        /// </summary>
        /// <param name="boxA">文字框A</param>
        /// <param name="boxB">文字框B</param>
        /// <returns>是否通過v3水平間距檢查</returns>
        private bool CheckHorizontalSpacingV3(Rectangle boxA, Rectangle boxB)
        {
            // v3算法：計算水平間距
            int horizontalGap = CalculateHorizontalDistance(boxA, boxB);

            // v3算法：計算平均字元高度（動態閾值基準）
            double avgCharHeight = (boxA.Height + boxB.Height) / 2.0;

            // v3標準：1.5x平均高度動態閾值
            const double V3_HORIZONTAL_MULTIPLIER = 1.5;
            double v3Threshold = avgCharHeight * V3_HORIZONTAL_MULTIPLIER;

            Logger.Debug($"📏 v3水平間距檢查：{horizontalGap}px vs v3閾值{v3Threshold:F1}px（{V3_HORIZONTAL_MULTIPLIER}x平均字高={avgCharHeight:F1}px）");
            return horizontalGap < v3Threshold;
        }

        /// <summary>
        /// 計算兩個矩形之間的水平距離
        /// </summary>
        /// <param name="boxA">文字框A</param>
        /// <param name="boxB">文字框B</param>
        /// <returns>水平間距（像素）</returns>
        private int CalculateHorizontalDistance(Rectangle boxA, Rectangle boxB)
        {
            // 確保boxA在boxB左側（如果不是則交換）
            if (boxA.Right > boxB.Left)
            {
                var temp = boxA;
                boxA = boxB;
                boxB = temp;
            }

            // 計算水平間距（右邊框到左邊框的距離）
            return Math.Max(0, boxB.Left - boxA.Right);
        }

        /// <summary>
        /// 合併兩個文字行
        /// </summary>
        /// <param name="lineA">文字行A</param>
        /// <param name="lineB">文字行B</param>
        /// <returns>合併後的文字行</returns>
        private LayoutLine MergeTwoLines(LayoutLine lineA, LayoutLine lineB)
        {
            // 確保從左到右的順序
            var leftLine = lineA.BoundingBox.Left <= lineB.BoundingBox.Left ? lineA : lineB;
            var rightLine = lineA.BoundingBox.Left <= lineB.BoundingBox.Left ? lineB : lineA;

            // 合併文字內容
            string mergedText = $"{leftLine.Text} {rightLine.Text}".Trim();

            // 計算合併的邊界框
            var mergedBBox = Rectangle.Union(lineA.BoundingBox, lineB.BoundingBox);

            // 計算平均置信度（按字符數加權）
            double weightA = leftLine.Text.Length;
            double weightB = rightLine.Text.Length;
            double totalWeight = weightA + weightB;
            double mergedConfidence = totalWeight > 0 
                ? (leftLine.Confidence * weightA + rightLine.Confidence * weightB) / totalWeight 
                : (leftLine.Confidence + rightLine.Confidence) / 2.0;

            // 創建合併後的行，記錄合併來源
            var mergedLine = new LayoutLine
            {
                Text = mergedText,
                Confidence = mergedConfidence,
                BoundingBox = mergedBBox,
                LineHeight = mergedBBox.Height,
                OriginalIndex = Math.Min(leftLine.OriginalIndex, rightLine.OriginalIndex) // 保留較小的索引
            };

            // 記錄合併來源
            mergedLine.MergedFromIndices.AddRange(leftLine.MergedFromIndices.Count > 0 ? leftLine.MergedFromIndices : new[] { leftLine.OriginalIndex });
            mergedLine.MergedFromIndices.AddRange(rightLine.MergedFromIndices.Count > 0 ? rightLine.MergedFromIndices : new[] { rightLine.OriginalIndex });

            return mergedLine;
        }

        /// <summary>
        /// v3版本：合併兩個文字行
        /// v3算法：支持迭代式合併，保持更新後的邊界框和索引追蹤
        /// </summary>
        /// <param name="lineA">文字行A（可能是已合併的行）</param>
        /// <param name="lineB">文字行B（候選合併行）</param>
        /// <returns>v3合併後的文字行</returns>
        private LayoutLine MergeTwoLinesV3(LayoutLine lineA, LayoutLine lineB)
        {
            // v3算法：確保從左到右的順序（支持動態邊界框）
            var leftLine = lineA.BoundingBox.Left <= lineB.BoundingBox.Left ? lineA : lineB;
            var rightLine = lineA.BoundingBox.Left <= lineB.BoundingBox.Left ? lineB : lineA;

            // v3優化：智能空格處理（避免重複空格）
            string mergedText = $"{leftLine.Text.TrimEnd()} {rightLine.Text.TrimStart()}".Trim();

            // v3算法：動態邊界框合併（支持不規則形狀）
            var mergedBBox = Rectangle.Union(lineA.BoundingBox, lineB.BoundingBox);

            // v3算法：增強置信度計算（考慮文字品質和長度）
            double weightA = Math.Max(1, leftLine.Text.Length) * leftLine.Confidence;
            double weightB = Math.Max(1, rightLine.Text.Length) * rightLine.Confidence;
            double totalWeight = Math.Max(1, leftLine.Text.Length) + Math.Max(1, rightLine.Text.Length);
            double mergedConfidence = (weightA + weightB) / totalWeight;

            // v3創建：增強合併後的行物件
            var mergedLine = new LayoutLine
            {
                Text = mergedText,
                Confidence = Math.Min(1.0, mergedConfidence), // v3限制：確保置信度不超過1.0
                BoundingBox = mergedBBox,
                LineHeight = mergedBBox.Height,
                OriginalIndex = Math.Min(leftLine.OriginalIndex, rightLine.OriginalIndex), // v3保持：最小索引優先
                MergedFromIndices = new List<int>() // v3初始化：準備合併索引記錄
            };

            // v3索引追蹤：完整的合併來源記錄
            mergedLine.MergedFromIndices.AddRange(leftLine.MergedFromIndices ?? new List<int> { leftLine.OriginalIndex });
            mergedLine.MergedFromIndices.AddRange(rightLine.MergedFromIndices ?? new List<int> { rightLine.OriginalIndex });

            Logger.Debug($"📝 v3合併詳情：「{leftLine.Text}」+「{rightLine.Text}」→「{mergedText}」(置信度:{mergedConfidence:F3})");

            return mergedLine;
        }

        /// <summary>
        /// 階段二：智能分欄 (v3版本)
        /// 使用單次遍歷有序聚類演算法識別畫面中的獨立文字區塊或欄位
        /// v3特點：消除重複掃描，每個文字行只被有效訪問一次
        /// </summary>
        /// <param name="mergedLines">階段一輸出的完整文字行列表</param>
        /// <returns>按欄位分組的文字行列表</returns>
        private List<List<LayoutLine>> PerformIntelligentColumnDetection(List<LayoutLine> mergedLines)
        {
            Logger.Debug($"🔄 開始智能分欄 (v3算法)，輸入 {mergedLines.Count} 個完整文字行");

            if (mergedLines.Count == 0)
            {
                Logger.Warn("⚠️ 輸入文字行為空，返回空欄位列表");
                return new List<List<LayoutLine>>();
            }

            // 步驟1：全局預排序（從上到下、從左到右）
            Logger.Debug("📋 v3步驟1：全局預排序");
            var sortedLines = GlobalPreSort(mergedLines);

            // 步驟2：單次遍歷有序聚類 (v3核心算法)
            Logger.Debug("🌪️ v3步驟2：開始單次遍歷有序聚類");
            var columns = SinglePassOrderedClusteringV3(sortedLines);

            Logger.Info($"🎯 v3智能分欄完成：{mergedLines.Count} 行 → {columns.Count} 個欄位");
            
            // 記錄每個欄位的詳細信息
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var bbox = CalculateColumnBoundingBox(column);
                Logger.Debug($"📂 v3欄位{i + 1}：{column.Count} 行，範圍({bbox.X},{bbox.Y},{bbox.Width},{bbox.Height})");
            }

            return columns;
        }

        /// <summary>
        /// 全局預排序：從上到下、從左到右
        /// </summary>
        /// <param name="lines">待排序的文字行列表</param>
        /// <returns>排序後的文字行列表</returns>
        private List<LayoutLine> GlobalPreSort(List<LayoutLine> lines)
        {
            // 按Y座標（上到下）為主要排序，X座標（左到右）為次要排序
            var sorted = lines.OrderBy(line => line.BoundingBox.Top)
                             .ThenBy(line => line.BoundingBox.Left)
                             .ToList();

            Logger.Debug($"📋 全局排序完成：{lines.Count} 行已按從上到下、從左到右排序");
            return sorted;
        }

        /// <summary>
        /// v3版本：單次遍歷有序聚類 (Single-Pass Ordered Clustering)
        /// 核心特點：消除重複掃描，每個文字行只被訪問一次
        /// </summary>
        /// <param name="sortedLines">已排序的文字行列表</param>
        /// <returns>按欄位分組的文字行列表</returns>
        private List<List<LayoutLine>> SinglePassOrderedClusteringV3(List<LayoutLine> sortedLines)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Logger.Debug($"🚀 v3單次遍歷開始，處理 {sortedLines.Count} 行");

            // v3核心數據結構
            var activeColumns = new List<List<LayoutLine>>(); // 活躍欄位列表
            var completedColumns = new List<List<LayoutLine>>(); // 已完成欄位列表
            
            // 計算全局平均行間距（用於欄位修剪）
            double globalAvgLineSpacing = CalculateGlobalAverageLineSpacing(sortedLines);
            Logger.Debug($"📏 v3全局平均行間距：{globalAvgLineSpacing:F1}px");

            // v3主循環：只使用一個for循環遍歷所有行
            for (int i = 0; i < sortedLines.Count; i++)
            {
                var currentLine = sortedLines[i];
                bool assigned = false;

                Logger.Debug($"🔍 v3處理行{i}: 「{currentLine.Text.Substring(0, Math.Min(30, currentLine.Text.Length))}...」");

                // v3步驟3：歸屬判斷 - 嘗試將當前行歸屬到活躍欄位
                assigned = ProceedToOwnershipCheckV3(currentLine, activeColumns, i);

                // 如果無法歸入任何活躍欄位，創建新欄位
                if (!assigned)
                {
                    var newColumn = new List<LayoutLine> { currentLine };
                    activeColumns.Add(newColumn);
                    
                    DebugStage2V3(i, currentLine, activeColumns.Count - 1, "新欄位種子");
                    Logger.Debug($"🌱 v3新種子：行{i}創建欄位{activeColumns.Count}");
                }

                // v3步驟4：混合模式欄位修剪
                PerformColumnPruningV3(currentLine, activeColumns, completedColumns, globalAvgLineSpacing, i);
            }

            // 將所有剩餘的活躍欄位移入已完成列表
            completedColumns.AddRange(activeColumns);

            Logger.Debug($"✅ v3聚類完成：{completedColumns.Count} 個欄位，活躍欄位最大數={activeColumns.Count}");
            
            stopwatch.Stop();
            int totalLines = completedColumns.Sum(col => col.Count);
            DebugStagePerformance("v3階段二-單次遍歷有序聚類", sortedLines.Count, completedColumns.Count, stopwatch.Elapsed.TotalMilliseconds, "O(n×m)");
            
            return completedColumns;
        }

        /// <summary>
        /// v3算法：計算全局平均行間距
        /// 文檔要求：使用中位數排除極端大間距（如段落間距）的干擾
        /// </summary>
        /// <param name="sortedLines">已排序的文字行列表</param>
        /// <returns>全局平均行間距（像素）</returns>
        private double CalculateGlobalAverageLineSpacing(List<LayoutLine> sortedLines)
        {
            if (sortedLines.Count < 2) return 50.0; // 默認值

            var spacings = new List<double>();
            
            // 計算所有相鄰行的垂直間距（相鄰行間距統計）
            for (int i = 1; i < sortedLines.Count; i++)
            {
                var prevLine = sortedLines[i - 1];
                var currentLine = sortedLines[i];
                
                // v3標準：計算真實的垂直間距
                double spacing = Math.Max(0, currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom);
                spacings.Add(spacing);
            }

            // v3核心：使用中位數作為代表值，排除異常大間距的干擾
            if (spacings.Count == 0) return 50.0;
            
            spacings.Sort();
            double median = spacings.Count % 2 == 0 
                ? (spacings[spacings.Count / 2 - 1] + spacings[spacings.Count / 2]) / 2.0
                : spacings[spacings.Count / 2];

            Logger.Debug($"📊 v3行間距統計：{spacings.Count}個相鄰間距，中位數={median:F1}px（排除異常值）");
            Logger.Debug($"📊 間距範圍：最小={spacings[0]:F1}px，最大={spacings[spacings.Count-1]:F1}px");
            
            return Math.Max(10.0, median); // 確保最小值為10px
        }

        /// <summary>
        /// v3算法：歸屬判斷 - 嘗試將當前行歸屬到現有的活躍欄位
        /// </summary>
        /// <param name="currentLine">當前處理的文字行</param>
        /// <param name="activeColumns">活躍欄位列表</param>
        /// <param name="lineIndex">行索引</param>
        /// <returns>是否成功歸屬</returns>
        private bool ProceedToOwnershipCheckV3(LayoutLine currentLine, List<List<LayoutLine>> activeColumns, int lineIndex)
        {
            // 遍歷所有活躍欄位，尋找可歸屬的欄位
            for (int columnIndex = 0; columnIndex < activeColumns.Count; columnIndex++)
            {
                var column = activeColumns[columnIndex];
                
                // v3標準：檢查歸屬條件
                if (CanAssignToColumnV3(currentLine, column, columnIndex, lineIndex))
                {
                    // 成功歸屬：將行添加到欄位並更新邊界
                    column.Add(currentLine);
                    
                    DebugStage2V3(lineIndex, currentLine, columnIndex, "歸屬成功");
                    Logger.Debug($"✅ v3歸屬：行{lineIndex}加入欄位{columnIndex}（欄位大小：{column.Count}）");
                    
                    return true; // 找到歸屬後立即返回
                }
            }
            
            return false; // 無法歸入任何活躍欄位
        }

        /// <summary>
        /// v3算法：混合模式欄位修剪
        /// 及時識別並移除不可能再增長的欄位
        /// </summary>
        /// <param name="currentLine">當前處理的文字行</param>
        /// <param name="activeColumns">活躍欄位列表</param>
        /// <param name="completedColumns">已完成欄位列表</param>
        /// <param name="globalAvgLineSpacing">全局平均行間距</param>
        /// <param name="currentLineIndex">當前行索引（用於調試）</param>
        private void PerformColumnPruningV3(LayoutLine currentLine, List<List<LayoutLine>> activeColumns, 
            List<List<LayoutLine>> completedColumns, double globalAvgLineSpacing, int currentLineIndex = -1)
        {
            // v3混合修剪條件：基於全局平均行間距的動態閾值
            double pruningThreshold = globalAvgLineSpacing * 3.0; // v3標準：3倍行間距
            
            var columnsToRemove = new List<int>();
            
            // 檢查每個活躍欄位是否應該被修剪
            for (int i = 0; i < activeColumns.Count; i++)
            {
                var column = activeColumns[i];
                if (column.Count == 0) continue;
                
                // 計算當前行與欄位底部的垂直距離
                var columnBottom = column.Max(line => line.BoundingBox.Bottom);
                double verticalDistance = Math.Max(0, currentLine.BoundingBox.Top - columnBottom);
                
                // 如果距離超過修剪閾值，標記為完成
                if (verticalDistance > pruningThreshold)
                {
                    columnsToRemove.Add(i);
                    DebugStage2Pruning(currentLineIndex, i, column.Count, verticalDistance, pruningThreshold);
                    Logger.Debug($"✂️ v3修剪：欄位{i}距離{verticalDistance:F1}px > 閾值{pruningThreshold:F1}px，標記完成");
                }
            }
            
            // 從後往前移除（避免索引變化問題）
            for (int i = columnsToRemove.Count - 1; i >= 0; i--)
            {
                int columnIndex = columnsToRemove[i];
                var completedColumn = activeColumns[columnIndex];
                
                completedColumns.Add(completedColumn);
                activeColumns.RemoveAt(columnIndex);
                
                Logger.Debug($"📋 v3完成：欄位{columnIndex}移入已完成列表，包含{completedColumn.Count}行");
            }
        }

        /// <summary>
        /// v3算法：檢查當前行是否可以歸屬到指定欄位
        /// 採用v3標準的歸屬判斷條件
        /// </summary>
        /// <param name="currentLine">當前文字行</param>
        /// <param name="column">目標欄位</param>
        /// <param name="columnIndex">欄位索引（用於調試）</param>
        /// <param name="lineIndex">行索引（用於調試）</param>
        /// <returns>是否可以歸屬</returns>
        private bool CanAssignToColumnV3(LayoutLine currentLine, List<LayoutLine> column, int columnIndex = -1, int lineIndex = -1)
        {
            if (column.Count == 0) return false;
            
            var currentBox = currentLine.BoundingBox;
            
            // v3標準1：垂直鄰近度檢查
            // 計算當前行與欄位中所有行的最小垂直距離
            double minVerticalDistance = double.MaxValue;
            double avgLineHeightInColumn = column.Average(line => line.BoundingBox.Height);
            
            foreach (var existingLine in column)
            {
                double verticalDist = CalculateVerticalDistance(currentBox, existingLine.BoundingBox);
                minVerticalDistance = Math.Min(minVerticalDistance, verticalDist);
            }
            
            double verticalThreshold = avgLineHeightInColumn * 1.2; // v3標準：1.2倍欄位內平均行高
            bool verticalPassed = minVerticalDistance <= verticalThreshold;
            if (!verticalPassed)
            {
                Logger.Debug($"📏 v3垂直檢查失敗：最小距離{minVerticalDistance:F1}px > 閾值{verticalThreshold:F1}px");
                return false;
            }
            
            // v3標準2：水平重疊度檢查
            // 文檔要求：計算當前行與目標欄位邊界框之間的水平重疊範圍
            var columnLeft = column.Min(line => line.BoundingBox.Left);
            var columnRight = column.Max(line => line.BoundingBox.Right);
            
            double overlapLeft = Math.Max(currentBox.Left, columnLeft);
            double overlapRight = Math.Min(currentBox.Right, columnRight);
            double overlapWidth = Math.Max(0, overlapRight - overlapLeft);
            
            // v3標準：重疊寬度除以兩者寬度的較小值
            double currentWidth = currentBox.Width;
            double columnWidth = columnRight - columnLeft;
            double minWidth = Math.Min(currentWidth, columnWidth);
            double overlapRatio = minWidth > 0 ? overlapWidth / minWidth : 0;
            
            const double V3_OVERLAP_THRESHOLD = 0.5; // v3標準：50%重疊
            bool overlapPassed = overlapRatio >= V3_OVERLAP_THRESHOLD;
            
            // 添加詳細的歸屬檢查調試信息
            DebugStage2OwnershipDetail(lineIndex, columnIndex, minVerticalDistance, verticalThreshold, overlapRatio, V3_OVERLAP_THRESHOLD, verticalPassed && overlapPassed);
            
            if (!overlapPassed)
            {
                Logger.Debug($"📏 v3重疊檢查失敗：重疊率{overlapRatio:F2} < 閾值{V3_OVERLAP_THRESHOLD}");
                Logger.Debug($"📏 詳細：當前行寬{currentWidth}px，欄位寬{columnWidth}px，重疊寬{overlapWidth}px");
                return false;
            }
            
            Logger.Debug($"✅ v3歸屬檢查通過：垂直距離{minVerticalDistance:F1}px，重疊率{overlapRatio:F2}");
            return true;
        }

        /// <summary>
        /// 有序觸發「滾雪球」聚類 (舊版本 - 已被v3取代)
        /// 注意：此方法已被 SinglePassOrderedClusteringV3 取代，保留僅供參考
        /// </summary>
        /// <param name="sortedLines">已排序的文字行列表</param>
        /// <returns>按欄位分組的文字行列表</returns>
        [Obsolete("此方法已被v3版本取代，請使用 SinglePassOrderedClusteringV3")]
        private List<List<LayoutLine>> OrderedSnowballClustering(List<LayoutLine> sortedLines)
        {
            var columns = new List<List<LayoutLine>>();
            var isProcessed = new bool[sortedLines.Count]; // 標記每行是否已被處理

            // 計算平均行高，用於鄰近度判斷
            double avgLineHeight = sortedLines.Average(line => line.LineHeight);
            Logger.Debug($"📏 平均行高：{avgLineHeight:F1}px");

            // 主循環：遍歷所有行，尋找未處理的行作為種子
            for (int i = 0; i < sortedLines.Count; i++)
            {
                if (isProcessed[i]) continue; // 跳過已處理的行

                // 創建新欄位，以當前行作為種子
                var currentColumn = new List<LayoutLine> { sortedLines[i] };
                isProcessed[i] = true;
                
                DebugStage2(i, sortedLines[i], columns.Count, "新欄位種子");
                Logger.Debug($"🌱 新種子：行{i + 1}「{sortedLines[i].Text}」");

                // 滾雪球子循環：不斷擴展當前欄位
                bool foundNewMember;
                do
                {
                    foundNewMember = false;
                    
                    // 掃描所有未處理的行，尋找可以加入當前欄位的行
                    for (int j = 0; j < sortedLines.Count; j++)
                    {
                        if (isProcessed[j]) continue; // 跳過已處理的行

                        var candidateLine = sortedLines[j];
                        
                        // 檢查候選行是否與當前欄位中的任何行「足夠近」
                        if (IsCloseToColumn(candidateLine, currentColumn, avgLineHeight))
                        {
                            // 吸納新成員
                            currentColumn.Add(candidateLine);
                            isProcessed[j] = true;
                            foundNewMember = true;
                            
                            DebugStage2(j, candidateLine, columns.Count, "滾雪球吸納");
                            Logger.Debug($"🔗 吸納新成員：行{j + 1}「{candidateLine.Text}」加入欄位{columns.Count + 1}");
                        }
                        else
                        {
                            DebugStage2(j, candidateLine, columns.Count, "距離過遠-跳過");
                        }
                    }
                } 
                while (foundNewMember); // 持續滾雪球直到無法再找到新成員

                // 完成當前欄位，添加到結果列表
                columns.Add(currentColumn);
                DebugLog($"✅ 欄位{columns.Count}完成：包含{currentColumn.Count}行文字");
                Logger.Debug($"✅ 欄位{columns.Count}完成：{currentColumn.Count} 行");
            }

            return columns;
        }

        /// <summary>
        /// 檢查候選行是否與欄位中的任何行「足夠近」
        /// 需要同時滿足垂直鄰近度和水平鄰近度條件
        /// </summary>
        /// <param name="candidateLine">候選文字行</param>
        /// <param name="column">目標欄位</param>
        /// <param name="avgLineHeight">平均行高</param>
        /// <returns>是否足夠近</returns>
        private bool IsCloseToColumn(LayoutLine candidateLine, List<LayoutLine> column, double avgLineHeight)
        {
            // 檢查候選行是否與欄位中的任何一行都「足夠近」
            foreach (var existingLine in column)
            {
                if (IsCloseToLine(candidateLine, existingLine, avgLineHeight))
                {
                    return true; // 只要與欄位中任何一行接近就可以加入
                }
            }
            
            return false; // 與欄位中所有行都不夠近
        }

        /// <summary>
        /// 檢查兩行是否「足夠近」
        /// 需要同時滿足垂直鄰近度和水平鄰近度條件
        /// </summary>
        /// <param name="lineA">文字行A</param>
        /// <param name="lineB">文字行B</param>
        /// <param name="avgLineHeight">平均行高</param>
        /// <returns>是否足夠近</returns>
        private bool IsCloseToLine(LayoutLine lineA, LayoutLine lineB, double avgLineHeight)
        {
            var boxA = lineA.BoundingBox;
            var boxB = lineB.BoundingBox;

            // 條件a：垂直鄰近度檢查
            // 兩個文字框的垂直間距小於平均行高的1.2倍
            double verticalDistance = CalculateVerticalDistance(boxA, boxB);
            double verticalThreshold = avgLineHeight * 1.2;
            
            if (verticalDistance > verticalThreshold)
            {
                Logger.Debug($"📏 垂直距離過大：{verticalDistance:F1}px > {verticalThreshold:F1}px");
                return false;
            }

            // 條件b：水平鄰近度檢查
            // 兩個文字框的水平範圍有顯著重疊（重疊寬度 > 50%）
            double horizontalOverlap = CalculateHorizontalOverlap(boxA, boxB);
            double minWidth = Math.Min(boxA.Width, boxB.Width);
            double overlapRatio = minWidth > 0 ? horizontalOverlap / minWidth : 0;
            
            if (overlapRatio < 0.5) // 重疊寬度需要 > 50%
            {
                Logger.Debug($"📏 水平重疊不足：{overlapRatio:F2} < 0.5");
                return false;
            }

            Logger.Debug($"✅ 足夠近：垂直距離={verticalDistance:F1}px，水平重疊率={overlapRatio:F2}");
            return true;
        }

        /// <summary>
        /// 計算兩個矩形之間的垂直距離
        /// </summary>
        /// <param name="boxA">矩形A</param>
        /// <param name="boxB">矩形B</param>
        /// <returns>垂直距離（像素）</returns>
        private double CalculateVerticalDistance(Rectangle boxA, Rectangle boxB)
        {
            // 如果有垂直重疊，距離為0
            if (boxA.Bottom >= boxB.Top && boxA.Top <= boxB.Bottom)
            {
                return 0;
            }

            // 否則計算最短垂直距離
            if (boxA.Bottom < boxB.Top)
            {
                return boxB.Top - boxA.Bottom; // A在B上方
            }
            else
            {
                return boxA.Top - boxB.Bottom; // A在B下方
            }
        }

        /// <summary>
        /// 計算兩個矩形的水平重疊寬度
        /// </summary>
        /// <param name="boxA">矩形A</param>
        /// <param name="boxB">矩形B</param>
        /// <returns>重疊寬度（像素）</returns>
        private double CalculateHorizontalOverlap(Rectangle boxA, Rectangle boxB)
        {
            int overlapLeft = Math.Max(boxA.Left, boxB.Left);
            int overlapRight = Math.Min(boxA.Right, boxB.Right);
            
            return Math.Max(0, overlapRight - overlapLeft);
        }

        /// <summary>
        /// 計算欄位的整體邊界框
        /// </summary>
        /// <param name="column">欄位中的文字行列表</param>
        /// <returns>整體邊界框</returns>
        private Rectangle CalculateColumnBoundingBox(List<LayoutLine> column)
        {
            if (column.Count == 0) return Rectangle.Empty;

            int minX = column.Min(line => line.BoundingBox.Left);
            int minY = column.Min(line => line.BoundingBox.Top);
            int maxX = column.Max(line => line.BoundingBox.Right);
            int maxY = column.Max(line => line.BoundingBox.Bottom);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 合併候選項
        /// </summary>
        private class MergeCandidate
        {
            public int IndexA { get; set; }
            public int IndexB { get; set; }
            public int HorizontalDistance { get; set; }
        }

        /// <summary>
        /// 階段三：混合模式段落分割 (v3版本)
        /// 採用混合模式段落檢測策略，為不同類型的內容提供最優化的處理路徑
        /// </summary>
        /// <param name="columns">已分欄的文字行</param>
        /// <returns>分割後的段落字典</returns>
        private Dictionary<string, List<LayoutParagraph>> PerformParagraphSegmentation(List<List<LayoutLine>> columns)
        {
            Logger.Debug($"🔄 v3階段三：開始混合模式段落分割，輸入 {columns.Count} 個欄位");
            Console.WriteLine($"🔄 v3階段三：開始混合模式段落分割，輸入 {columns.Count} 個欄位");
            
            var result = new Dictionary<string, List<LayoutParagraph>>();
            
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var column = columns[columnIndex];
                if (column.Count == 0) continue;

                string columnKey = $"column_{columnIndex + 1}";
                var columnColor = GetColumnColor(columnIndex);
                
                Logger.Debug($"📑 v3處理 {columnKey}：{column.Count} 行");
                Console.WriteLine($"📑 v3處理 {columnKey}：{column.Count} 行");
                
                // v3核心：混合模式段落檢測
                var paragraphs = HybridParagraphDetectionV3(column, columnColor, columnKey);
                result[columnKey] = paragraphs;
                
                Logger.Debug($"✅ v3完成 {columnKey}：生成 {paragraphs.Count} 個段落");
                Console.WriteLine($"✅ v3完成 {columnKey}：生成 {paragraphs.Count} 個段落");
                
                Logger.Debug($"✅ {columnKey} v3分割完成：{paragraphs.Count} 個段落");
            }

            Logger.Info($"🎯 v3混合模式段落分割完成：處理 {columns.Count} 個欄位");
            return result;
        }

        /// <summary>
        /// v3算法：混合模式段落檢測 (Hybrid Paragraph Detection)
        /// 步驟一：內容類型預檢查
        /// 步驟二：計算標準行距
        /// 步驟三：多指標加權決策系統
        /// </summary>
        /// <param name="column">欄位中的文字行</param>
        /// <param name="columnColor">欄位顏色</param>
        /// <param name="columnKey">欄位鍵值</param>
        /// <returns>分割後的段落列表</returns>
        private List<LayoutParagraph> HybridParagraphDetectionV3(List<LayoutLine> column, Color columnColor, string columnKey)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            if (column.Count == 0) return new List<LayoutParagraph>();

            Logger.Debug($"🚀 v3混合檢測開始：{columnKey} ({column.Count}行)");

            // v3步驟一：內容類型預檢查 (Content Type Pre-analysis)
            var contentType = AnalyzeContentTypeV3(column);
            Logger.Debug($"📊 v3內容類型：{contentType}");

            switch (contentType)
            {
                case ContentType.SingleLine:
                    var singleResult = HandleSingleLineShortcutV3(column, columnColor, columnKey);
                    stopwatch.Stop();
                    DebugStagePerformance("v3階段三-單行捷徑", column.Count, singleResult.Count, stopwatch.Elapsed.TotalMilliseconds, "O(1)");
                    return singleResult;
                
                case ContentType.ListItems:
                    var listResult = HandleListItemDetectionV3(column, columnColor, columnKey);
                    stopwatch.Stop();
                    DebugStagePerformance("v3階段三-列表項目檢測", column.Count, listResult.Count, stopwatch.Elapsed.TotalMilliseconds, "O(n)");
                    return listResult;
                
                case ContentType.ContinuousText:
                default:
                    var textResult = HandleContinuousTextV3(column, columnColor, columnKey);
                    stopwatch.Stop();
                    DebugStagePerformance("v3階段三-混合模式段落檢測", column.Count, textResult.Count, stopwatch.Elapsed.TotalMilliseconds, "O(n)");
                    return textResult;
            }
        }

        /// <summary>
        /// v3內容類型枚舉
        /// </summary>
        private enum ContentType
        {
            SingleLine,      // 單行欄位
            ListItems,       // 列表項目
            ContinuousText   // 連續文本
        }

        /// <summary>
        /// v3步驟一：內容類型預檢查 (Content Type Pre-analysis)
        /// </summary>
        private ContentType AnalyzeContentTypeV3(List<LayoutLine> column)
        {
            // 1. 單行欄位捷徑 (Single-Line Shortcut)
            if (column.Count == 1)
            {
                Logger.Debug("🎯 v3預檢查：單行欄位捷徑");
                return ContentType.SingleLine;
            }

            // 2. 列表項目識別 (List Item Detection)
            int listIndicatorCount = 0;
            foreach (var line in column)
            {
                var trimmedText = line.Text.Trim();
                if (IsListIndicator(trimmedText))
                {
                    listIndicatorCount++;
                }
            }

            // 如果超過50%的行具有列表特徵，認為是列表
            double listRatio = (double)listIndicatorCount / column.Count;
            if (listRatio >= 0.5)
            {
                Logger.Debug($"🎯 v3預檢查：列表項目識別 (列表比例:{listRatio:F2})");
                return ContentType.ListItems;
            }

            // 3. 連續文本處理 (Continuous Text Handling)
            Logger.Debug("🎯 v3預檢查：連續文本處理");
            return ContentType.ContinuousText;
        }

        /// <summary>
        /// 檢查文本是否為列表指示符
        /// </summary>
        private bool IsListIndicator(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            // 檢查常見列表符號
            var listPatterns = new[] { "*", "-", "•", "○", "●", "►", "▶" };
            if (listPatterns.Any(pattern => text.StartsWith(pattern)))
                return true;

            // 檢查數字列表 (1. 2. 3. 等)
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+[\.\)]\s"))
                return true;

            // 檢查字母列表 (a) b) c) 等)
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^[a-zA-Z][\.\)]\s"))
                return true;

            return false;
        }

        /// <summary>
        /// v3處理：單行欄位捷徑 (Single-Line Shortcut)
        /// </summary>
        private List<LayoutParagraph> HandleSingleLineShortcutV3(List<LayoutLine> column, Color columnColor, string columnKey)
        {
            Logger.Debug("⚡ v3單行捷徑：跳過所有分割邏輯");
            
            var paragraph = CreateParagraphV3(column, 0, columnColor);
            DebugStage3V3(columnKey, 0, column[0], 0, "單行捷徑");
            
            return new List<LayoutParagraph> { paragraph };
        }

        /// <summary>
        /// v3處理：列表項目識別 (List Item Detection)
        /// </summary>
        private List<LayoutParagraph> HandleListItemDetectionV3(List<LayoutLine> column, Color columnColor, string columnKey)
        {
            Logger.Debug("📋 v3列表檢測：基於縮排和列表符號的快速分割");
            
            // 統計列表指示符數量用於調試
            int listIndicatorCount = column.Count(line => IsListIndicator(line.Text));
            DebugStage3ContentType(columnKey, ContentType.ListItems, column.Count, listIndicatorCount);
            
            var paragraphs = new List<LayoutParagraph>();
            
            // 將每個列表項視為一個獨立段落
            for (int i = 0; i < column.Count; i++)
            {
                var singleLineList = new List<LayoutLine> { column[i] };
                var paragraph = CreateParagraphV3(singleLineList, i, columnColor);
                paragraphs.Add(paragraph);
                
                DebugStage3V3(columnKey, i, column[i], i, "列表項目分割");
            }
            
            return paragraphs;
        }

        /// <summary>
        /// v3處理：連續文本處理 (Continuous Text Handling)
        /// 使用多指標加權決策系統進行精細分割
        /// </summary>
        private List<LayoutParagraph> HandleContinuousTextV3(List<LayoutLine> column, Color columnColor, string columnKey)
        {
            Logger.Debug("📖 v3連續文本：啟用多指標加權決策系統");
            
            // 添加內容類型調試信息
            DebugStage3ContentType(columnKey, ContentType.ContinuousText, column.Count);

            // v3步驟二：計算標準行距 (Calculate Standard Line Spacing)
            double standardLineSpacing = CalculateStandardLineSpacingV3(column, columnKey);
            Logger.Debug($"📏 v3標準行距：{standardLineSpacing:F1}px (中位數)");

            // v3步驟三：多指標加權決策系統 (Multi-Indicator Weighted System)
            return ApplyWeightedDecisionSystemV3(column, standardLineSpacing, columnColor, columnKey);
        }        /// <summary>
        /// v3步驟二：計算標準行距 (Calculate Standard Line Spacing)
        /// 使用中位數計算標準行內間距，排除極端大間距的干擾
        /// </summary>
        private double CalculateStandardLineSpacingV3(List<LayoutLine> column, string columnKey = "未知欄位")
        {
            if (column.Count < 2) return 20.0; // 默認值

            var spacings = new List<double>();
            
            // 計算所有相鄰行的垂直間距
            for (int i = 1; i < column.Count; i++)
            {
                var prevLine = column[i - 1];
                var currentLine = column[i];
                
                // v3標準：計算真實的垂直間距
                double spacing = Math.Max(0, currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom);
                spacings.Add(spacing);
            }

            // v3核心：使用中位數作為代表值，排除異常大間距的干擾
            spacings.Sort();
            double median = spacings.Count % 2 == 0
                ? (spacings[spacings.Count / 2 - 1] + spacings[spacings.Count / 2]) / 2.0
                : spacings[spacings.Count / 2];
                
            // 添加標準行距計算的調試信息
            double minSpacing = spacings.Count > 0 ? spacings[0] : 0;
            double maxSpacing = spacings.Count > 0 ? spacings[spacings.Count - 1] : 0;
            DebugStage3StandardSpacing("未知欄位", spacings.Count, median, minSpacing, maxSpacing);

            Logger.Debug($"📊 v3間距分析：共{spacings.Count}個間距值，中位數={median:F1}px");
            return Math.Max(1.0, median); // 確保不為0
        }

        /// <summary>
        /// v3步驟三：多指標加權決策系統 (Multi-Indicator Weighted System)
        /// 合併分數 = 相對距離得分 + 字體高度得分 + 對齊風格得分 + 重疊獎勵得分
        /// </summary>
        private List<LayoutParagraph> ApplyWeightedDecisionSystemV3(List<LayoutLine> column, double standardLineSpacing, Color columnColor, string columnKey)
        {
            // v3新特性：內容特徵自適應閾值系統
            double adaptiveThreshold = CalculateAdaptiveThresholdV3(column, standardLineSpacing, columnKey);
            
            var paragraphs = new List<LayoutParagraph>();
            var currentParagraph = new List<LayoutLine> { column[0] };
            
            Logger.Debug($"🧮 v3加權系統：自適應閾值={adaptiveThreshold:F2}");
            Console.WriteLine($"🧮 v3自適應閾值系統：動態閾值={adaptiveThreshold:F2} (基礎2.0+調整)");
            DebugStage3V3(columnKey, 0, column[0], 0, "段落起始");

            for (int i = 1; i < column.Count; i++)
            {
                var currentLine = column[i];
                var previousLine = column[i - 1];
                
                // 計算合併分數
                double mergeScore = CalculateMergeScoreV3(previousLine, currentLine, standardLineSpacing);
                
                Logger.Debug($"📊 v3分數：行{i} 「{currentLine.Text.Substring(0, Math.Min(20, currentLine.Text.Length))}...」 → {mergeScore:F2}");
                Console.WriteLine($"📊 v3合併分數：行{i} → {mergeScore:F2} (閾值:{adaptiveThreshold:F2})");
                
                // 獲取各組件分數用於詳細調試
                double relativeDistanceScore = CalculateRelativeDistanceScoreV3(previousLine, currentLine, standardLineSpacing);
                double fontHeightPenalty = CalculateFontHeightPenaltyV3(previousLine, currentLine);
                double alignmentPenalty = CalculateAlignmentStylePenaltyV3(previousLine, currentLine);
                double overlapBonus = CalculateOverlapBonusV3(previousLine, currentLine);
                
                DebugStage3WeightedScore(i, relativeDistanceScore, fontHeightPenalty, alignmentPenalty, overlapBonus, mergeScore, adaptiveThreshold, mergeScore > adaptiveThreshold);
                
                if (mergeScore > adaptiveThreshold)
                {
                    // 合併到當前段落
                    currentParagraph.Add(currentLine);
                    DebugStage3V3(columnKey, i, currentLine, paragraphs.Count, $"合併-分數{mergeScore:F2}");
                    Console.WriteLine($"🔗 v3決策：行{i} 合併 (分數{mergeScore:F2} > {adaptiveThreshold:F2})");
                }
                else
                {
                    // 創建新段落
                    if (currentParagraph.Count > 0)
                    {
                        paragraphs.Add(CreateParagraphV3(currentParagraph, paragraphs.Count, columnColor));
                        Logger.Debug($"✂️ v3分割：分數{mergeScore:F2} ≤ {adaptiveThreshold:F2}，創建段落{paragraphs.Count}");
                        Console.WriteLine($"✂️ v3決策：行{i} 分割 (分數{mergeScore:F2} ≤ {adaptiveThreshold:F2})，創建段落{paragraphs.Count}");
                        DebugStage3V3(columnKey, i-1, previousLine, paragraphs.Count-1, $"段落結束-{currentParagraph.Count}行");
                    }
                    currentParagraph = new List<LayoutLine> { currentLine };
                    DebugStage3V3(columnKey, i, currentLine, paragraphs.Count, $"新段落-合併分數{mergeScore:F2}");
                }
            }
            
            // 處理最後一個段落
            if (currentParagraph.Count > 0)
            {
                paragraphs.Add(CreateParagraphV3(currentParagraph, paragraphs.Count, columnColor));
                DebugStage3V3(columnKey, column.Count-1, column[column.Count-1], paragraphs.Count-1, $"最終段落-{currentParagraph.Count}行");
            }

            Logger.Debug($"🎯 v3決策完成：{paragraphs.Count} 個段落");
            Console.WriteLine($"🎯 v3段落分析完成：{paragraphs.Count} 個段落");
            return paragraphs;
        }

        /// <summary>
        /// v3新特性：內容特徵自適應閾值計算系統
        /// 根據欄位的內容特徵動態調整合併閾值
        /// </summary>
        private double CalculateAdaptiveThresholdV3(List<LayoutLine> column, double standardLineSpacing, string columnKey)
        {
            const double BASE_THRESHOLD = 2.0; // 基礎閾值
            
            // 1. 行數密度調整
            double avgLineHeight = column.Average(line => line.LineHeight);
            double lineDensityFactor = (column.Count / avgLineHeight) * 0.1;
            lineDensityFactor = Math.Min(lineDensityFactor, 0.5); // 限制最大調整量
            
            // 2. 字體一致性加成
            var lineHeights = column.Select(line => (double)line.LineHeight).ToArray();
            double heightStdDev = CalculateStandardDeviation(lineHeights);
            double fontConsistencyBonus = heightStdDev < 0.1 * avgLineHeight ? 0.3 : 0.0;
            
            // 3. 標準行距調整
            double spacingFactor = standardLineSpacing > avgLineHeight ? 0.2 : -0.1; // 行距大時更保守合併
            
            // 計算最終閾值
            double adaptiveThreshold = BASE_THRESHOLD + lineDensityFactor + fontConsistencyBonus + spacingFactor;
            adaptiveThreshold = Math.Max(1.5, Math.Min(adaptiveThreshold, 4.0)); // 限制在合理範圍內
            
            Logger.Debug($"📊 v3自適應閾值計算：基礎{BASE_THRESHOLD} + 密度{lineDensityFactor:F2} + 一致性{fontConsistencyBonus:F1} + 行距{spacingFactor:F1} = {adaptiveThreshold:F2}");
            Console.WriteLine($"📊 v3自適應閾值詳細：基礎{BASE_THRESHOLD} + 密度{lineDensityFactor:F2} + 一致性{fontConsistencyBonus:F1} + 行距{spacingFactor:F1} = {adaptiveThreshold:F2}");
            
            return adaptiveThreshold;
        }

        /// <summary>
        /// 計算標準差的輔助方法
        /// </summary>
        private double CalculateStandardDeviation(double[] values)
        {
            if (values.Length == 0) return 0.0;
            
            double mean = values.Average();
            double sumOfSquaredDifferences = values.Select(val => (val - mean) * (val - mean)).Sum();
            return Math.Sqrt(sumOfSquaredDifferences / values.Length);
        }

        /// <summary>
        /// v3合併分數計算模型 (加減分混合公式)
        /// 合併分數 = 相對距離得分 - 字體高度懲罰 - 對齊風格懲罰 + 重疊獎勵得分
        /// </summary>
        private double CalculateMergeScoreV3(LayoutLine prevLine, LayoutLine currentLine, double standardLineSpacing)
        {
            // 從基礎分開始計算
            double mergeScore = 0.0;
            
            // 1. 相對距離得分 (Relative Distance Score) - 主要得分項
            double relativeDistanceScore = CalculateRelativeDistanceScoreV3(prevLine, currentLine, standardLineSpacing);
            mergeScore += relativeDistanceScore;
            
            // 2. 字體高度懲罰 (Font Height Penalty)
            double fontHeightPenalty = CalculateFontHeightPenaltyV3(prevLine, currentLine);
            mergeScore -= fontHeightPenalty;
            
            // 3. 對齊風格懲罰 (Alignment Style Penalty)
            double alignmentStylePenalty = CalculateAlignmentStylePenaltyV3(prevLine, currentLine);
            mergeScore -= alignmentStylePenalty;
            
            // 4. 重疊獎勵得分 (Overlap Bonus Score)
            double overlapBonus = CalculateOverlapBonusV3(prevLine, currentLine);
            mergeScore += overlapBonus;
            
            Logger.Debug($"   🧮 v3分數明細：相對距離{relativeDistanceScore:F2} - 字體懲罰{fontHeightPenalty:F2} - 對齊懲罰{alignmentStylePenalty:F2} + 重疊獎勵{overlapBonus:F2} = {mergeScore:F2}");
            
            return mergeScore; // 允許負分存在
        }

        /// <summary>
        /// 1. 相對距離得分 (Relative Distance Score)
        /// 計算：max(0, 3.0 - (line_i.Y - line_{i-1}.Bottom) / 標準行距)
        /// </summary>
        private double CalculateRelativeDistanceScoreV3(LayoutLine prevLine, LayoutLine currentLine, double standardLineSpacing)
        {
            double verticalDistance = Math.Max(0, currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom);
            double relativeDistance = verticalDistance / standardLineSpacing;
            
            // v3公式：距離越近，得分越高
            double score = Math.Max(0, 3.0 - relativeDistance);
            
            Logger.Debug($"      📏 相對距離：{verticalDistance}px ÷ {standardLineSpacing:F1}px = {relativeDistance:F2} → 得分{score:F2}");
            return score;
        }

        /// <summary>
        /// 2. 字體高度懲罰 (Font Height Penalty)
        /// ≤5%差異: 0懲罰, ≤15%差異: 0.3懲罰, >15%差異: 0.5懲罰
        /// </summary>
        private double CalculateFontHeightPenaltyV3(LayoutLine prevLine, LayoutLine currentLine)
        {
            double heightDiff = Math.Abs(prevLine.LineHeight - currentLine.LineHeight);
            double avgHeight = (prevLine.LineHeight + currentLine.LineHeight) / 2.0;
            
            if (avgHeight == 0) return 0.0;
            
            double heightDiffRatio = heightDiff / avgHeight;
            
            if (heightDiffRatio <= 0.05) // ≤5%
            {
                Logger.Debug($"      📏 字體高度懲罰：差異{heightDiffRatio:P1} ≤ 5% → 0懲罰");
                return 0.0;
            }
            else if (heightDiffRatio <= 0.15) // ≤15%
            {
                Logger.Debug($"      📏 字體高度懲罰：差異{heightDiffRatio:P1} ≤ 15% → 0.3懲罰");
                return 0.3;
            }
            else // >15%
            {
                Logger.Debug($"      📏 字體高度懲罰：差異{heightDiffRatio:P1} > 15% → 0.5懲罰");
                return 0.5;
            }
        }

        /// <summary>
        /// 3. 對齊風格懲罰 (Alignment Style Penalty)
        /// 相同對齊: 0懲罰, 不同對齊: 0.2懲罰
        /// </summary>
        private double CalculateAlignmentStylePenaltyV3(LayoutLine prevLine, LayoutLine currentLine)
        {
            const int ALIGNMENT_TOLERANCE = 10; // 對齊容差（像素）
            
            // 簡化的對齊檢測：比較左邊界
            int leftDiff = Math.Abs(prevLine.BoundingBox.Left - currentLine.BoundingBox.Left);
            
            if (leftDiff <= ALIGNMENT_TOLERANCE)
            {
                Logger.Debug($"      📏 對齊風格懲罰：左邊界差異{leftDiff}px ≤ {ALIGNMENT_TOLERANCE}px → 0懲罰");
                return 0.0;
            }
            else
            {
                Logger.Debug($"      📏 對齊風格懲罰：左邊界差異{leftDiff}px > {ALIGNMENT_TOLERANCE}px → 0.2懲罰");
                return 0.2;
            }
        }

        /// <summary>
        /// 4. 重疊獎勵得分 (Overlap Bonus Score)
        /// 垂直重疊: 2.0分, 無重疊: 0分
        /// </summary>
        private double CalculateOverlapBonusV3(LayoutLine prevLine, LayoutLine currentLine)
        {
            double verticalDistance = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
            
            if (verticalDistance < 0) // 發生重疊
            {
                Logger.Debug($"      📏 重疊獎勵：垂直距離{verticalDistance}px < 0 → 2.0分");
                return 2.0;
            }
            else
            {
                Logger.Debug($"      📏 重疊獎勵：垂直距離{verticalDistance}px ≥ 0 → 0分");
                return 0.0;
            }
        }

        /// <summary>
        /// v3版本：創建段落對象
        /// </summary>
        private LayoutParagraph CreateParagraphV3(List<LayoutLine> lines, int paragraphIndex, Color columnColor)
        {
            var boundingBox = CalculateParagraphBoundingBox(lines);
            
            return new LayoutParagraph
            {
                ParagraphId = $"paragraph_{paragraphIndex + 1}",
                Lines = new List<LayoutLine>(lines),
                BoundingBox = boundingBox,
                ColumnColor = columnColor
            };
        }

        /// <summary>
        /// 計算段落的邊界框
        /// </summary>
        private Rectangle CalculateParagraphBoundingBox(List<LayoutLine> lines)
        {
            if (lines.Count == 0) return Rectangle.Empty;

            int minX = lines.Min(line => line.BoundingBox.Left);
            int minY = lines.Min(line => line.BoundingBox.Top);
            int maxX = lines.Max(line => line.BoundingBox.Right);
            int maxY = lines.Max(line => line.BoundingBox.Bottom);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }

    /// <summary>
    /// 版面分析結果
    /// </summary>
    public class LayoutAnalysisResult
    {
        public bool Success { get; set; }
        public Dictionary<string, List<LayoutParagraph>> Layout { get; set; }
        public double ProcessingTimeMs { get; set; }
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 調試視覺化信息（僅在調試模式下填充）
        /// </summary>
        public LayoutDebugInfo DebugInfo { get; set; }
    }

    /// <summary>
    /// 版面分析調試信息
    /// </summary>
    public class LayoutDebugInfo
    {
        /// <summary>
        /// 欄位顏色映射 (ColumnKey -> Color)
        /// </summary>
        public Dictionary<string, Color> ColumnColors { get; set; } = new Dictionary<string, Color>();
        
        /// <summary>
        /// 段落邊界框（用於繪製大框）
        /// </summary>
        public List<ParagraphDebugInfo> ParagraphBounds { get; set; } = new List<ParagraphDebugInfo>();
        
        /// <summary>
        /// 合併的原始OCR行索引（用於識別哪些框是合併後的）
        /// </summary>
        public HashSet<int> MergedOriginalIndices { get; set; } = new HashSet<int>();
    }

    /// <summary>
    /// 段落調試信息
    /// </summary>
    public class ParagraphDebugInfo
    {
        public string ColumnKey { get; set; }
        public string ParagraphId { get; set; }
        public Rectangle BoundingBox { get; set; }
        public Color Color { get; set; }
        public List<Rectangle> OriginalLineBounds { get; set; } = new List<Rectangle>();
    }

    /// <summary>
    /// 版面段落
    /// </summary>
    public class LayoutParagraph
    {
        public string ParagraphId { get; set; }
        public List<LayoutLine> Lines { get; set; }
        public Rectangle BoundingBox { get; set; }
        
        /// <summary>
        /// 所屬欄位的顏色（用於視覺化）
        /// </summary>
        public Color ColumnColor { get; set; }
    }

    /// <summary>
    /// 版面文字行（增強版OCR行）
    /// </summary>
    public class LayoutLine
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
        public int LineHeight { get; set; }
        public int OriginalIndex { get; set; } // 原始OCR行索引
        
        /// <summary>
        /// 合併來源的原始索引列表（如果這一行是合併而來的）
        /// </summary>
        public List<int> MergedFromIndices { get; set; } = new List<int>();
        
        /// <summary>
        /// 是否為合併產生的行
        /// </summary>
        public bool IsMerged => MergedFromIndices.Count > 1;
    }
}
