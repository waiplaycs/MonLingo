using System;
using System.Drawing;
using System.Linq;
using MonLingo.Core.Service;

namespace LayoutAnalysisTest
{
    /// <summary>
    /// 版面分析階段一測試程式
    /// 測試橫向行合併功能
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🧪 版面分析階段三測試 - 段落分割（含調試功能）");
            Console.WriteLine("========================================");

            // 模擬OCR結果：多欄位佈局情況
            var testOcrResult = CreateTestOcrResult();
            
            // 執行完整的三階段版面分析（啟用調試模式）
            var layoutService = new LayoutAnalysisService();
            layoutService.EnableDebugMode = true; // 啟用調試模式
            
            var result = layoutService.AnalyzeLayout(testOcrResult);
            
            // 顯示結果
            DisplayResults(testOcrResult, result);
            
            Console.WriteLine("\n按任意鍵退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 創建測試用的OCR結果
        /// 模擬多欄位佈局的情況
        /// </summary>
        private static OcrResult CreateTestOcrResult()
        {
            var ocrLines = new OcrLine[]
            {
                // 左欄：新聞標題和內容
                new OcrLine
                {
                    Text = "今日新聞",
                    Confidence = 0.95,
                    BoundingBox = new Rectangle(10, 10, 120, 30), // 左欄標題
                    Words = new OcrWord[] { new OcrWord { Text = "今日新聞", Confidence = 0.95, BoundingBox = new Rectangle(10, 10, 120, 30) } }
                },
                new OcrLine
                {
                    Text = "據報導，人工智能技術",
                    Confidence = 0.92,
                    BoundingBox = new Rectangle(10, 50, 140, 25), // 左欄內容行1
                    Words = new OcrWord[] { new OcrWord { Text = "據報導，人工智能技術", Confidence = 0.92, BoundingBox = new Rectangle(10, 50, 140, 25) } }
                },
                new OcrLine
                {
                    Text = "正在快速發展中。",
                    Confidence = 0.89,
                    BoundingBox = new Rectangle(10, 80, 130, 25), // 左欄內容行2
                    Words = new OcrWord[] { new OcrWord { Text = "正在快速發展中。", Confidence = 0.89, BoundingBox = new Rectangle(10, 80, 130, 25) } }
                },

                // 右欄：廣告內容
                new OcrLine
                {
                    Text = "特價優惠",
                    Confidence = 0.94,
                    BoundingBox = new Rectangle(200, 15, 100, 28), // 右欄標題
                    Words = new OcrWord[] { new OcrWord { Text = "特價優惠", Confidence = 0.94, BoundingBox = new Rectangle(200, 15, 100, 28) } }
                },
                new OcrLine
                {
                    Text = "限時搶購，機會難得！",
                    Confidence = 0.91,
                    BoundingBox = new Rectangle(200, 55, 150, 25), // 右欄內容行1
                    Words = new OcrWord[] { new OcrWord { Text = "限時搶購，機會難得！", Confidence = 0.91, BoundingBox = new Rectangle(200, 55, 150, 25) } }
                },
                new OcrLine
                {
                    Text = "立即行動吧。",
                    Confidence = 0.88,
                    BoundingBox = new Rectangle(200, 85, 110, 25), // 右欄內容行2
                    Words = new OcrWord[] { new OcrWord { Text = "立即行動吧。", Confidence = 0.88, BoundingBox = new Rectangle(200, 85, 110, 25) } }
                },

                // 底部：單獨的頁腳信息（應該形成第三欄）
                new OcrLine
                {
                    Text = "版權所有 © 2025",
                    Confidence = 0.86,
                    BoundingBox = new Rectangle(50, 150, 140, 20), // 頁腳
                    Words = new OcrWord[] { new OcrWord { Text = "版權所有 © 2025", Confidence = 0.86, BoundingBox = new Rectangle(50, 150, 140, 20) } }
                },

                // 測試相鄰但不應合併的情況：間距過大的兩行
                new OcrLine
                {
                    Text = "Left",
                    Confidence = 0.90,
                    BoundingBox = new Rectangle(10, 200, 40, 20), 
                    Words = new OcrWord[] { new OcrWord { Text = "Left", Confidence = 0.90, BoundingBox = new Rectangle(10, 200, 40, 20) } }
                },
                new OcrLine
                {
                    Text = "Right",
                    Confidence = 0.87,
                    BoundingBox = new Rectangle(300, 205, 50, 20), // 水平間距很大，應該分為不同欄位
                    Words = new OcrWord[] { new OcrWord { Text = "Right", Confidence = 0.87, BoundingBox = new Rectangle(300, 205, 50, 20) } }
                }
            };

            return new OcrResult
            {
                Text = string.Join(" ", ocrLines.Select(line => line.Text)),
                Confidence = ocrLines.Average(line => line.Confidence),
                BoundingBox = new Rectangle(10, 10, 340, 215),
                Lines = ocrLines
            };
        }

        /// <summary>
        /// 顯示測試結果
        /// </summary>
        private static void DisplayResults(OcrResult originalOcr, LayoutAnalysisResult result)
        {
            Console.WriteLine("📋 原始OCR結果：");
            Console.WriteLine($"   總行數：{originalOcr.Lines.Length}");
            for (int i = 0; i < originalOcr.Lines.Length; i++)
            {
                var line = originalOcr.Lines[i];
                Console.WriteLine($"   行{i + 1}：「{line.Text}」位置({line.BoundingBox.X},{line.BoundingBox.Y},{line.BoundingBox.Width},{line.BoundingBox.Height})");
            }

            Console.WriteLine("\n🎯 版面分析結果：");
            if (result.Success)
            {
                Console.WriteLine($"   處理時間：{result.ProcessingTimeMs:F1}ms");
                Console.WriteLine($"   欄位數量：{result.Layout.Count}");
                
                foreach (var column in result.Layout)
                {
                    Console.WriteLine($"\n   📂 {column.Key}：");
                    for (int i = 0; i < column.Value.Count; i++)
                    {
                        var paragraph = column.Value[i];
                        Console.WriteLine($"      段落{i + 1}({paragraph.ParagraphId})：{paragraph.Lines.Count} 行");
                        foreach (var line in paragraph.Lines)
                        {
                            Console.WriteLine($"         💬 「{line.Text}」信心度:{line.Confidence:F2}");
                        }
                        
                        // 顯示段落顏色信息
                        Console.WriteLine($"         🎨 欄位顏色: {paragraph.ColumnColor.Name} (R:{paragraph.ColumnColor.R}, G:{paragraph.ColumnColor.G}, B:{paragraph.ColumnColor.B})");
                    }
                }

                // 顯示調試信息
                if (result.DebugInfo != null)
                {
                    Console.WriteLine($"\n🔍 調試信息：");
                    Console.WriteLine($"   欄位顏色映射：");
                    foreach (var colorMapping in result.DebugInfo.ColumnColors)
                    {
                        var color = colorMapping.Value;
                        Console.WriteLine($"     {colorMapping.Key}: {color.Name} (R:{color.R}, G:{color.G}, B:{color.B})");
                    }
                    
                    Console.WriteLine($"   段落邊界框信息：");
                    foreach (var paragraphBound in result.DebugInfo.ParagraphBounds)
                    {
                        Console.WriteLine($"     {paragraphBound.ColumnKey}-{paragraphBound.ParagraphId}: 大框({paragraphBound.BoundingBox.X},{paragraphBound.BoundingBox.Y},{paragraphBound.BoundingBox.Width},{paragraphBound.BoundingBox.Height})");
                        Console.WriteLine($"       包含 {paragraphBound.OriginalLineBounds.Count} 個原始識別框");
                    }
                }

                // 統計合併效果
                int originalLineCount = originalOcr.Lines.Length;
                int mergedLineCount = result.Layout.Values.SelectMany(column => column).Sum(para => para.Lines.Count);
                int mergedCount = originalLineCount - mergedLineCount;
                
                Console.WriteLine($"\n📊 合併效果統計：");
                Console.WriteLine($"   原始行數：{originalLineCount}");
                Console.WriteLine($"   合併後行數：{mergedLineCount}");
                Console.WriteLine($"   合併了 {mergedCount} 個文字碎片");
                Console.WriteLine($"   合併效率：{(double)mergedCount / originalLineCount * 100:F1}%");
            }
            else
            {
                Console.WriteLine($"   ❌ 失敗：{result.ErrorMessage}");
            }
        }
    }
}
