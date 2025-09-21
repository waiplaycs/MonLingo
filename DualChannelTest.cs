using System;
using System.Collections.Generic;
using System.Drawing;
using MonLingo.Core.Service;
using MonLingo.Core.Service.DualChannel;

namespace MonLingo.Debug
{
    /// <summary>
    /// 專門測試雙通道調試輸出的控制台程序
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("🎯 MonLingo 雙通道架構 v4.0 調試演示");
            Console.WriteLine("=======================================");
            Console.WriteLine();

            try
            {
                // 創建雙通道控制器
                var dualChannelController = new DualChannelController();
                
                // 創建模擬輸入
                var testLines = CreateTestLayoutLines();
                
                Console.WriteLine($"📥 輸入數據: {testLines.Count} 行文字");
                Console.WriteLine();
                
                // 執行雙通道分析
                var result = dualChannelController.ProcessLayoutLines(testLines);
                
                Console.WriteLine();
                Console.WriteLine($"📊 處理結果: {result?.Count ?? 0} 個段落");
                Console.WriteLine();
                
                // 顯示統計信息
                var stats = dualChannelController.GetStatistics();
                DisplayStatistics(stats);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 錯誤: {ex.Message}");
                Console.WriteLine($"📍 堆棧: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("🏁 按任意鍵退出...");
            Console.ReadKey();
        }
        
        static List<LayoutAnalysisService.LayoutLine> CreateTestLayoutLines()
        {
            return new List<LayoutAnalysisService.LayoutLine>
            {
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是標題行，字體較大",
                    Confidence = 0.95,
                    BoundingBox = new Rectangle(10, 20, 400, 24),
                    LineHeight = 24,
                    OriginalIndex = 0,
                    LineId = "LINE_001"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是第一段的第一行內容",
                    Confidence = 0.92,
                    BoundingBox = new Rectangle(10, 60, 350, 18),
                    LineHeight = 18,
                    OriginalIndex = 1,
                    LineId = "LINE_002"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是第一段的第二行內容",
                    Confidence = 0.90,
                    BoundingBox = new Rectangle(10, 78, 320, 18),
                    LineHeight = 18,
                    OriginalIndex = 2,
                    LineId = "LINE_003"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是第二段的開始，有較大間距",
                    Confidence = 0.88,
                    BoundingBox = new Rectangle(10, 120, 380, 18),
                    LineHeight = 18,
                    OriginalIndex = 3,
                    LineId = "LINE_004"
                },
                new LayoutAnalysisService.LayoutLine
                {
                    Text = "這是第二段的第二行",
                    Confidence = 0.85,
                    BoundingBox = new Rectangle(10, 138, 300, 18),
                    LineHeight = 18,
                    OriginalIndex = 4,
                    LineId = "LINE_005"
                }
            };
        }
        
        static void DisplayStatistics(dynamic stats)
        {
            if (stats == null)
            {
                Console.WriteLine("📈 無統計數據可用");
                return;
            }
            
            Console.WriteLine("📈 雙通道統計信息:");
            Console.WriteLine("================");
            
            try
            {
                // 嘗試顯示基本統計
                Console.WriteLine($"  • 總處理時間: 未知");
                Console.WriteLine($"  • 通道選擇次數: 未知");
                Console.WriteLine($"  • 統計通道使用: 未知");
                Console.WriteLine($"  • 經驗通道使用: 未知");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  統計顯示錯誤: {ex.Message}");
            }
        }
    }
}