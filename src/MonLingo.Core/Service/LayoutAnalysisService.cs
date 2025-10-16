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
    /// 基於 v3.4 規格實作三階段版面分析：
    /// 階段一：橫向行合併 (Horizontal Line Merging)
    /// 階段二：智能分欄 (Intelligent Column Detection) 
    /// 階段三：段落分段 (Paragraph Segmentation)
    /// v4.0: 整合雙通道決策架構
    /// </summary>
    public partial class LayoutAnalysisService
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
                // 輸出到控制台和日誌 - v3.5索引穩定性版本
                Console.WriteLine($"[MonLingo v3.5版面分析] {message}");
                Logger.Debug($"[MonLingo v3.5版面分析] {message}");
                
                // 同時輸出到系統調試輸出
                System.Diagnostics.Debug.WriteLine($"[MonLingo v3.5版面分析] {message}");
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
                DebugLog($"   ✂️ v3.5欄位修剪: 行{currentLineIndex}觸發 | 欄位{columnIndex}({columnSize}行) | 距離:{verticalDistance:F1}px > 閾值:{threshold:F1}px");
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
        /// <param name="actualSpacing">實際行距</param>
        /// <param name="standardSpacing">標準行距</param>
        private void DebugStage3WeightedScore(int lineIndex, double relativeDistanceScore, double fontHeightPenalty, 
            double alignmentPenalty, double overlapBonus, double totalScore, double threshold, bool willMerge,
            double actualSpacing = -1, double standardSpacing = -1)
        {
            if (EnableDebugMode)
            {
                string decision = willMerge ? "🔗合併" : "✂️分割";
                string spacingInfo = actualSpacing >= 0 && standardSpacing > 0 
                    ? $" | 行距:{actualSpacing:F1}px(標準{standardSpacing:F1}px)" 
                    : "";
                DebugLog($"   🧮 v3加權分析: 行{lineIndex} | 距離:{relativeDistanceScore:F2} | 字體懲罰:-{fontHeightPenalty:F2} | 對齊懲罰:-{alignmentPenalty:F2} | 重疊:+{overlapBonus:F1}{spacingInfo} | 總分:{totalScore:F2}/{threshold} | {decision}");
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
        /// 雙峰模型調試輸出方法
        /// </summary>
        private void DebugBiPeakModel(string columnKey, BiPeakSpacingModel model, List<double> spacings)
        {
            if (!EnableDebugMode) return;

            Console.WriteLine($"🔬 【v3.1雙峰模型分析】欄位: {columnKey}");
            Console.WriteLine($"  📊 間距統計: 共{model.TotalSpacings}個間距，{model.ValidPeaks}個有效峰值");
            Console.WriteLine($"  🎯 關鍵峰值: 合併峰值={model.PeakMerge:F1}px，分割峰值={model.PeakSplit:F1}px");
            Console.WriteLine($"  📏 容差設定: {model.Tolerance:F1}px ({model.PeakMerge * 0.2:F1}px = 合併峰值 × 0.2)");
            Console.WriteLine($"  🟢 合併區間: [0, {model.MergeZone.Max:F1}px]");
            Console.WriteLine($"  🔴 分割區間: [{model.SplitZone.Min:F1}px, ∞)");
            Console.WriteLine($"  🟡 模糊區間: ({model.AmbiguityZone.Min:F1}px, {model.AmbiguityZone.Max:F1}px)");
            
            if (spacings.Count > 0)
            {
                Console.WriteLine($"  📈 間距分佈: 最小={spacings.Min():F1}px，最大={spacings.Max():F1}px，聚類閾值={model.ClusterThreshold:F1}px");
            }
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
        /// 完整版面分析入口 (舊版 - 已廢棄)
        /// 執行三階段處理流程，返回結構化版面數據
        /// </summary>
        /// <param name="ocrResult">原始OCR識別結果</param>
        /// <returns>結構化版面分析結果</returns>
        /// <remarks>
        /// ⚠️ 此方法已被 AnalyzeLayoutV2() 替代
        /// 新架構使用AI驅動的智能翻譯層處理段落合併
        /// 建議使用: AnalyzeLayoutV2() + AITranslationService.SmartTranslateBatchAsync()
        /// </remarks>
        [Obsolete("此方法已廢棄,請使用 AnalyzeLayoutV2() 配合 AITranslationService 進行智能翻譯", false)]
        public LayoutAnalysisResult AnalyzeLayout(OcrResult ocrResult)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                Logger.Info("🔍 開始執行高級版面分析 - MonLingo v4.1");
                DebugLog("=== MonLingo v4.1版面分析開始 ===");
                
                if (EnableDebugMode)
                {
                    Console.WriteLine("🚀 MonLingo v4.1 版面分析引擎啟動");
                    Console.WriteLine("📈 新增功能：自適應聚類閾值策略 (取代固定倍數0.25)");
                    Console.WriteLine("🧠 智能算法：自然間隙分析 + 變異係數校驗");
                    Console.WriteLine("⚡ 改進效果：自動適應不同文檔密度");
                    Console.WriteLine("=====================================");
                }
                
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

                // 全局預排序 (供智能分欄使用)
                Logger.Info("📝 全局預排序：按閱讀順序排序文字行");
                DebugLog($"📝 全局預排序：輸入 {ocrResult.Lines.Length} 行文字");
                var sortedLines = PerformGlobalPreSorting(ocrResult.Lines);
                Logger.Info($"✅ 全局預排序完成：{sortedLines.Count} 行已排序");
                DebugLog($"✅ 全局預排序結果：{sortedLines.Count} 行按「從上到下、從左到右」排序");

                // 階段二：智能分欄 (直接使用排序後的行,不再執行橫向合併)
                Logger.Info("📂 階段二：開始智能分欄");
                DebugLog($"📂 階段二：對 {sortedLines.Count} 行執行分欄檢測");
                var columns = PerformIntelligentColumnDetection(sortedLines);
                Logger.Info($"✅ 階段二完成：識別出 {columns.Count} 個欄位");
                DebugLog($"✅ 階段二結果：識別出 {columns.Count} 個欄位");
                
                // 顯示欄位詳細信息
                for (int i = 0; i < columns.Count; i++)
                {
                    DebugLog($"   欄位 {i + 1}: {columns[i].Count} 行，顏色 #{GetColumnColor(i).Name}");
                }

                // 階段三：段落分段 (v4.0 雙通道架構)
                Logger.Info("📑 v4階段三：開始雙通道段落檢測");
                Console.WriteLine("📑 v4階段三：開始雙通道段落檢測");
                DebugLog($"📑 v4階段三：對 {columns.Count} 個欄位執行雙通道段落分割");
                var layoutResult = PerformParagraphSegmentationV4(columns);
                Logger.Info($"✅ v4階段三完成：生成 {layoutResult.Count} 個欄位的段落結構 (雙通道架構)");
                Console.WriteLine($"✅ v4階段三完成：生成 {layoutResult.Count} 個欄位的段落結構 (雙通道架構)");
                

                
                // 返回最終的結構化版面數據
                var tempLayout = layoutResult;

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                Logger.Info($"🎯 MonLingo v4.1版面分析完成，耗時 {processingTime:F1}ms");
                DebugLog($"🎯 MonLingo v4.1版面分析完成，總耗時 {processingTime:F1}ms");
                
                if (EnableDebugMode)
                {
                    Console.WriteLine("=====================================");
                    Console.WriteLine($"✅ MonLingo v4.1 版面分析完成！");
                    Console.WriteLine($"⏱️ 總處理時間: {processingTime:F1}ms");
                    Console.WriteLine($"📊 結果統計: {layoutResult.Count}個欄位");
                    Console.WriteLine($"🧠 v4.1智能閾值策略已應用");
                    Console.WriteLine("=====================================");
                }

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
        /// 全局預排序 (Global Pre-sorting)
        /// 將所有OCR文字行按「從上到下、從左到右」的閱讀順序排序
        /// 這是智能分欄的基礎,確保處理順序符合閱讀直覺
        /// </summary>
        /// <param name="ocrLines">原始OCR行結果</param>
        /// <returns>排序後的文字行列表</returns>
        private List<LayoutLine> PerformGlobalPreSorting(OcrLine[] ocrLines)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Logger.Debug($"🔄 開始全局預排序，輸入 {ocrLines.Length} 個OCR行");
            Logger.Debug($"📊 排序規則：按Y座標升序(從上到下)，Y相同時按X座標升序(從左到右)");

            // 全局預排序 - 先按Y座標升序，Y座標相同時再按X座標升序
            // 確保處理順序符合「從上到下、從左到右」的閱讀直覺
            var sortedLines = ocrLines.Select((line, index) => new LayoutLine
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

            Logger.Info($"🎯 全局預排序完成：{ocrLines.Length} 行已排序 (算法複雜度: O(n log n))");
            
            stopwatch.Stop();
            DebugStagePerformance("全局預排序", ocrLines.Length, sortedLines.Count, stopwatch.Elapsed.TotalMilliseconds, "O(n log n)");
            
            return sortedLines;
        }

        /// <summary>
        /// 階段一：橫向行合併 (v3版本) - 已廢棄
        /// ⚠️ 此方法已不再使用,改為只執行全局預排序
        /// 原功能：將因OCR辨識而產生的、在同一水平線上的文字碎片，拼接成語義上完整的單行文字
        /// </summary>
        /// <param name="ocrLines">原始OCR行結果</param>
        /// <returns>合併後的完整文字行列表</returns>
        [Obsolete("階段一橫向行合併已移除,請使用 PerformGlobalPreSorting 進行全局預排序")]
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
        /// v3.5更新：修剪欄位不重新索引，保持索引穩定性
        /// </summary>
        /// <param name="sortedLines">已排序的文字行列表</param>
        /// <returns>按欄位分組的文字行列表</returns>
        private List<List<LayoutLine>> SinglePassOrderedClusteringV3(List<LayoutLine> sortedLines)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Logger.Debug($"🚀 v3單次遍歷開始，處理 {sortedLines.Count} 行");

            // v3.5核心數據結構：使用標記陣列避免索引混亂
            var activeColumns = new List<List<LayoutLine>>(); // 活躍欄位列表（不物理移除）
            var prunedColumns = new List<bool>(); // 標記陣列：追蹤哪些欄位已被修剪
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

                // v3.5步驟1：先執行欄位修剪檢查（在歸屬檢查之前）
                PerformColumnPruningV3_5(currentLine, activeColumns, prunedColumns, completedColumns, globalAvgLineSpacing, i);

                // v3步驟2：歸屬判斷 - 嘗試將當前行歸屬到活躍欄位
                assigned = ProceedToOwnershipCheckV3_5(currentLine, activeColumns, prunedColumns, i);

                // 如果無法歸入任何活躍欄位，創建新欄位
                if (!assigned)
                {
                    var newColumn = new List<LayoutLine> { currentLine };
                    activeColumns.Add(newColumn);
                    prunedColumns.Add(false); // 新欄位初始狀態為未修剪
                    
                    DebugStage2V3(i, currentLine, activeColumns.Count - 1, "新欄位種子");
                    Logger.Debug($"🌱 v3新種子：行{i}創建欄位{activeColumns.Count - 1}");
                }
            }

            // 將所有未修剪的活躍欄位移入已完成列表
            for (int i = 0; i < activeColumns.Count; i++)
            {
                if (!prunedColumns[i] && activeColumns[i].Count > 0)
                {
                    completedColumns.Add(activeColumns[i]);
                }
            }

            Logger.Debug($"✅ v3.5聚類完成：{completedColumns.Count} 個欄位，活躍欄位最大數={activeColumns.Count}");
            
            stopwatch.Stop();
            int totalLines = completedColumns.Sum(col => col.Count);
            DebugStagePerformance("v3.5階段二-單次遍歷有序聚類(索引穩定)", sortedLines.Count, completedColumns.Count, stopwatch.Elapsed.TotalMilliseconds, "O(n×m)");
            
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
                
                // v3.1改進：保留負間距（重疊），提升雙峰模型精度
                double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
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
        /// v3.5算法：歸屬判斷 - 嘗試將當前行歸屬到現有的活躍欄位（跳過已修剪欄位）
        /// 核心改進：避免索引混亂，跳過已修剪欄位而不物理移除
        /// </summary>
        /// <param name="currentLine">當前處理的文字行</param>
        /// <param name="activeColumns">活躍欄位列表</param>
        /// <param name="prunedColumns">修剪標記陣列</param>
        /// <param name="lineIndex">行索引</param>
        /// <returns>是否成功歸屬</returns>
        private bool ProceedToOwnershipCheckV3_5(LayoutLine currentLine, List<List<LayoutLine>> activeColumns, List<bool> prunedColumns, int lineIndex)
        {
            // v3.5調試：輸出當前欄位狀態
            DebugLog($"   🔍 v3.5歸屬檢查開始：行{lineIndex}，活躍欄位數={activeColumns.Count}，修剪陣列長度={prunedColumns.Count}");
            
            // v3.5調試：顯示修剪陣列狀態
            string prunedStatus = "";
            for (int i = 0; i < prunedColumns.Count; i++)
            {
                prunedStatus += $"欄位{i}:{(prunedColumns[i] ? "已修剪" : "活躍")} ";
            }
            DebugLog($"   🗂️ 修剪狀態：{prunedStatus}");
            
            // 遍歷所有活躍欄位，跳過已修剪的欄位
            for (int columnIndex = 0; columnIndex < activeColumns.Count; columnIndex++)
            {
                // v3.5關鍵：跳過已修剪的欄位，避免索引混亂
                if (columnIndex >= prunedColumns.Count)
                {
                    DebugLog($"   ⚠️ v3.5索引錯誤：欄位{columnIndex}超出修剪陣列範圍{prunedColumns.Count}");
                    break;
                }
                
                if (prunedColumns[columnIndex])
                {
                    DebugLog($"   ⏭️ v3.5跳過已修剪欄位{columnIndex}");
                    continue;
                }

                var column = activeColumns[columnIndex];
                
                // v3標準：檢查歸屬條件
                if (CanAssignToColumnV3(currentLine, column, columnIndex, lineIndex))
                {
                    // 成功歸屬：將行添加到欄位並更新邊界
                    column.Add(currentLine);
                    
                    DebugStage2V3(lineIndex, currentLine, columnIndex, "歸屬成功");
                    Logger.Debug($"✅ v3.5歸屬：行{lineIndex}加入欄位{columnIndex}（欄位大小：{column.Count}）");
                    
                    return true; // 找到歸屬後立即返回
                }
            }
            
            DebugLog($"   ❌ v3.5歸屬失敗：行{lineIndex}無法歸入任何活躍欄位");
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
        /// v3.5算法：混合模式欄位修剪（標記模式）
        /// 核心改進：不物理移除欄位，只標記為已修剪，避免索引混亂
        /// </summary>
        /// <param name="currentLine">當前處理的文字行</param>
        /// <param name="activeColumns">活躍欄位列表</param>
        /// <param name="prunedColumns">修剪標記陣列</param>
        /// <param name="completedColumns">已完成欄位列表</param>
        /// <param name="globalAvgLineSpacing">全局平均行間距</param>
        /// <param name="currentLineIndex">當前行索引（用於調試）</param>
        private void PerformColumnPruningV3_5(LayoutLine currentLine, List<List<LayoutLine>> activeColumns, 
            List<bool> prunedColumns, List<List<LayoutLine>> completedColumns, double globalAvgLineSpacing, int currentLineIndex = -1)
        {
            // v3混合修剪條件：基於全局平均行間距的動態閾值
            double pruningThreshold = globalAvgLineSpacing * 3.0; // v3標準：3倍行間距
            
            // v3.5調試：詳細距離檢查輸出
            DebugLog($"   🔍 v3.5修剪檢查：行{currentLineIndex}，閾值={pruningThreshold:F1}px，當前行Top={currentLine.BoundingBox.Top:F1}px");
            
            // 檢查每個活躍欄位是否應該被修剪
            for (int i = 0; i < activeColumns.Count; i++)
            {
                // 跳過已經修剪的欄位
                if (prunedColumns[i]) continue;
                
                var column = activeColumns[i];
                if (column.Count == 0) continue;
                
                // 計算當前行與欄位底部的垂直距離
                var columnBottom = column.Max(line => line.BoundingBox.Bottom);
                double verticalDistance = Math.Max(0, currentLine.BoundingBox.Top - columnBottom);
                
                // v3.5調試：顯示每個欄位的距離計算
                DebugLog($"   📏 欄位{i}距離檢查：欄位底部={columnBottom:F1}px，距離={verticalDistance:F1}px，是否修剪={(verticalDistance > pruningThreshold ? "是" : "否")}");
                
                // 如果距離超過修剪閾值，標記為已修剪
                if (verticalDistance > pruningThreshold)
                {
                    // v3.5關鍵：只標記為已修剪，不物理移除
                    prunedColumns[i] = true;
                    completedColumns.Add(column);
                    
                    DebugStage2Pruning(currentLineIndex, i, column.Count, verticalDistance, pruningThreshold);
                    DebugLog($"   ✂️ v3.5修剪：欄位{i}距離{verticalDistance:F1}px > 閾值{pruningThreshold:F1}px，標記已修剪（索引保持）");
                }
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
            
            // v3.5調試：顯示歸屬檢查開始
            DebugLog($"   🔍 v3.5開始檢查欄位{columnIndex}：行{lineIndex}");
            
            // v3標準1：垂直鄰近度檢查
            // 計算當前行與欄位中所有行的最小垂直距離
            double minVerticalDistance = double.MaxValue;
            double avgLineHeightInColumn = column.Average(line => line.BoundingBox.Height);
            
            foreach (var existingLine in column)
            {
                double verticalDist = CalculateVerticalDistance(currentBox, existingLine.BoundingBox);
                minVerticalDistance = Math.Min(minVerticalDistance, verticalDist);
            }
            
            double verticalThreshold = avgLineHeightInColumn * 1.5; // v3.5標準：1.5倍欄位內平均行高（解決小字型行影響）
            bool verticalPassed = minVerticalDistance <= verticalThreshold;
            
            // v3.5調試：顯示垂直檢查結果
            DebugLog($"   📏 v3.5垂直檢查欄位{columnIndex}：距離={minVerticalDistance:F1}px，閾值={verticalThreshold:F1}px，通過={verticalPassed}");
            
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
        /// 階段三：混合模式段落分割 (v3版本 - 已廢棄)
        /// 採用混合模式段落檢測策略，為不同類型的內容提供最優化的處理路徑
        /// </summary>
        /// <param name="columns">已分欄的文字行</param>
        /// <returns>分割後的段落字典</returns>
        /// <remarks>
        /// ⚠️ 此方法已被AI驅動的智能翻譯層替代
        /// 新架構: AnalyzeLayoutV2() → AITranslationService.SmartTranslateBatchAsync()
        /// AI翻譯層會自動處理段落合併,無需算法計算
        /// </remarks>
        [Obsolete("此方法已廢棄,段落合併現由AI翻譯層智能處理", false)]
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
        /// v3算法：混合模式段落檢測 (Hybrid Paragraph Detection - 已廢棄)
        /// 步驟一：內容類型預檢查
        /// 步驟二：計算標準行距
        /// 步驟三：多指標加權決策系統
        /// v4.0更新：集成雙通道架構調試輸出
        /// </summary>
        /// <param name="column">欄位中的文字行</param>
        /// <param name="columnColor">欄位顏色</param>
        /// <param name="columnKey">欄位鍵值</param>
        /// <returns>分割後的段落列表</returns>
        /// <remarks>
        /// ⚠️ 此複雜算法已被AI智能翻譯替代
        /// 新方案使用GPT-4o-mini進行語義理解和段落合併
        /// 優勢: 更高準確率、無需參數調優、支持多語言
        /// </remarks>
        [Obsolete("此複雜算法已廢棄,改用AI智能翻譯進行段落合併", false)]
        private List<LayoutParagraph> HybridParagraphDetectionV3(List<LayoutLine> column, Color columnColor, string columnKey)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            if (column.Count == 0) return new List<LayoutParagraph>();

            Logger.Debug($"🚀 v3混合檢測開始：{columnKey} ({column.Count}行)");

            // v4.0 雙通道架構調試輸出
            DebugLogV4($"===============================================");
            DebugLogV4($"🎯 v4.1自適應雙通道段落檢測: {columnKey}");
            DebugLogV4($"📄 輸入數據: {column.Count}行文字");
            
            // v3步驟一：內容類型預檢查 (Content Type Pre-analysis)
            var contentType = AnalyzeContentTypeV3(column);
            Logger.Debug($"📊 v3內容類型：{contentType}");
            
            // v4.0 雙通道架構分析
            DebugLogV4($"🔍 階段1: 內容類型分析");
            DebugLogV4($"   📊 檢測結果: {contentType}");
            
            // 模擬雙通道選擇邏輯
            string channelChoice = "經驗規則通道"; // 當前使用v3算法
            if (column.Count >= 8)
            {
                // 計算間距樣本
                var spacings = new List<double>();
                for (int i = 1; i < column.Count; i++)
                {
                    double spacing = column[i].BoundingBox.Y - (column[i-1].BoundingBox.Y + column[i-1].BoundingBox.Height);
                    spacings.Add(Math.Max(0, spacing));
                }
                
                // 簡化的峰值檢測
                var distinctSpacings = spacings.Distinct().OrderBy(s => s).ToList();
                bool hasBimodalPattern = distinctSpacings.Count >= 3 && 
                    (distinctSpacings.Last() / Math.Max(distinctSpacings.First(), 0.001)) > 2.0;
                
                if (hasBimodalPattern)
                {
                    channelChoice = "雙峰統計通道";
                    DebugLogV4($"");
                    DebugLogV4($"┌─ 【通道選擇決策結果】 ─────────────────");
                    DebugLogV4($"│ 📊 選擇通道: {channelChoice}");
                    DebugLogV4($"│ 📈 數據品質指標:");
                    DebugLogV4($"│   • 樣本數量: {spacings.Count}");
                    DebugLogV4($"│   • 間距變化: {distinctSpacings.Count}個不同值");
                    DebugLogV4($"│   • 峰值模式: 檢測到雙峰特徵");
                    DebugLogV4($"│ 🎯 選擇原因: 統計條件充足，啟用高精度算法");
                    DebugLogV4($"└─────────────────────────────────────");
                }
                else
                {
                    DebugLogV4($"");
                    DebugLogV4($"┌─ 【通道選擇決策結果】 ─────────────────");
                    DebugLogV4($"│ 🎯 選擇通道: {channelChoice}");
                    DebugLogV4($"│ 📈 數據品質指標:");
                    DebugLogV4($"│   • 樣本數量: {spacings.Count}");
                    DebugLogV4($"│   • 間距變化: {distinctSpacings.Count}個不同值");
                    DebugLogV4($"│   • 峰值模式: 無明顯雙峰特徵");
                    DebugLogV4($"│ 🎯 選擇原因: 統計模式不明顯，使用經驗規則");
                    DebugLogV4($"└─────────────────────────────────────");
                }
            }
            else
            {
                DebugLogV4($"");
                DebugLogV4($"┌─ 【通道選擇決策結果】 ─────────────────");
                DebugLogV4($"│ 🎯 選擇通道: {channelChoice}");
                DebugLogV4($"│ 📈 數據品質指標:");
                DebugLogV4($"│   • 樣本數量: {column.Count} (不足)");
                DebugLogV4($"│ 🎯 選擇原因: 樣本數量不足，使用經驗規則");
                DebugLogV4($"└─────────────────────────────────────");
            }
            DebugLogV4($"");
            DebugLogV4($"⚙️ 階段2: {channelChoice}處理");

            switch (contentType)
            {
                case ContentType.SingleLine:
                    var singleResult = HandleSingleLineShortcutV3(column, columnColor, columnKey);
                    stopwatch.Stop();
                    
                    // v4.0 雙通道架構調試輸出 - 完成
                    DebugLogV4($"");
                    DebugLogV4($"✅ v4.0處理完成! 耗時: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                    DebugLogV4($"📊 結果統計: {column.Count}行 → {singleResult.Count}段落");
                    DebugLogV4($"🎯 使用通道: {channelChoice} (單行模式)");
                    DebugLogV4($"===============================================");
                    DebugLogV4($"");
                    
                    DebugStagePerformance("v3階段三-單行捷徑", column.Count, singleResult.Count, stopwatch.Elapsed.TotalMilliseconds, "O(1)");
                    return singleResult;
                
                case ContentType.ListItems:
                    var listResult = HandleListItemDetectionV3(column, columnColor, columnKey);
                    stopwatch.Stop();
                    
                    // v4.0 雙通道架構調試輸出 - 完成
                    DebugLogV4($"");
                    DebugLogV4($"✅ v4.0處理完成! 耗時: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                    DebugLogV4($"📊 結果統計: {column.Count}行 → {listResult.Count}段落");
                    DebugLogV4($"🎯 使用通道: {channelChoice} (列表模式)");
                    DebugLogV4($"===============================================");
                    DebugLogV4($"");
                    
                    DebugStagePerformance("v3階段三-列表項目檢測", column.Count, listResult.Count, stopwatch.Elapsed.TotalMilliseconds, "O(n)");
                    return listResult;
                
                case ContentType.ContinuousText:
                default:
                    var textResult = HandleContinuousTextV3(column, columnColor, columnKey);
                    stopwatch.Stop();
                    
                    // v4.0 雙通道架構調試輸出 - 完成
                    DebugLogV4($"");
                    DebugLogV4($"✅ v4.0處理完成! 耗時: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                    DebugLogV4($"📊 結果統計: {column.Count}行 → {textResult.Count}段落");
                    DebugLogV4($"🎯 使用通道: {channelChoice} (連續文本模式)");
                    DebugLogV4($"===============================================");
                    DebugLogV4($"");
                    
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
            Console.WriteLine($"\n┌─ 【階段3-處理通道選擇】 ───────────────────");
            
            // 1. 單行欄位捷徑 (Single-Line Shortcut)
            if (column.Count == 1)
            {
                Logger.Debug("🎯 v3預檢查：單行欄位捷徑");
                Console.WriteLine("│ 🛤️ 通道選擇：單行欄位捷徑 (Single-Line Shortcut)");
                Console.WriteLine($"│ 📋 選擇原因：欄位僅含1行，直接創建段落，跳過所有分割邏輯");
                Console.WriteLine($"└─────────────────────────────────────────\n");
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
                Console.WriteLine("│ 🛤️ 通道選擇：列表項目識別 (List Item Detection)");
                Console.WriteLine($"│ 📋 選擇原因：{listIndicatorCount}/{column.Count}行含列表符號 (比例:{listRatio:F2} ≥ 0.5)，啟用基於縮排和符號的快速分割");
                Console.WriteLine($"└─────────────────────────────────────────\n");
                return ContentType.ListItems;
            }

            // 3. 連續文本處理 (Continuous Text Handling)
            Logger.Debug("🎯 v3預檢查：連續文本處理");
            Console.WriteLine("│ 🛤️ 通道選擇：連續文本處理 (Continuous Text Handling)");
            Console.WriteLine($"│ 📋 選擇原因：{listIndicatorCount}/{column.Count}行含列表符號 (比例:{listRatio:F2} < 0.5)，啟用雙峰驅動多指標加權決策系統");
            Console.WriteLine($"└─────────────────────────────────────────\n");
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
            Console.WriteLine("⚡ 【單行捷徑執行】跳過所有分割邏輯，直接創建段落");
            
            var paragraph = CreateParagraphV3(column, 0, columnColor);
            DebugStage3V3(columnKey, 0, column[0], 0, "單行捷徑");
            Console.WriteLine($"   📄 創建段落0：「{column[0].Text.Substring(0, Math.Min(30, column[0].Text.Length))}...」");
            
            return new List<LayoutParagraph> { paragraph };
        }

        /// <summary>
        /// v3處理：列表項目識別 (List Item Detection)
        /// </summary>
        private List<LayoutParagraph> HandleListItemDetectionV3(List<LayoutLine> column, Color columnColor, string columnKey)
        {
            Logger.Debug("📋 v3列表檢測：基於縮排和列表符號的快速分割");
            Console.WriteLine("📋 【列表項目執行】基於縮排和符號的快速分割，每行獨立段落");
            
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
                
                string indicator = IsListIndicator(column[i].Text.Trim()) ? "✓列表符號" : "✗無符號";
                DebugStage3V3(columnKey, i, column[i], i, "列表項目分割");
                Console.WriteLine($"   📄 創建段落{i}：{indicator} 「{column[i].Text.Substring(0, Math.Min(30, column[i].Text.Length))}...」");
            }
            
            Console.WriteLine($"📋 【列表分割完成】總計{paragraphs.Count}個段落 (每行1個段落)");
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

            // v3.1步驟二：雙峰驅動標準行距計算模型 (Bi-Peak Driven Standard Line Spacing)
            var biPeakModel = CalculateBiPeakDrivenLineSpacingV31(column, columnKey);
            Logger.Debug($"📏 v3.1雙峰模型：合併峰值={biPeakModel.PeakMerge:F1}px，分割峰值={biPeakModel.PeakSplit:F1}px");
            
            Console.WriteLine($"┌─ 【雙峰模型分析結果】 ─────────────────────");
            Console.WriteLine($"│ 📏 合併峰值：{biPeakModel.PeakMerge:F1}px (段落內間距標準)");
            Console.WriteLine($"│ 📏 分割峰值：{biPeakModel.PeakSplit:F1}px (段落間間距標準)");
            Console.WriteLine($"│ 📏 容差範圍：±{biPeakModel.Tolerance:F1}px");
            Console.WriteLine($"│ 🔍 合併區間：[0, {(biPeakModel.PeakMerge + biPeakModel.Tolerance):F1}]px");
            Console.WriteLine($"│ 🔍 模糊區間：({(biPeakModel.PeakMerge + biPeakModel.Tolerance):F1}, {(biPeakModel.PeakSplit - biPeakModel.Tolerance):F1})px");
            Console.WriteLine($"│ 🔍 分割區間：[{(biPeakModel.PeakSplit - biPeakModel.Tolerance):F1}, ∞)px");
            Console.WriteLine($"└─────────────────────────────────────────");

            // v3.1步驟三：雙峰驅動的多指標加權決策系統 (暫時使用舊方法，稍後更新)
            return ApplyWeightedDecisionSystemV31(column, biPeakModel, columnColor, columnKey);
        }        
        
        /// <summary>
        /// v3.1步驟二：穩健的雙峰驅動標準行距計算模型 (Robust Bi-Peak Driven Standard Line Spacing - 已廢棄)
        /// 解決多峰分佈問題，識別 peak_merge 和 peak_split，定義三個決策區間
        /// </summary>
        /// <remarks>
        /// ⚠️ 此複雜的統計模型已被AI替代
        /// 不再需要: 雙峰識別、聚類分析、變異係數計算
        /// AI可直接理解文本語義進行段落劃分
        /// </remarks>
        [Obsolete("複雜的雙峰統計模型已廢棄,改用AI語義理解", false)]
        private BiPeakSpacingModel CalculateBiPeakDrivenLineSpacingV31(List<LayoutLine> column, string columnKey = "未知欄位")
        {
            Logger.Debug($"    🔬 [v3.1 雙峰驅動模型] 開始分析欄位 '{columnKey}' (共{column.Count}行)");
            
            if (column.Count < 2) 
            {
                Logger.Debug($"    ⚠️  行數不足，啟用回退機制");
                var fallbackModel = new BiPeakSpacingModel
                {
                    PeakMerge = 20.0,
                    PeakSplit = 36.0,  // 20.0 * 1.8
                    Tolerance = 4.0,   // 20.0 * 0.2
                    MergeZone = new Range { Min = 0, Max = 24.0 },
                    SplitZone = new Range { Min = 32.0, Max = double.MaxValue },
                    AmbiguityZone = new Range { Min = 24.0, Max = 32.0 },
                    TotalSpacings = 0
                };
                DebugBiPeakModel(columnKey, fallbackModel, new List<double>());
                return fallbackModel;
            }

            // === 步驟 2.1：多峰識別與關鍵角色分配 ===
            
            // 1. 收集與排序所有相鄰行間距
            Logger.Debug($"    📊 步驟2.1-1：收集相鄰行間距");
            var spacings = new List<double>();
            for (int i = 1; i < column.Count; i++)
            {
                var prevLine = column[i - 1];
                var currentLine = column[i];
                // v3.1改進：保留負間距（重疊），讓雙峰模型感知重疊特徵
                double spacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
                spacings.Add(spacing);
                Logger.Debug($"      行{i-1}→行{i}: 間距={spacing:F1}px{(spacing < 0 ? "(重疊)" : "")}");
            }
            spacings.Sort();
            Logger.Debug($"    📈 排序後間距: [{string.Join(", ", spacings.Select(s => s.ToString("F1")))}]");

            // 2. 基於密度的一維聚類 (v4.1 自適應閾值改進)
            double avgFontHeight = column.Average(line => line.BoundingBox.Height);
            
            // v4.1: 記錄舊版閾值用於比較
            double oldThreshold = avgFontHeight * 0.25;
            
            // v4.1: 使用混合自適應策略
            string selectedMethod;
            double clusterThreshold = DetermineClusterThreshold(spacings, avgFontHeight, out selectedMethod);
            
            // v4.1: 調試輸出和比較 - 使用方法返回的選擇信息
            LogV41AdaptiveThresholdSelection(columnKey, spacings, avgFontHeight, clusterThreshold, selectedMethod);
            LogV41ThresholdComparison(columnKey, oldThreshold, clusterThreshold, 
                "提升不同文檔密度下的聚類穩定性，減少固定閾值敏感性問題");
            
            Logger.Debug($"    🎯 步驟2.1-2：聚類分析 (平均字體高度={avgFontHeight:F1}px, v4.1聚類閾值={clusterThreshold:F1}px)");

            var clusters = PerformOneDimensionalClustering(spacings, clusterThreshold);
            Logger.Debug($"    📦 發現{clusters.Count}個聚類:");
            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];
                Logger.Debug($"      簇{i+1}: 成員數={cluster.Count}, 範圍=[{cluster.Min():F1}-{cluster.Max():F1}], 均值={cluster.Average():F1}");
            }
            
            // 3. 關鍵峰值定義 (文檔要求：過濾成員數 ≤ 1 的噪音簇)
            Logger.Debug($"    🏔️  步驟2.1-3：關鍵峰值定義");
            Console.WriteLine($"🔍 【噪音簇檢查】檢查{clusters.Count}個聚類是否有噪音簇 (成員數≤1)");
            
            // 識別並記錄噪音簇 (用戶要求：在調試輸出中記錄被過濾掉的噪音簇)
            var filteredClusters = new List<int>();
            for (int i = 0; i < clusters.Count; i++)
            {
                if (clusters[i].Count <= 1)
                {
                    filteredClusters.Add(i);
                }
            }
            
            if (filteredClusters.Any())
            {
                Logger.Debug($"    🗑️  過濾噪音簇 (成員數≤1):");
                Console.WriteLine($"🗑️  發現並過濾行距噪音簇：");
                for (int i = 0; i < filteredClusters.Count; i++)
                {
                    int clusterIndex = filteredClusters[i];
                    var noise = clusters[clusterIndex];
                    string logMessage = $"      第{clusterIndex+1}個簇: 成員數={noise.Count}, 值={noise.Average():F1}px{(noise.Count == 1 ? " (單點噪音)" : "")}";
                    Logger.Debug(logMessage);
                    Console.WriteLine($"    {logMessage}");
                }
                Console.WriteLine($"🗑️  總計過濾了{filteredClusters.Count}個噪音簇 (單點間距值，不具統計意義)");
            }
            else
            {
                Logger.Debug($"    ✨ 無噪音簇需要過濾");
                Console.WriteLine($"✨ 檢查完成：所有{clusters.Count}個聚類都是有效的 (成員數>1)，無噪音簇需要過濾");
            }
            
            var validPeaks = clusters
                .Where(cluster => cluster.Count > 1) // 過濾成員數 ≤ 1 的噪音簇（文檔規格修正）
                .Select(cluster => cluster.Average()) // 計算簇的均值
                .OrderBy(peak => peak)
                .ToList();
            Logger.Debug($"    ✅ 有效峰值數量: {validPeaks.Count}, 值: [{string.Join(", ", validPeaks.Select(p => p.ToString("F1")))}]");

            
            // 4. 回退機制
            double peakMerge, peakSplit;
            
            if (validPeaks.Count == 0)
            {
                // 回退機制：找不到有效峰值（文檔規格）
                Logger.Debug($"    🔄 回退機制：找不到有效峰值，使用平均字體高度");
                peakMerge = avgFontHeight;
                peakSplit = peakMerge * 1.8;
                Logger.Debug($"    📐 回退值: peak_merge={peakMerge:F1}, peak_split={peakSplit:F1}");
            }
            else if (validPeaks.Count == 1)
            {
                // 回退機制：只有一個有效峰值（文檔規格）
                Logger.Debug($"    🔄 回退機制：只有一個有效峰值");
                peakMerge = validPeaks[0];
                peakSplit = peakMerge * 1.8;
                Logger.Debug($"    📐 單峰值: peak_merge={peakMerge:F1}, peak_split={peakSplit:F1} (經驗係數1.8)");
            }
            else
            {
                // 正常情況：定義關鍵峰值（文檔規格）
                Logger.Debug($"    🎯 正常情況：多峰值模式");
                peakMerge = validPeaks.First();  // 最小峰值 = 強烈合併信號
                peakSplit = validPeaks.Last();   // 最大峰值 = 強烈分割信號
                Logger.Debug($"    🔗 peak_merge={peakMerge:F1} (最小峰值，段落內部標準行距)");
                Logger.Debug($"    ✂️  peak_split={peakSplit:F1} (最大峰值，段落之間間距)");
            }

            // === 步驟 2.2：定義決策區間 ===
            Logger.Debug($"    🎪 步驟2.2：定義決策區間");
            double tolerance = peakMerge * 0.2;
            Logger.Debug($"    📏 容差範圍: {tolerance:F1}px (peak_merge * 0.2)");
            
            // 三個決策區間（文檔規格）
            var mergeZoneMax = peakMerge + tolerance;
            var splitZoneMin = peakSplit - tolerance;
            
            Logger.Debug($"    🟢 合併區間: [0, {mergeZoneMax:F1}] - 段落內部模式");
            Logger.Debug($"    🔴 分割區間: [{splitZoneMin:F1}, ∞) - 段落之間模式");
            Logger.Debug($"    🟡 模糊區間: ({mergeZoneMax:F1}, {splitZoneMin:F1}) - 交由其他指標決策");
            
            var model = new BiPeakSpacingModel
            {
                PeakMerge = peakMerge,
                PeakSplit = peakSplit,
                Tolerance = tolerance,
                MergeZone = new Range { Min = 0, Max = mergeZoneMax },
                SplitZone = new Range { Min = splitZoneMin, Max = double.MaxValue },
                AmbiguityZone = new Range { Min = mergeZoneMax, Max = splitZoneMin },
                TotalSpacings = spacings.Count,
                ValidPeaks = validPeaks.Count,
                ClusterThreshold = clusterThreshold
            };

            DebugBiPeakModel(columnKey, model, spacings);
            Logger.Debug($"    ✅ 雙峰驅動模型v3.1建立完成");
            return model;
        }

        /// <summary>
        /// 基於密度的一維聚類算法
        /// </summary>
        private List<List<double>> PerformOneDimensionalClustering(List<double> sortedSpacings, double threshold)
        {
            var clusters = new List<List<double>>();
            if (sortedSpacings.Count == 0) return clusters;

            var currentCluster = new List<double> { sortedSpacings[0] };
            
            for (int i = 1; i < sortedSpacings.Count; i++)
            {
                double diff = sortedSpacings[i] - sortedSpacings[i - 1];
                
                if (diff <= threshold)
                {
                    // 屬於同一簇
                    currentCluster.Add(sortedSpacings[i]);
                }
                else
                {
                    // 創建新簇
                    clusters.Add(currentCluster);
                    currentCluster = new List<double> { sortedSpacings[i] };
                }
            }
            
            // 添加最後一個簇
            clusters.Add(currentCluster);
            return clusters;
        }

        /// <summary>
        /// 雙峰間距模型數據結構
        /// </summary>
        public class BiPeakSpacingModel
        {
            public double PeakMerge { get; set; }     // 合併峰值
            public double PeakSplit { get; set; }     // 分割峰值
            public double Tolerance { get; set; }     // 容差
            public Range MergeZone { get; set; }      // 合併區間
            public Range SplitZone { get; set; }      // 分割區間
            public Range AmbiguityZone { get; set; }  // 模糊區間
            public int TotalSpacings { get; set; }    // 總間距數量
            public int ValidPeaks { get; set; }       // 有效峰值數量
            public double ClusterThreshold { get; set; } // 聚類閾值
        }

        public class Range
        {
            public double Min { get; set; }
            public double Max { get; set; }
            
            public bool Contains(double value)
            {
                return value >= Min && value <= Max;
            }
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
                
                // v3.1改進：計算真實行距（包含負值重疊）
                double actualSpacing = currentLine.BoundingBox.Top - previousLine.BoundingBox.Bottom;
                
                DebugStage3WeightedScore(i, relativeDistanceScore, fontHeightPenalty, alignmentPenalty, overlapBonus, mergeScore, adaptiveThreshold, mergeScore > adaptiveThreshold, actualSpacing, standardLineSpacing);
                
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
        /// v3.1步驟三：雙峰驅動的多指標加權決策系統 (Bi-Peak Driven Multi-Indicator Weighted System - 已廢棄)
        /// 基於雙峰模型計算合併分數，支持三區間決策邏輯
        /// </summary>
        /// <remarks>
        /// ⚠️ 此加權決策系統已被AI替代
        /// 不再需要: 自適應閾值、合併分數計算、多指標權重
        /// AI通過語義理解自動完成段落劃分
        /// </remarks>
        [Obsolete("加權決策系統已廢棄,改用AI智能決策", false)]
        private List<LayoutParagraph> ApplyWeightedDecisionSystemV31(List<LayoutLine> column, BiPeakSpacingModel biPeakModel, Color columnColor, string columnKey)
        {
            // v3.1新特性：基於雙峰模型的自適應閾值系統
            double adaptiveThreshold = CalculateAdaptiveThresholdV31(column, biPeakModel, columnKey);
            
            var paragraphs = new List<LayoutParagraph>();
            var currentParagraph = new List<LayoutLine> { column[0] };
            
            Logger.Debug($"🧮 v4.2雙峰加權系統：自適應閾值={adaptiveThreshold:F2}");
            Console.WriteLine($"🎯 v4.2最終合併閾值確認：{adaptiveThreshold:F2} (智能相機系統全面啟動)");
            DebugStage3V3(columnKey, 0, column[0], 0, "段落起始");

            for (int i = 1; i < column.Count; i++)
            {
                var currentLine = column[i];
                var previousLine = column[i - 1];
                
                Console.WriteLine($"\n┌─ 【行{i}分析】 ─────────────────────────────");
                
                // 使用v3.1雙峰驅動合併分數計算
                double mergeScore = CalculateMergeScoreV31(previousLine, currentLine, biPeakModel);
                
                Logger.Debug($"📊 v3.1分數：行{i} 「{currentLine.Text.Substring(0, Math.Min(20, currentLine.Text.Length))}...」 → {mergeScore:F2}");
                Console.WriteLine($"│ 📊 最終合併分數：{mergeScore:F2} (v4.2最終合併閾值:{adaptiveThreshold:F2})");
                
                if (mergeScore > adaptiveThreshold)
                {
                    // 合併到當前段落
                    currentParagraph.Add(currentLine);
                    DebugStage3V3(columnKey, i, currentLine, paragraphs.Count, $"合併-分數{mergeScore:F2}");
                    Console.WriteLine($"│ 🔗 決策結果：行{i} 合併到段落{paragraphs.Count} (分數{mergeScore:F2} > {adaptiveThreshold:F2})");
                    Console.WriteLine($"└─────────────────────────────────────────\n");
                }
                else
                {
                    // 創建新段落
                    Console.WriteLine($"│ ✂️ 決策結果：行{i} 分割 (分數{mergeScore:F2} ≤ {adaptiveThreshold:F2})，創建段落{paragraphs.Count + 1}");
                    Console.WriteLine($"└─────────────────────────────────────────\n");
                    
                    if (currentParagraph.Count > 0)
                    {
                        paragraphs.Add(CreateParagraphV3(currentParagraph, paragraphs.Count, columnColor));
                        Logger.Debug($"✂️ v3.1分割：分數{mergeScore:F2} ≤ {adaptiveThreshold:F2}，創建段落{paragraphs.Count}");
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

            Logger.Debug($"🎯 v3.1決策完成：{paragraphs.Count} 個段落");
            Console.WriteLine($"🎯 v3.1雙峰段落分析完成：{paragraphs.Count} 個段落");
            return paragraphs;
        }

        /// <summary>
        /// v4.2 智能自適應閾值系統重構 - 基於統計分佈的動態閾值計算
        /// 實現文檔中方案4.1-4.5的完整改進方案，解決v3.1的核心問題
        /// </summary>
        private double CalculateAdaptiveThresholdV31(List<LayoutLine> column, BiPeakSpacingModel biPeakModel, string columnKey)
        {
            try
            {
                Logger.Debug($"    🎯 v4.2智能自適應閾值系統啟動：基於新改進方案 4.1-4.5");
                
                // 收集間距數據用於統計分析
                var spacings = new List<double>();
                for (int i = 1; i < column.Count; i++)
                {
                    double spacing = column[i].BoundingBox.Top - column[i - 1].BoundingBox.Bottom;
                    spacings.Add(spacing);
                }

                // 方案4.1：基於統計分佈的動態基礎閾值
                double baseThreshold = CalculateStatisticalBaseThreshold(spacings, biPeakModel.PeakMerge);
                Logger.Debug($"      📸 方案4.1 - 智能相機感光度：動態基礎閾值={baseThreshold:F2}");

                // 方案4.2：基於變異係數的分離度調整
                double separationFactor = CalculateVariationCoefficientSeparation(spacings, biPeakModel);
                Logger.Debug($"      🎛️  方案4.2 - 智能相機景深控制：分離度調整={separationFactor:F2}");

                // 方案4.3：連續型可信度評分系統
                double confidenceFactor = CalculateContinuousConfidenceScore(spacings, biPeakModel);
                Logger.Debug($"      📊 方案4.3 - 智能相機測光權重：可信度調整={confidenceFactor:F2}");

                // 計算初步閾值
                double preliminaryThreshold = baseThreshold + separationFactor + confidenceFactor;
                
                // 方案4.4：基於文檔類型的動態約束範圍
                double adaptiveThreshold = ApplyDocumentTypeConstraints(preliminaryThreshold, column, spacings);
                Logger.Debug($"      ⚡ 方案4.4 - 智能相機快門限制：約束後閾值={adaptiveThreshold:F2}");

                // 方案4.5：多層驗證和異常處理機制
                adaptiveThreshold = ValidateAndHandleAnomalies(adaptiveThreshold, spacings, biPeakModel);
                Logger.Debug($"      🛡️  方案4.5 - 智能相機安全監控：最終閾值={adaptiveThreshold:F2}");

                Logger.Debug($"    🧮 v4.2完整計算：基礎{baseThreshold:F2} + 分離{separationFactor:F2} + 可信{confidenceFactor:F2} + 約束調整 = {adaptiveThreshold:F2}");
                Console.WriteLine($"🧮 v4.2智能自適應閾值：{adaptiveThreshold:F2} (統計驅動基礎{baseThreshold:F2} + 智能調整{(separationFactor + confidenceFactor):F2})");
                Console.WriteLine($"📊 v4.2最終合併閾值：{adaptiveThreshold:F2} - 智能相機系統五階段優化完成");
                
                return adaptiveThreshold;
            }
            catch (Exception ex)
            {
                Logger.Warn($"    ⚠️  v4.2智能閾值計算異常，回退到v3.1機制：{ex.Message}");
                // 回退到原v3.1實現
                return CalculateAdaptiveThresholdV31_Legacy(column, biPeakModel, columnKey);
            }
        }

        /// <summary>
        /// 方案4.1：基於統計分佈的動態基礎閾值
        /// 類比：智能相機自動感光度系統
        /// </summary>
        private double CalculateStatisticalBaseThreshold(List<double> spacings, double peakMerge)
        {
            if (spacings.Count < 2)
            {
                return Math.Max(0.5, peakMerge * 1.2);
            }

            // 基於間距分佈的百分位數計算
            double p25 = GetPercentile(spacings, 0.25);  // 第25百分位數
            double p75 = GetPercentile(spacings, 0.75);  // 第75百分位數
            double iqr = p75 - p25;  // 四分位距

            double baseThreshold = Math.Max(0.5, Math.Min(peakMerge + iqr * 0.5, peakMerge * 2.0));
            
            Logger.Debug($"        📊 統計分析：P25={p25:F2}, P75={p75:F2}, IQR={iqr:F2} → 基礎閾值={baseThreshold:F2}");
            return baseThreshold;
        }

        /// <summary>
        /// 方案4.2：基於變異係數的分離度調整
        /// 類比：智能相機景深控制系統
        /// </summary>
        private double CalculateVariationCoefficientSeparation(List<double> spacings, BiPeakSpacingModel biPeakModel)
        {
            if (spacings.Count < 2)
            {
                return 0.0;
            }

            // 避免除零，使用變異係數標準化
            double cv = CalculateCoefficientOfVariation(spacings);
            double peakSeparation = biPeakModel.PeakSplit - biPeakModel.PeakMerge;
            double normalizedSeparation = peakSeparation / (biPeakModel.PeakMerge + 1.0);  // +1避免除零
            double separationFactor = Math.Tanh(normalizedSeparation) * cv * 0.4;  // 使用tanh防止爆炸

            Logger.Debug($"        🎛️  景深控制：變異係數={cv:F3}, 標準化分離度={normalizedSeparation:F2} → 調整因子={separationFactor:F2}");
            return separationFactor;
        }

        /// <summary>
        /// 方案4.3：連續型可信度評分系統
        /// 類比：智能相機測光權重分配系統
        /// </summary>
        private double CalculateContinuousConfidenceScore(List<double> spacings, BiPeakSpacingModel biPeakModel)
        {
            // 基於樣本量和峰值質量的連續評分
            double sampleScore = Math.Min(1.0, (spacings.Count - 2) / 8.0);  // 樣本量評分
            double peakQuality = CalculatePeakSeparationQuality(biPeakModel);      // 峰值質量評分
            double confidenceScore = (sampleScore + peakQuality) / 2.0;     // 綜合可信度
            double confidenceFactor = (confidenceScore - 0.5) * 0.6;  // 映射到[-0.3, +0.3]

            Logger.Debug($"        📊 測光權重：樣本評分={sampleScore:F2}, 峰值質量={peakQuality:F2}, 綜合可信度={confidenceScore:F2} → 調整因子={confidenceFactor:F2}");
            return confidenceFactor;
        }

        /// <summary>
        /// 方案4.4：基於文檔類型的動態約束範圍
        /// 類比：智能相機快門速度限制系統
        /// </summary>
        private double ApplyDocumentTypeConstraints(double preliminaryThreshold, List<LayoutLine> column, List<double> spacings)
        {
            // 根據平均字體大小和間距密度動態調整約束範圍
            double avgFontSize = CalculateAverageLineHeight(column);
            double spacingDensity = CalculateSpacingDensity(spacings);
            double minThreshold = Math.Max(0.2, avgFontSize * 0.05);
            double maxThreshold = Math.Min(10.0, avgFontSize * 0.8);
            double adaptiveThreshold = Math.Max(minThreshold, Math.Min(preliminaryThreshold, maxThreshold));

            Logger.Debug($"        ⚡ 快門限制：平均字體={avgFontSize:F1}, 密度={spacingDensity:F2}, 約束範圍=[{minThreshold:F2}, {maxThreshold:F2}] → 調整後={adaptiveThreshold:F2}");
            return adaptiveThreshold;
        }

        /// <summary>
        /// 方案4.5：多層驗證和異常處理機制
        /// 類比：智能相機拍攝安全監控系統
        /// </summary>
        private double ValidateAndHandleAnomalies(double adaptiveThreshold, List<double> spacings, BiPeakSpacingModel biPeakModel)
        {
            // 計算結果合理性驗證
            if (spacings.Count > 0 && adaptiveThreshold > spacings.Max() * 1.5)
            {
                // 閾值異常過大，回退到保守策略
                double fallbackThreshold = spacings.Average() * 1.2;
                Logger.Warn($"        🛡️  安全監控：閾值異常過大({adaptiveThreshold:F2} > {spacings.Max() * 1.5:F2})，回退到保守策略={fallbackThreshold:F2}");
                return fallbackThreshold;
            }

            // 檢查是否有級聯誤差的跡象 - 只在極端異常時才介入
            if (adaptiveThreshold < 0.1 || adaptiveThreshold > 15.0)
            {
                double conservativeThreshold = Math.Max(0.5, Math.Min(adaptiveThreshold, 10.0));
                Logger.Warn($"        🛡️  安全監控：檢測到極端異常值，應用安全約束={conservativeThreshold:F2}");
                return conservativeThreshold;
            }

            Logger.Debug($"        ✅ 安全監控：閾值通過所有驗證檢查");
            return adaptiveThreshold;
        }

        /// <summary>
        /// v3.1原版實現（回退用）
        /// </summary>
        private double CalculateAdaptiveThresholdV31_Legacy(List<LayoutLine> column, BiPeakSpacingModel biPeakModel, string columnKey)
        {
            // v3.1改進：基於合併峰值動態調整基礎閾值，避免小間距文檔閾值過高
            double baseThreshold = Math.Min(2.0, Math.Max(1.0, biPeakModel.PeakMerge * 1.2));
            
            Logger.Debug($"    🎯 v3.1自適應閾值計算：基於雙峰模型 (動態基礎閾值={baseThreshold:F2})");
            
            // 1. 雙峰分離度調整 - 雙峰差距越大，需要更保守的合併策略
            double peakSeparation = biPeakModel.PeakSplit - biPeakModel.PeakMerge;
            double separationFactor = Math.Min(peakSeparation / biPeakModel.PeakMerge, 1.0) * 0.3; // 降低影響權重
            Logger.Debug($"      📏 峰值分離度：{peakSeparation:F1}px → 調整因子{separationFactor:F2}");
            
            // 2. 峰值可信度調整 - 基於有效峰值數量和間距數量
            double confidenceFactor = 0.0;
            if (biPeakModel.ValidPeaks >= 2 && biPeakModel.TotalSpacings >= 3)
            {
                // 雙峰模型可信度高，可以更激進的合併
                confidenceFactor = -0.3;
                Logger.Debug($"      ✅ 高可信度：有效峰值{biPeakModel.ValidPeaks}個，間距{biPeakModel.TotalSpacings}個 → 調整{confidenceFactor:F1}");
            }
            else
            {
                // 雙峰模型可信度低，採用保守策略
                confidenceFactor = 0.2;
                Logger.Debug($"      ⚠️  低可信度：有效峰值{biPeakModel.ValidPeaks}個，間距{biPeakModel.TotalSpacings}個 → 調整{confidenceFactor:F1}");
            }
            
            // 計算最終閾值 (移除了內容調整分：字體一致性和行數密度)
            double adaptiveThreshold = baseThreshold + separationFactor + confidenceFactor;
            adaptiveThreshold = Math.Max(0.8, Math.Min(adaptiveThreshold, 3.5)); // 降低最低閾值，適應緊密間距文檔
            
            Logger.Debug($"    🧮 v3.1閾值計算：基礎{baseThreshold:F2} + 分離{separationFactor:F2} + 可信{confidenceFactor:F1} = {adaptiveThreshold:F2}");
            Console.WriteLine($"🧮 v3.1雙峰自適應閾值：{adaptiveThreshold:F2} (動態基礎{baseThreshold:F2} + 雙峰調整{(separationFactor + confidenceFactor):F2})");
            
            return adaptiveThreshold;
        }

        #region v4.2智能自適應閾值系統輔助方法

        /// <summary>
        /// 計算數列的指定百分位數
        /// </summary>
        private double GetPercentile(List<double> values, double percentile)
        {
            if (values == null || values.Count == 0)
                return 0.0;

            var sortedValues = values.OrderBy(x => x).ToList();
            if (sortedValues.Count == 1)
                return sortedValues[0];

            double index = percentile * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);

            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];

            double weight = index - lowerIndex;
            return sortedValues[lowerIndex] * (1 - weight) + sortedValues[upperIndex] * weight;
        }

        /// <summary>
        /// 計算變異係數 (標準差/平均值)
        /// </summary>
        private double CalculateCoefficientOfVariation(List<double> values)
        {
            if (values == null || values.Count < 2)
                return 0.0;

            double mean = values.Average();
            if (Math.Abs(mean) < 1e-10) // 避免除零
                return 0.0;

            double variance = values.Sum(x => Math.Pow(x - mean, 2)) / values.Count;
            double standardDeviation = Math.Sqrt(variance);
            
            return standardDeviation / Math.Abs(mean);
        }

        /// <summary>
        /// 計算峰值分離質量評分
        /// </summary>
        private double CalculatePeakSeparationQuality(BiPeakSpacingModel biPeakModel)
        {
            if (biPeakModel.ValidPeaks < 2)
                return 0.0;

            // 基於峰值分離度和峰值數量的質量評分
            double separation = biPeakModel.PeakSplit - biPeakModel.PeakMerge;
            double relativeSeparation = separation / (biPeakModel.PeakMerge + 1.0); // 避免除零
            
            // 分離度質量：使用 tanh 函數將相對分離度映射到 [0, 1]
            double separationQuality = Math.Tanh(relativeSeparation / 2.0);
            
            // 峰值數量質量：有效峰值越多，質量越高，但有上限
            double peakCountQuality = Math.Min(1.0, biPeakModel.ValidPeaks / 3.0);
            
            // 綜合質量評分
            return (separationQuality + peakCountQuality) / 2.0;
        }

        /// <summary>
        /// 計算欄位平均行高
        /// </summary>
        private double CalculateAverageLineHeight(List<LayoutLine> column)
        {
            if (column == null || column.Count == 0)
                return 12.0; // 默認值

            return column.Average(line => line.LineHeight);
        }

        /// <summary>
        /// 計算間距密度指標
        /// </summary>
        private double CalculateSpacingDensity(List<double> spacings)
        {
            if (spacings == null || spacings.Count < 2)
                return 1.0; // 默認密度

            // 計算間距的變異係數作為密度指標
            double cv = CalculateCoefficientOfVariation(spacings);
            
            // 低變異係數表示高密度（間距相似），高變異係數表示低密度（間距差異大）
            // 使用反向映射：cv 越小，密度越高
            return Math.Max(0.1, 2.0 / (1.0 + cv)); // 映射到 [0.1, 2.0] 範圍
        }

        #endregion

        /// <summary>
        /// v3新特性：內容特徵自適應閾值計算系統
        /// 根據欄位的內容特徵動態調整合併閾值
        /// (移除了內容調整分：字體一致性和行數密度)
        /// </summary>
        private double CalculateAdaptiveThresholdV3(List<LayoutLine> column, double standardLineSpacing, string columnKey)
        {
            const double BASE_THRESHOLD = 2.0; // 基礎閾值
            
            // 1. 標準行距調整 (主要調整因子)
            double spacingFactor = standardLineSpacing > column.Average(line => line.LineHeight) ? 0.2 : -0.1; // 行距大時更保守合併
            
            // 計算最終閾值 (移除了內容調整分：字體一致性和行數密度)
            double adaptiveThreshold = BASE_THRESHOLD + spacingFactor;
            adaptiveThreshold = Math.Max(1.5, Math.Min(adaptiveThreshold, 4.0)); // 限制在合理範圍內
            
            Logger.Debug($"📊 v3自適應閾值計算：基礎{BASE_THRESHOLD} + 行距{spacingFactor:F1} = {adaptiveThreshold:F2}");
            Console.WriteLine($"📊 v3自適應閾值詳細：基礎{BASE_THRESHOLD} + 行距{spacingFactor:F1} = {adaptiveThreshold:F2}");
            
            return adaptiveThreshold;
        }

        /// <summary>
        /// v3.4 雙峰驅動距離得分計算
        /// 基於步驟二識別的 peak_merge 和 peak_split，使用簡化的評分邏輯
        /// v3.4 改進：固定分數 + 線性遞減，邏輯清晰直觀
        /// </summary>
        private double CalculateBiPeakDistanceScoreV31(LayoutLine prevLine, LayoutLine currentLine, BiPeakSpacingModel biPeakModel)
        {
            // v3.4改進：計算真實間距（保留負值重疊信息）
            double currentSpacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
            
            // 獲取雙峰模型參數
            double peakMerge = biPeakModel.PeakMerge;
            double peakSplit = biPeakModel.PeakSplit;
            double tolerance = biPeakModel.Tolerance;
            
            Logger.Debug($"      🎯 雙峰距離分析：間距={currentSpacing:F1}, peak_merge={peakMerge:F1}, peak_split={peakSplit:F1}, 容差={tolerance:F1}");
            Console.WriteLine($"│ ⓶ 雙峰分析：合併峰={peakMerge:F1}px, 分割峰={peakSplit:F1}px, 容差={tolerance:F1}px");
            
            // 1. 強分割信號：落入分割區間
            if (currentSpacing >= peakSplit - tolerance)
            {
                Logger.Debug($"      🔴 分割區間：{currentSpacing:F1} ≥ {(peakSplit - tolerance):F1} → -10.0分（強制分割）");
                Console.WriteLine($"│    🔴 分割區間：間距{currentSpacing:F1}px ≥ 分割閾值{(peakSplit - tolerance):F1}px → 強制分割(-10.0分)");
                return -10.0;  // 固定否決分，強制分割
            }
            
            // 2. 強合併信號：落入合併區間（包含負值重疊）
            if (currentSpacing <= peakMerge + tolerance)
            {
                string spacingType = currentSpacing < 0 ? "(重疊)" : currentSpacing == 0 ? "(完美貼合)" : "";
                Logger.Debug($"      🟢 合併區間：{currentSpacing:F1}{spacingType} ≤ {(peakMerge + tolerance):F1} → 4.5分（強合併信號）");
                Console.WriteLine($"│    🟢 合併區間：間距{currentSpacing:F1}px{spacingType} ≤ 合併閾值{(peakMerge + tolerance):F1}px → 強合併信號(4.5分)");
                return 4.5;  // 固定高分，強烈合併信號
            }
            
            // 3. 模糊區間：線性遞減評分
            // 範圍：(peak_merge + tolerance, peak_split - tolerance)
            else
            {
                double mergeBoundary = peakMerge + tolerance;
                double splitBoundary = peakSplit - tolerance;
                double zoneWidth = splitBoundary - mergeBoundary;
                
                // 防止除零錯誤
                if (zoneWidth <= 0)
                {
                    Logger.Debug($"      🟠 重疊區間：{currentSpacing:F1}px，區間寬度={zoneWidth:F1} → 2.0分（默認分數）");
                    Console.WriteLine($"│    🟠 重疊區間：間距{currentSpacing:F1}px，區間重疊 → 默認分數(2.0分)");
                    return 2.0; // 當兩個區間重疊時的默認分數
                }
                
                // 線性遞減：從合併邊界的4.0分遞減到分割邊界的0.5分
                double distanceFromMerge = currentSpacing - mergeBoundary;
                double normalizedPosition = distanceFromMerge / zoneWidth; // [0, 1]
                double score = 4.0 - (normalizedPosition * 3.5); // 4.0 → 0.5 線性遞減
                
                Logger.Debug($"      🟡 模糊區間線性遞減：{currentSpacing:F1}px，位置比例{normalizedPosition:F2}，線性遞減 → {score:F2}分");
                Console.WriteLine($"│    🟡 模糊區間：間距{currentSpacing:F1}px在[{mergeBoundary:F1}, {splitBoundary:F1}]，位置{normalizedPosition:F2} → 線性遞減({score:F2}分)");
                return score;
            }
        }

        /// <summary>
        /// v3.1雙峰驅動合併分數計算模型 (文檔規格實現)
        /// 合併分數 = 雙峰驅動距離得分 - 字體高度懲罰 - 對齊風格懲罰 + 重疊獎勵得分
        /// </summary>
        private double CalculateMergeScoreV31(LayoutLine prevLine, LayoutLine currentLine, BiPeakSpacingModel biPeakModel)
        {
            // 從基礎分開始計算
            double mergeScore = 0.0;
            
            // 計算實際間距用於調試
            double actualSpacing = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
            string spacingInfo = actualSpacing < 0 ? $"間距={actualSpacing:F1}px(重疊)" : $"間距={actualSpacing:F1}px";
            
            Console.WriteLine($"│ ⓵ 實際間距：{spacingInfo}");
            
            // 1. 雙峰驅動距離得分 (Bi-Peak Driven Distance Score) - 主要得分項
            double biPeakDistanceScore = CalculateBiPeakDistanceScoreV31(prevLine, currentLine, biPeakModel);
            mergeScore += biPeakDistanceScore;
            
            // 2. 字體高度懲罰 (Font Height Penalty) - 漸進式容錯模型
            double fontHeightPenalty = CalculateFontHeightPenaltyV3(prevLine, currentLine);
            mergeScore -= fontHeightPenalty;
            Console.WriteLine($"│ ⓷ 字體懲罰：-{fontHeightPenalty:F2} (高度差異)");
            
            // 3. 對齊風格懲罰 (Alignment Style Penalty)
            double alignmentStylePenalty = CalculateAlignmentStylePenaltyV3(prevLine, currentLine);
            mergeScore -= alignmentStylePenalty;
            Console.WriteLine($"│ ⓸ 對齊懲罰：-{alignmentStylePenalty:F2} (風格差異)");
            
            // 4. 重疊獎勵得分 (Overlap Bonus Score)
            double overlapBonus = CalculateOverlapBonusV3(prevLine, currentLine);
            mergeScore += overlapBonus;
            Console.WriteLine($"│ ⓹ 重疊獎勵：+{overlapBonus:F2} (重疊加分)");
            
            Logger.Debug($"   🧮 v3.1雙峰分數明細：雙峰距離{biPeakDistanceScore:F2} - 字體懲罰{fontHeightPenalty:F2} - 對齊懲罰{alignmentStylePenalty:F2} + 重疊獎勵{overlapBonus:F2} = {mergeScore:F2}");
            Console.WriteLine($"│ ⓺ 分數計算：{biPeakDistanceScore:F2} - {fontHeightPenalty:F2} - {alignmentStylePenalty:F2} + {overlapBonus:F2} = {mergeScore:F2}");
            
            return mergeScore;
        }

        /// <summary>
        /// v3合併分數計算模型 (加減分混合公式)
        /// 合併分數 = 雙峰驅動距離得分 - 字體高度懲罰 - 對齊風格懲罰 + 重疊獎勵得分
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
        /// 2. 字體高度懲罰 (Font Height Penalty) - v3.1漸進式容錯模型
        /// 採用漸進式懲罰機制，考慮 OCR 框架誤差與真實字體差異
        /// </summary>
        private double CalculateFontHeightPenaltyV3(LayoutLine prevLine, LayoutLine currentLine)
        {
            double height1 = prevLine.LineHeight;
            double height2 = currentLine.LineHeight;
            
            // 計算相對高度差異百分比
            double heightDiff = Math.Abs(height1 - height2) / Math.Min(height1, height2) * 100;
            
            if (heightDiff <= 10.0) // ≤10% - OCR 微小誤差容錯範圍
            {
                Logger.Debug($"      📏 字體高度懲罰v3.1：差異{heightDiff:F1}% ≤ 10% → 0.0懲罰 (OCR容錯)");
                return 0.0;
            }
            else if (heightDiff <= 25.0) // 10-25% - 輕微差異，可能是同字體的 OCR 變異
            {
                Logger.Debug($"      📏 字體高度懲罰v3.1：差異{heightDiff:F1}% ≤ 25% → 0.1懲罰 (同字體變異)");
                return 0.1;
            }
            else if (heightDiff <= 50.0) // 25-50% - 中等差異，可能是相鄰字體級別
            {
                Logger.Debug($"      📏 字體高度懲罰v3.1：差異{heightDiff:F1}% ≤ 50% → 0.4懲罰 (相鄰級別)");
                return 0.4;
            }
            else if (heightDiff <= 80.0) // 50-80% - 顯著差異，不同字體級別
            {
                Logger.Debug($"      📏 字體高度懲罰v3.1：差異{heightDiff:F1}% ≤ 80% → 0.8懲罰 (不同級別)");
                return 0.8;
            }
            else if (heightDiff <= 120.0) // 80-120% - 大幅差異，標題與正文級別
            {
                Logger.Debug($"      📏 字體高度懲罰v3.1：差異{heightDiff:F1}% ≤ 120% → 1.2懲罰 (標題級別)");
                return 1.2;
            }
            else // >120% - 極大差異，強制分割級別
            {
                Logger.Debug($"      📏 字體高度懲罰v3.1：差異{heightDiff:F1}% > 120% → 2.0懲罰 (極端差異)");
                return 2.0;
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
        /// 垂直重疊或完美貼合: 2.0分, 其他: 0分
        /// v3.1改進: 0.0px間距也視為極強合併信號
        /// </summary>
        private double CalculateOverlapBonusV3(LayoutLine prevLine, LayoutLine currentLine)
        {
            double verticalDistance = currentLine.BoundingBox.Top - prevLine.BoundingBox.Bottom;
            
            if (verticalDistance <= 0) // 發生重疊或完美貼合
            {
                string type = verticalDistance < 0 ? "重疊" : "完美貼合";
                Logger.Debug($"      📏 重疊獎勵：垂直距離{verticalDistance}px ≤ 0 → 2.0分({type})");
                return 2.0;
            }
            else
            {
                Logger.Debug($"      📏 重疊獎勵：垂直距離{verticalDistance}px > 0 → 0分");
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

        #region v4.1 自適應聚類閾值方法

        /// <summary>
        /// v4.1 階段一：自然間隙分析法 (Gap-Based Analysis)
        /// 分析相鄰間距差值序列，尋找最大"自然斷點"作為閾值
        /// 類比：如同在山脈中尋找最明顯的峽谷來劃分山峰
        /// </summary>
        /// <param name="sortedSpacings">已排序的間距列表</param>
        /// <returns>自然間隙閾值，若無明顯間隙則返回 double.NaN</returns>
        private double CalculateGapBasedThreshold(List<double> sortedSpacings)
        {
            if (EnableDebugMode)
            {
                Console.WriteLine($"🔬 v4.1自然間隙分析開始：分析{sortedSpacings.Count}個間距樣本");
            }
            
            if (sortedSpacings.Count < 3) 
            {
                if (EnableDebugMode)
                {
                    Console.WriteLine($"⚠️ 樣本不足({sortedSpacings.Count}<3)，跳過自然間隙分析");
                }
                return double.NaN;
            }
            
            // 計算相鄰間距的差值序列
            var gaps = new List<double>();
            for (int i = 1; i < sortedSpacings.Count; i++)
            {
                gaps.Add(sortedSpacings[i] - sortedSpacings[i - 1]);
            }
            
            // 尋找最大間隙作為"自然斷點"
            double maxGap = gaps.Max();
            double avgGap = gaps.Average();
            
            if (EnableDebugMode)
            {
                Console.WriteLine($"    📏 間隙統計：最大間隙={maxGap:F2}px，平均間隙={avgGap:F2}px");
                Console.WriteLine($"    📊 顯著性係數：{maxGap/avgGap:F2} (需>=2.0才算顯著)");
            }
            
            // 驗證最大間隙是否"顯著"（是平均間隙的2倍以上）
            if (maxGap >= avgGap * 2.0)
            {
                double threshold = maxGap * 0.6; // 使用最大間隙的60%作為閾值
                Logger.Debug($"    🎯 自然間隙分析：最大間隙={maxGap:F2}, 平均間隙={avgGap:F2}, 顯著性={maxGap/avgGap:F2}, 閾值={threshold:F2}");
                
                if (EnableDebugMode)
                {
                    Console.WriteLine($"    ✅ 發現顯著自然間隙！閾值={threshold:F2}px (最大間隙60%)");
                }
                
                return threshold;
            }
            
            Logger.Debug($"    ⚠️ 自然間隙分析：最大間隙={maxGap:F2}, 平均間隙={avgGap:F2}, 顯著性={maxGap/avgGap:F2} < 2.0 (不明顯)");
            
            if (EnableDebugMode)
            {
                Console.WriteLine($"    ❌ 無顯著自然間隙，轉入變異係數校驗");
            }
            
            return double.NaN; // 自然間隙不明顯，需要階段二補償
        }

        /// <summary>
        /// v4.1 階段二：變異係數校驗法 (Coefficient of Variation Method)
        /// 當自然間隙不明顯時，使用統計離散度指標進行補償校驗
        /// 類比：如同氣象學家在雲層密佈時改用氣壓變化來劃分天氣系統
        /// </summary>
        /// <param name="sortedSpacings">已排序的間距列表</param>
        /// <param name="avgFontHeight">平均字體高度</param>
        /// <returns>基於變異係數的自適應閾值</returns>
        private double CalculateAdaptiveThreshold(List<double> sortedSpacings, double avgFontHeight)
        {
            if (EnableDebugMode)
            {
                Console.WriteLine($"🧮 v4.1變異係數校驗啟動：分析間距離散程度");
            }
            
            if (sortedSpacings.Count < 2) 
            {
                if (EnableDebugMode)
                {
                    Console.WriteLine($"⚠️ 樣本不足，使用預設閾值");
                }
                return avgFontHeight * 0.25;
            }
            
            double mean = sortedSpacings.Average();
            double variance = sortedSpacings.Select(x => Math.Pow(x - mean, 2)).Average();
            double stdDev = Math.Sqrt(variance);
            double coefficientOfVariation = stdDev / mean;
            
            if (EnableDebugMode)
            {
                Console.WriteLine($"    📊 統計參數：平均={mean:F2}px，標準差={stdDev:F2}px");
                Console.WriteLine($"    📈 變異係數：{coefficientOfVariation:F3} (離散程度指標)");
            }
            
            // 根據數據離散程度動態調整閾值倍數
            double adaptiveFactor;
            string disperseLevel;
            if (coefficientOfVariation > 0.8)        // 高離散度：使用較大閾值
            {
                adaptiveFactor = 0.35;
                disperseLevel = "高離散(密度變化大)";
            }
            else if (coefficientOfVariation > 0.4)   // 中等離散度：標準閾值
            {
                adaptiveFactor = 0.25;
                disperseLevel = "中等離散(標準密度)";
            }
            else                                     // 低離散度：使用較小閾值
            {
                adaptiveFactor = 0.15;
                disperseLevel = "低離散(密度均勻)";
            }
            
            double threshold = avgFontHeight * adaptiveFactor;
            Logger.Debug($"    📊 變異係數校驗：CV={coefficientOfVariation:F3}, 自適應倍數={adaptiveFactor:F2}, 閾值={threshold:F2}");
            
            if (EnableDebugMode)
            {
                Console.WriteLine($"    🎯 離散等級：{disperseLevel}");
                Console.WriteLine($"    ⚖️ 自適應閾值：{threshold:F2}px (倍數={adaptiveFactor:F2})");
            }
            
            return threshold;
        }

        /// <summary>
        /// v4.1 混合策略整合邏輯
        /// 整合兩階段自適應閾值策略並提供回退機制
        /// 優先嘗試自然間隙分析，失敗時回退到變異係數方法
        /// </summary>
        /// <param name="sortedSpacings">已排序的間距列表</param>
        /// <param name="avgFontHeight">平均字體高度</param>
        /// <param name="selectedMethod">輸出所選擇的方法名稱</param>
        /// <returns>最終確定的聚類閾值</returns>
        private double DetermineClusterThreshold(List<double> sortedSpacings, double avgFontHeight, out string selectedMethod)
        {
            if (EnableDebugMode)
            {
                Console.WriteLine($"🎯 v4.1混合策略整合：智能閾值選擇開始");
            }
            
            // 優先嘗試自然間隙分析
            double gapBasedThreshold = CalculateGapBasedThreshold(sortedSpacings);
            if (!double.IsNaN(gapBasedThreshold))
            {
                selectedMethod = "自然間隙分析法";
                Logger.Debug($"🎯 v4.1 使用自然間隙分析閾值: {gapBasedThreshold:F2}px");
                
                if (EnableDebugMode)
                {
                    Console.WriteLine($"✅ 階段一成功：使用自然間隙分析結果");
                    Console.WriteLine($"🎯 最終選擇：自然間隙閾值 = {gapBasedThreshold:F2}px");
                }
                
                return gapBasedThreshold;
            }
            
            // 回退到變異係數方法
            selectedMethod = "變異係數校驗法";
            if (EnableDebugMode)
            {
                Console.WriteLine($"🔄 回退至階段二：變異係數校驗法");
            }
            
            double adaptiveThreshold = CalculateAdaptiveThreshold(sortedSpacings, avgFontHeight);
            Logger.Debug($"📊 v4.1 使用變異係數自適應閾值: {adaptiveThreshold:F2}px");
            
            if (EnableDebugMode)
            {
                Console.WriteLine($"✅ 階段二完成：使用變異係數自適應結果");
                Console.WriteLine($"🎯 最終選擇：統計分析閾值 = {adaptiveThreshold:F2}px");
            }
            
            return adaptiveThreshold;
        }

        #endregion

    }

    /// <summary>
    /// 版面分析結果
    /// </summary>
    public partial class LayoutAnalysisResult
    {
        public bool Success { get; set; }
        public Dictionary<string, List<LayoutParagraph>> Layout { get; set; }
        public double ProcessingTimeMs { get; set; }
        
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
    public partial class LayoutParagraph
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
        /// 行標識符
        /// </summary>
        public string LineId { get; set; }
        
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
