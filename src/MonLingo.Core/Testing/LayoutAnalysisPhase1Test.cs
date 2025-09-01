using System;
using System.Drawing;
using System.Linq;
using MonLingo.Core.Service;

namespace MonLingo.Core.Testing
{
    /// <summary>
    /// 版面分析階段一測試程式
    /// 測試橫向行合併功能
    /// </summary>
    class LayoutAnalysisPhase1Test
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🧪 版面分析階段一測試 - 橫向行合併");
            Console.WriteLine("========================================");

            // 模擬OCR結果：同一行被錯誤拆分的情況
            var testOcrResult = CreateTestOcrResult();
            
            // 執行版面分析
            var layoutService = new LayoutAnalysisService();
            var result = layoutService.AnalyzeLayout(testOcrResult);
            
            // 顯示結果
            DisplayResults(testOcrResult, result);
            
            Console.WriteLine("\n按任意鍵退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 創建測試用的OCR結果
        /// 模擬同一行文字被錯誤分割的情況
        /// </summary>
        private static OcrResult CreateTestOcrResult()
        {
            var ocrLines = new OcrLine[]
            {
                // 第一行：被拆分成三段的英文句子
                new OcrLine
                {
                    Text = "Hello",
                    Confidence = 0.95,
                    BoundingBox = new Rectangle(10, 10, 60, 25), // Y=10-35
                    Words = new OcrWord[] { new OcrWord { Text = "Hello", Confidence = 0.95, BoundingBox = new Rectangle(10, 10, 60, 25) } }
                },
                new OcrLine
                {
                    Text = "world",
                    Confidence = 0.92,
                    BoundingBox = new Rectangle(80, 12, 70, 22), // Y=12-34，有重疊
                    Words = new OcrWord[] { new OcrWord { Text = "world", Confidence = 0.92, BoundingBox = new Rectangle(80, 12, 70, 22) } }
                },
                new OcrLine
                {
                    Text = "today!",
                    Confidence = 0.88,
                    BoundingBox = new Rectangle(160, 11, 80, 24), // Y=11-35，有重疊
                    Words = new OcrWord[] { new OcrWord { Text = "today!", Confidence = 0.88, BoundingBox = new Rectangle(160, 11, 80, 24) } }
                },
                
                // 第二行：獨立的中文句子（不應該與第一行合併）
                new OcrLine
                {
                    Text = "這是測試文字",
                    Confidence = 0.90,
                    BoundingBox = new Rectangle(10, 60, 150, 30), // Y=60-90，距離較遠
                    Words = new OcrWord[] { new OcrWord { Text = "這是測試文字", Confidence = 0.90, BoundingBox = new Rectangle(10, 60, 150, 30) } }
                },
                
                // 第三行：另一組被拆分的英文（應該合併）
                new OcrLine
                {
                    Text = "How",
                    Confidence = 0.94,
                    BoundingBox = new Rectangle(20, 110, 50, 28), // Y=110-138
                    Words = new OcrWord[] { new OcrWord { Text = "How", Confidence = 0.94, BoundingBox = new Rectangle(20, 110, 50, 28) } }
                },
                new OcrLine
                {
                    Text = "are",
                    Confidence = 0.96,
                    BoundingBox = new Rectangle(80, 112, 40, 26), // Y=112-138，有重疊
                    Words = new OcrWord[] { new OcrWord { Text = "are", Confidence = 0.96, BoundingBox = new Rectangle(80, 112, 40, 26) } }
                },
                new OcrLine
                {
                    Text = "you?",
                    Confidence = 0.91,
                    BoundingBox = new Rectangle(130, 111, 50, 27), // Y=111-138，有重疊
                    Words = new OcrWord[] { new OcrWord { Text = "you?", Confidence = 0.91, BoundingBox = new Rectangle(130, 111, 50, 27) } }
                },
                
                // 第四行：間距過大，不應該合併
                new OcrLine
                {
                    Text = "Far",
                    Confidence = 0.89,
                    BoundingBox = new Rectangle(10, 160, 40, 25), // Y=160-185
                    Words = new OcrWord[] { new OcrWord { Text = "Far", Confidence = 0.89, BoundingBox = new Rectangle(10, 160, 40, 25) } }
                },
                new OcrLine
                {
                    Text = "away",
                    Confidence = 0.87,
                    BoundingBox = new Rectangle(200, 162, 60, 23), // Y=162-185，間距過大（200-50=150px）
                    Words = new OcrWord[] { new OcrWord { Text = "away", Confidence = 0.87, BoundingBox = new Rectangle(200, 162, 60, 23) } }
                }
            };

            return new OcrResult
            {
                Text = string.Join(" ", ocrLines.Select(line => line.Text)),
                Confidence = ocrLines.Average(line => line.Confidence),
                BoundingBox = new Rectangle(10, 10, 250, 175),
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
