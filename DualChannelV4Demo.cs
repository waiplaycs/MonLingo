using System;
using System.Collections.Generic;
using System.Drawing;
using MonLingo.Core.Service;
using MonLingo.Core.Service.DualChannel;

namespace MonLingo.Debug
{
    /// <summary>
    /// MonLingo 雙通道架構 v4.0 調試演示
    /// 展示詳細的terminal調試輸出
    /// </summary>
    class DualChannelV4Demo
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Console.WriteLine("🎯 MonLingo 雙通道架構 v4.0 Terminal調試演示");
            Console.WriteLine("================================================");
            Console.WriteLine();
            
            try
            {
                // 創建雙通道控制器配置 (啟用調試模式)
                var config = new DualChannelController.Config
                {
                    EnableDebugLog = true
                };
                
                // 創建雙通道控制器
                var controller = new DualChannelController(config);
                
                Console.WriteLine("📋 測試案例1: 統計樣本充足的文檔 (雙峰統計通道)");
                Console.WriteLine("─────────────────────────────────────────");
                var column1 = CreateStatisticallyRichColumn();
                var result1 = controller.ProcessColumn(column1);
                
                Console.WriteLine();
                Console.WriteLine("📋 測試案例2: 統計樣本不足的文檔 (經驗規則通道)");
                Console.WriteLine("─────────────────────────────────────────");
                var column2 = CreateStatisticallyPoorColumn();
                var result2 = controller.ProcessColumn(column2);
                
                Console.WriteLine();
                Console.WriteLine("📋 測試案例3: 峰值不明顯的文檔 (經驗規則通道)");
                Console.WriteLine("─────────────────────────────────────────");
                var column3 = CreateLowPeakColumn();
                var result3 = controller.ProcessColumn(column3);
                
                Console.WriteLine();
                Console.WriteLine("🎊 所有測試案例完成!");
                Console.WriteLine($"📊 案例1結果: {result1?.Count ?? 0}段落 (統計通道)");
                Console.WriteLine($"📊 案例2結果: {result2?.Count ?? 0}段落 (經驗通道)");
                Console.WriteLine($"📊 案例3結果: {result3?.Count ?? 0}段落 (經驗通道)");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 錯誤: {ex.Message}");
                Console.WriteLine($"📍 詳細信息: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("🏁 演示結束. 按任意鍵退出...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// 創建統計樣本充足的欄位 (觸發雙峰統計通道)
        /// </summary>
        static Column CreateStatisticallyRichColumn()
        {
            var lines = new List<LayoutAnalysisService.LayoutLine>();
            
            // 創建10行文字，有明顯的雙峰間距模式
            for (int i = 0; i < 10; i++)
            {
                int yPos = 50 + i * (i % 3 == 2 ? 35 : 20); // 製造雙峰間距模式
                lines.Add(new LayoutAnalysisService.LayoutLine
                {
                    Text = $"這是第{i + 1}行文字，用於測試雙峰統計算法",
                    Confidence = 0.9 + (i % 2) * 0.05,
                    BoundingBox = new Rectangle(10, yPos, 350 + i * 10, 18),
                    LineHeight = 18,
                    OriginalIndex = i,
                    LineId = $"RICH_LINE_{i + 1:D3}"
                });
            }
            
            return new Column
            {
                ColumnId = "STATISTICAL_RICH_COL",
                Lines = lines,
                BoundingBox = new Rectangle(10, 50, 400, 300),
                Color = Color.Blue
            };
        }
        
        /// <summary>
        /// 創建統計樣本不足的欄位 (觸發經驗規則通道)
        /// </summary>
        static Column CreateStatisticallyPoorColumn()
        {
            var lines = new List<LayoutAnalysisService.LayoutLine>();
            
            // 只創建4行文字 (少於最小樣本數8)
            for (int i = 0; i < 4; i++)
            {
                lines.Add(new LayoutAnalysisService.LayoutLine
                {
                    Text = $"這是樣本不足第{i + 1}行文字",
                    Confidence = 0.85,
                    BoundingBox = new Rectangle(10, 50 + i * 25, 300, 18),
                    LineHeight = 18,
                    OriginalIndex = i,
                    LineId = $"POOR_LINE_{i + 1:D3}"
                });
            }
            
            return new Column
            {
                ColumnId = "STATISTICAL_POOR_COL",
                Lines = lines,
                BoundingBox = new Rectangle(10, 50, 350, 150),
                Color = Color.Red
            };
        }
        
        /// <summary>
        /// 創建峰值不明顯的欄位 (觸發經驗規則通道)
        /// </summary>
        static Column CreateLowPeakColumn()
        {
            var lines = new List<LayoutAnalysisService.LayoutLine>();
            
            // 創建9行文字，但間距變化很小 (峰值不明顯)
            for (int i = 0; i < 9; i++)
            {
                int spacing = 22 + (i % 3); // 間距變化很小：22, 23, 24px
                lines.Add(new LayoutAnalysisService.LayoutLine
                {
                    Text = $"這是峰值不明顯第{i + 1}行文字",
                    Confidence = 0.88,
                    BoundingBox = new Rectangle(10, 50 + i * spacing, 320, 18),
                    LineHeight = 18,
                    OriginalIndex = i,
                    LineId = $"LOWPEAK_LINE_{i + 1:D3}"
                });
            }
            
            return new Column
            {
                ColumnId = "LOW_PEAK_COL",
                Lines = lines,
                BoundingBox = new Rectangle(10, 50, 360, 250),
                Color = Color.Green
            };
        }
    }
}