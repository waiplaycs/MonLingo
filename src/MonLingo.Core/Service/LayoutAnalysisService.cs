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
                Console.WriteLine($"[版面分析調試] {message}");
                Logger.Debug($"[版面分析調試] {message}");
                
                // 同時輸出到系統調試輸出
                System.Diagnostics.Debug.WriteLine($"[版面分析調試] {message}");
            }
        }

        /// <summary>
        /// 階段一調試：記錄識別框合併過程
        /// </summary>
        /// <param name="boxA">文字框A</param>
        /// <param name="boxB">文字框B</param>
        /// <param name="stage">處理階段</param>
        /// <param name="result">處理結果</param>
        private void DebugStage1(Rectangle boxA, Rectangle boxB, string stage, string result)
        {
            if (EnableDebugMode)
            {
                DebugLog($"🔍 階段一-{stage}: 框A({boxA.X},{boxA.Y},{boxA.Width}×{boxA.Height}) + 框B({boxB.X},{boxB.Y},{boxB.Width}×{boxB.Height}) → {result}");
            }
        }

        /// <summary>
        /// 階段二調試：記錄分欄檢測過程
        /// </summary>
        /// <param name="lineIndex">行索引</param>
        /// <param name="line">文字行</param>
        /// <param name="columnIndex">分配的欄位索引</param>
        /// <param name="reason">分配原因</param>
        private void DebugStage2(int lineIndex, LayoutLine line, int columnIndex, string reason)
        {
            if (EnableDebugMode)
            {
                DebugLog($"📂 階段二-分欄: 行{lineIndex} 「{line.Text.Substring(0, Math.Min(20, line.Text.Length))}...」 → 欄位{columnIndex} ({reason})");
            }
        }

        /// <summary>
        /// 階段三調試：記錄段落分割過程
        /// </summary>
        /// <param name="columnKey">欄位鍵值</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="line">文字行</param>
        /// <param name="paragraphIndex">段落索引</param>
        /// <param name="action">執行動作</param>
        private void DebugStage3(string columnKey, int lineIndex, LayoutLine line, int paragraphIndex, string action)
        {
            if (EnableDebugMode)
            {
                DebugLog($"📑 階段三-段落: {columnKey} 行{lineIndex} 「{line.Text.Substring(0, Math.Min(15, line.Text.Length))}...」 → 段落{paragraphIndex} ({action})");
            }
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
                Logger.Info("📑 階段三：開始段落分段");
                DebugLog($"📑 階段三：對 {columns.Count} 個欄位執行段落分割");
                var layoutResult = PerformParagraphSegmentation(columns);
                Logger.Info($"✅ 階段三完成：生成 {layoutResult.Count} 個欄位的段落結構");
                
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
        /// 階段一：橫向行合併
        /// 將因OCR辨識而產生的、在同一水平線上的文字碎片，拼接成語義上完整的單行文字
        /// 採用兩階段嚴謹合併演算法
        /// </summary>
        /// <param name="ocrLines">原始OCR行結果</param>
        /// <returns>合併後的完整文字行列表</returns>
        private List<LayoutLine> PerformHorizontalLineMerging(OcrLine[] ocrLines)
        {
            Logger.Debug($"🔄 開始橫向行合併，輸入 {ocrLines.Length} 個OCR行");

            // 轉換為內部格式
            var layoutLines = ocrLines.Select((line, index) => new LayoutLine
            {
                Text = line.Text,
                Confidence = line.Confidence,
                BoundingBox = line.BoundingBox,
                LineHeight = line.BoundingBox.Height,
                OriginalIndex = index,
                MergedFromIndices = new List<int> { index } // 初始狀態，每行對應自己的索引
            }).ToList();

            // 建立合併候選對列表
            var mergeOperations = new List<MergeCandidate>();

            // 遍歷所有行對，檢查合併可能性
            for (int i = 0; i < layoutLines.Count; i++)
            {
                for (int j = i + 1; j < layoutLines.Count; j++)
                {
                    var boxA = layoutLines[i];
                    var boxB = layoutLines[j];

                    DebugStage1(boxA.BoundingBox, boxB.BoundingBox, "開始檢測", $"檢查行{i}與行{j}的合併可能性");

                    // 關卡一：垂直重疊率檢查
                    if (!CheckVerticalOverlap(boxA.BoundingBox, boxB.BoundingBox))
                    {
                        DebugStage1(boxA.BoundingBox, boxB.BoundingBox, "垂直重疊檢查", "未通過-垂直重疊率不足");
                        continue; // 不通過，跳過此對
                    }

                    // 關卡二：相對水平間距檢查
                    if (!CheckHorizontalSpacing(boxA.BoundingBox, boxB.BoundingBox))
                    {
                        DebugStage1(boxA.BoundingBox, boxB.BoundingBox, "水平間距檢查", "未通過-水平間距過大");
                        continue; // 不通過，跳過此對
                    }

                    // 兩個關卡都通過，加入合併候選
                    var candidate = new MergeCandidate
                    {
                        IndexA = i,
                        IndexB = j,
                        HorizontalDistance = CalculateHorizontalDistance(boxA.BoundingBox, boxB.BoundingBox)
                    };
                    mergeOperations.Add(candidate);

                    DebugStage1(boxA.BoundingBox, boxB.BoundingBox, "合併候選", $"通過所有檢查-距離{candidate.HorizontalDistance}px");

                    Logger.Debug($"✅ 發現合併候選：行{i}「{boxA.Text}」+ 行{j}「{boxB.Text}」（距離={mergeOperations.Last().HorizontalDistance}px）");
                }
            }

            // 按水平距離排序，優先合併距離最近的
            mergeOperations.Sort((a, b) => a.HorizontalDistance.CompareTo(b.HorizontalDistance));

            // 執行合併操作
            var merged = new bool[layoutLines.Count]; // 標記已合併的行
            var result = new List<LayoutLine>();

            DebugLog($"🔄 開始執行 {mergeOperations.Count} 個合併操作");

            foreach (var operation in mergeOperations)
            {
                // 檢查兩行是否已被合併
                if (merged[operation.IndexA] || merged[operation.IndexB])
                {
                    DebugStage1(layoutLines[operation.IndexA].BoundingBox, layoutLines[operation.IndexB].BoundingBox, 
                        "合併檢查", "跳過-其中一行已被合併");
                    continue;
                }

                // 執行合併
                var lineA = layoutLines[operation.IndexA];
                var lineB = layoutLines[operation.IndexB];
                var mergedLine = MergeTwoLines(lineA, lineB);

                result.Add(mergedLine);
                merged[operation.IndexA] = true;
                merged[operation.IndexB] = true;

                DebugStage1(lineA.BoundingBox, lineB.BoundingBox, "合併執行", 
                    $"成功合併-新文字「{mergedLine.Text.Substring(0, Math.Min(30, mergedLine.Text.Length))}...」");

                Logger.Debug($"🔗 合併執行：「{lineA.Text}」+「{lineB.Text}」→「{mergedLine.Text}」");
            }

            // 添加未被合併的行
            for (int i = 0; i < layoutLines.Count; i++)
            {
                if (!merged[i])
                {
                    result.Add(layoutLines[i]);
                    DebugStage1(layoutLines[i].BoundingBox, new Rectangle(), "獨立保留", 
                        $"行{i}未合併-文字「{layoutLines[i].Text.Substring(0, Math.Min(20, layoutLines[i].Text.Length))}...」");
                    Logger.Debug($"📝 保留獨立行：「{layoutLines[i].Text}」");
                }
            }

            Logger.Info($"🎯 橫向行合併完成：{ocrLines.Length} → {result.Count} 行");
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
        /// 階段二：智能分欄
        /// 使用有序聚類演算法識別畫面中的獨立文字區塊或欄位
        /// </summary>
        /// <param name="mergedLines">階段一輸出的完整文字行列表</param>
        /// <returns>按欄位分組的文字行列表</returns>
        private List<List<LayoutLine>> PerformIntelligentColumnDetection(List<LayoutLine> mergedLines)
        {
            Logger.Debug($"🔄 開始智能分欄，輸入 {mergedLines.Count} 個完整文字行");

            if (mergedLines.Count == 0)
            {
                Logger.Warn("⚠️ 輸入文字行為空，返回空欄位列表");
                return new List<List<LayoutLine>>();
            }

            // 步驟1：全局預排序（從上到下、從左到右）
            Logger.Debug("📋 步驟1：全局預排序");
            var sortedLines = GlobalPreSort(mergedLines);

            // 步驟2：有序觸發「滾雪球」聚類
            Logger.Debug("🌪️ 步驟2：開始有序滾雪球聚類");
            var columns = OrderedSnowballClustering(sortedLines);

            Logger.Info($"🎯 智能分欄完成：{mergedLines.Count} 行 → {columns.Count} 個欄位");
            
            // 記錄每個欄位的詳細信息
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var bbox = CalculateColumnBoundingBox(column);
                Logger.Debug($"📂 欄位{i + 1}：{column.Count} 行，範圍({bbox.X},{bbox.Y},{bbox.Width},{bbox.Height})");
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
        /// 有序觸發「滾雪球」聚類
        /// </summary>
        /// <param name="sortedLines">已排序的文字行列表</param>
        /// <returns>按欄位分組的文字行列表</returns>
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
        /// 執行段落分割（階段三）
        /// 使用加權評分系統分析每個欄位中的文字行，進行智能段落分組
        /// </summary>
        /// <param name="columns">已分欄的文字行</param>
        /// <returns>分割後的段落字典</returns>
        private Dictionary<string, List<LayoutParagraph>> PerformParagraphSegmentation(List<List<LayoutLine>> columns)
        {
            var result = new Dictionary<string, List<LayoutParagraph>>();
            
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var column = columns[columnIndex];
                if (column.Count == 0) continue;

                string columnKey = $"column_{columnIndex + 1}";
                var columnColor = GetColumnColor(columnIndex);
                
                DebugLog($"   處理 {columnKey}：{column.Count} 行，使用顏色 #{columnColor.Name}");
                
                var paragraphs = SegmentColumnIntoParagraphs(column, columnColor, columnKey);
                result[columnKey] = paragraphs;
                
                DebugLog($"   {columnKey} 分割結果：{paragraphs.Count} 個段落");
            }

            return result;
        }

        /// <summary>
        /// 將單個欄位分割成段落
        /// </summary>
        /// <param name="column">欄位中的文字行</param>
        /// <param name="columnColor">欄位顏色</param>
        /// <param name="columnKey">欄位鍵值</param>
        /// <returns>分割後的段落列表</returns>
        private List<LayoutParagraph> SegmentColumnIntoParagraphs(List<LayoutLine> column, Color columnColor, string columnKey)
        {
            var paragraphs = new List<LayoutParagraph>();
            
            if (column.Count == 0) return paragraphs;

            // 初始化第一個段落
            var currentParagraph = new List<LayoutLine> { column[0] };
            DebugStage3(columnKey, 0, column[0], 0, "新段落起始行");
            
            for (int i = 1; i < column.Count; i++)
            {
                var currentLine = column[i];
                var previousLine = column[i - 1];
                
                // 加權評分系統判斷是否分段
                int score = CalculateContinuityScore(previousLine, currentLine);
                
                DebugStage3(columnKey, i, currentLine, paragraphs.Count, $"連續性評分{score}");
                DebugLog($"     行 {i}: \"{currentLine.Text}\" 與前一行連續性評分: {score}");
                
                // 門檻值：75分（根據PRD規範）
                if (score >= 75)
                {
                    // 繼續當前段落
                    currentParagraph.Add(currentLine);
                    DebugStage3(columnKey, i, currentLine, paragraphs.Count, $"繼續段落-評分{score}≥75");
                    DebugLog($"     -> 歸入當前段落（評分 {score} ≥ 75）");
                }
                else
                {
                    // 結束當前段落，開始新段落
                    if (currentParagraph.Count > 0)
                    {
                        paragraphs.Add(CreateParagraph(currentParagraph, paragraphs.Count, columnColor));
                        DebugStage3(columnKey, i-1, previousLine, paragraphs.Count-1, $"段落結束-共{currentParagraph.Count}行");
                        DebugLog($"     -> 創建新段落 {paragraphs.Count}（評分 {score} < 75）");
                    }
                    currentParagraph = new List<LayoutLine> { currentLine };
                    DebugStage3(columnKey, i, currentLine, paragraphs.Count, $"新段落開始-評分{score}<75");
                }
            }
            
            // 處理最後一個段落
            if (currentParagraph.Count > 0)
            {
                paragraphs.Add(CreateParagraph(currentParagraph, paragraphs.Count, columnColor));
                DebugStage3(columnKey, column.Count-1, column[column.Count-1], paragraphs.Count-1, $"最終段落-共{currentParagraph.Count}行");
                DebugLog($"     -> 完成最後段落 {paragraphs.Count}");
            }

            return paragraphs;
        }

        /// <summary>
        /// 計算兩行之間的連續性評分（加權評分系統）
        /// </summary>
        /// <param name="line1">前一行</param>
        /// <param name="line2">當前行</param>
        /// <returns>連續性評分（0-100分）</returns>
        private int CalculateContinuityScore(LayoutLine line1, LayoutLine line2)
        {
            int totalScore = 0;

            // 1. 垂直距離評分（30分）
            int verticalScore = CalculateVerticalDistanceScore(line1, line2);
            totalScore += verticalScore;

            // 2. 對齊一致性評分（25分）
            int alignmentScore = CalculateAlignmentScore(line1, line2);
            totalScore += alignmentScore;

            // 3. 字體高度相似性評分（25分）
            int fontHeightScore = CalculateFontHeightScore(line1, line2);
            totalScore += fontHeightScore;

            // 4. 寬度比例評分（20分）
            int widthRatioScore = CalculateWidthRatioScore(line1, line2);
            totalScore += widthRatioScore;

            return Math.Min(100, totalScore); // 確保不超過100分
        }

        /// <summary>
        /// 計算垂直距離評分
        /// </summary>
        private int CalculateVerticalDistanceScore(LayoutLine line1, LayoutLine line2)
        {
            int verticalDistance = line2.BoundingBox.Top - line1.BoundingBox.Bottom;
            int avgLineHeight = (line1.LineHeight + line2.LineHeight) / 2;
            
            if (avgLineHeight == 0) return 0;

            double ratio = (double)verticalDistance / avgLineHeight;
            
            // 根據PRD規範：距離越小評分越高
            if (ratio <= 0.5) return 30; // 非常緊密
            if (ratio <= 1.0) return 25; // 緊密
            if (ratio <= 1.5) return 15; // 中等
            if (ratio <= 2.0) return 5;  // 稍遠
            return 0; // 太遠
        }

        /// <summary>
        /// 計算對齊一致性評分
        /// </summary>
        private int CalculateAlignmentScore(LayoutLine line1, LayoutLine line2)
        {
            int leftAlignment = Math.Abs(line1.BoundingBox.Left - line2.BoundingBox.Left);
            int avgWidth = (line1.BoundingBox.Width + line2.BoundingBox.Width) / 2;
            
            if (avgWidth == 0) return 0;

            double alignmentRatio = (double)leftAlignment / avgWidth;
            
            // 對齊越好評分越高
            if (alignmentRatio <= 0.05) return 25; // 完美對齊
            if (alignmentRatio <= 0.1) return 20;  // 良好對齊
            if (alignmentRatio <= 0.2) return 15;  // 中等對齊
            if (alignmentRatio <= 0.3) return 10;  // 稍微偏移
            return 5; // 對齊不佳
        }

        /// <summary>
        /// 計算字體高度相似性評分
        /// </summary>
        private int CalculateFontHeightScore(LayoutLine line1, LayoutLine line2)
        {
            if (line1.LineHeight == 0 || line2.LineHeight == 0) return 0;

            double heightRatio = (double)Math.Min(line1.LineHeight, line2.LineHeight) / 
                                Math.Max(line1.LineHeight, line2.LineHeight);
            
            // 高度越相似評分越高
            if (heightRatio >= 0.9) return 25; // 非常相似
            if (heightRatio >= 0.8) return 20; // 相似
            if (heightRatio >= 0.7) return 15; // 中等相似
            if (heightRatio >= 0.6) return 10; // 稍微不同
            return 5; // 差異較大
        }

        /// <summary>
        /// 計算寬度比例評分
        /// </summary>
        private int CalculateWidthRatioScore(LayoutLine line1, LayoutLine line2)
        {
            if (line1.BoundingBox.Width == 0 || line2.BoundingBox.Width == 0) return 0;

            double widthRatio = (double)Math.Min(line1.BoundingBox.Width, line2.BoundingBox.Width) / 
                               Math.Max(line1.BoundingBox.Width, line2.BoundingBox.Width);
            
            // 寬度比例越接近評分越高
            if (widthRatio >= 0.8) return 20; // 非常接近
            if (widthRatio >= 0.6) return 15; // 接近
            if (widthRatio >= 0.4) return 10; // 中等
            if (widthRatio >= 0.2) return 5;  // 差異較大
            return 0; // 差異很大
        }

        /// <summary>
        /// 創建段落對象
        /// </summary>
        private LayoutParagraph CreateParagraph(List<LayoutLine> lines, int paragraphIndex, Color columnColor)
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
