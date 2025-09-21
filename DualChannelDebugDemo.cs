using System;
using System.Collections.Generic;
using System.Drawing;
using MonLingo.Core.Service;
using MonLingo.Core.Service.DualChannel;

namespace MonLingo.Core
{
    /// <summary>
    /// 雙通道調試演示程序 - 展示豐富的終端調試輸出
    /// </summary>
    public class DualChannelDebugDemo
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("🔧 MonLingo 雙通道架構 v4.0 調試演示");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            try
            {
                // 創建布局分析服務實例
                var layoutService = new LayoutAnalysisService();
                
                // 創建模擬的OCR行數據
                var mockOcrLines = CreateMockOcrData();
                
                Console.WriteLine("📋 開始雙通道版面分析演示...");
                Console.WriteLine();
                
                // 觸發雙通道分析處理
                var result = layoutService.AnalyzeLayoutDualChannel(mockOcrLines);
                
                Console.WriteLine();
                Console.WriteLine("✅ 雙通道分析完成!");
                Console.WriteLine($"📊 結果統計: 總段落數={result?.Count ?? 0}");
                
                if (result != null && result.Count > 0)
                {
                    foreach (var paragraph in result)
                    {
                        Console.WriteLine($"   📄 段落: {paragraph.ParagraphId} (通道: {paragraph.CreatedByChannel})");
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("🏁 演示結束. 按任意鍵關閉...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 錯誤: {ex.Message}");
                Console.WriteLine("🏁 演示結束. 按任意鍵關閉...");
                Console.ReadKey();
            }
        }
        
        /// <summary>
        /// 創建模擬OCR數據用於演示
        /// </summary>
        private static List<LayoutAnalysisService.LayoutLine> CreateMockOcrData()
        {
            return new List<LayoutAnalysisService.LayoutLine>
            {
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是第一行文字，用於測試雙通道分析",
                    Confidence = 0.95,
                    BoundingBox = new Rectangle(10, 50, 300, 20),
                    LineHeight = 20,
                    OriginalIndex = 0,
                    LineId = "LINE_001"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是第二行文字，間距正常",
                    Confidence = 0.92,
                    BoundingBox = new Rectangle(10, 75, 280, 20),
                    LineHeight = 20,
                    OriginalIndex = 1,
                    LineId = "LINE_002"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是第三行文字，將觸發統計分析",
                    Confidence = 0.88,
                    BoundingBox = new Rectangle(10, 100, 320, 20),
                    LineHeight = 20,
                    OriginalIndex = 2,
                    LineId = "LINE_003"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "異常間距行 - 測試經驗規則通道",
                    Confidence = 0.75,
                    BoundingBox = new Rectangle(10, 150, 250, 18),
                    LineHeight = 18,
                    OriginalIndex = 3,
                    LineId = "LINE_004"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "最後一行文字，完成演示",
                    Confidence = 0.90,
                    BoundingBox = new Rectangle(10, 175, 200, 20),
                    LineHeight = 20,
                    OriginalIndex = 4,
                    LineId = "LINE_005"
                }
            };
        }
    }
}