using System;
using System.Collections.Generic;
using System.Drawing;
using MonLingo.Core.Service;
using MonLingo.Core.Service.DualChannel;

namespace MonLingo.Core.Tests.DebugDemo
{
    /// <summary>
    /// 雙通道架構 v4.0 終端調試演示
    /// 展示完整的調試輸出效果
    /// </summary>
    public class DualChannelDebugDemo
    {
        /// <summary>
        /// 執行雙通道調試演示
        /// </summary>
        public static void RunDebugDemo()
        {
            Console.WriteLine("=== MonLingo v4.0 雙通道架構調試演示 ===");
            Console.WriteLine();

            // 創建調試模式的版面分析服務
            var layoutService = new LayoutAnalysisService
            {
                EnableDebugMode = true,  // 啟用調試模式
                EnableDualChannel = true  // 啟用雙通道架構
            };

            // 創建模擬的OCR結果
            var mockOcrResult = CreateMockOcrResult();

            Console.WriteLine("🚀 開始 v4.0 雙通道版面分析...");
            Console.WriteLine();

            try
            {
                // 執行版面分析 - 這將產生豐富的調試輸出
                var result = layoutService.AnalyzeLayoutV4(mockOcrResult);

                Console.WriteLine();
                Console.WriteLine("📊 === 分析結果摘要 ===");
                Console.WriteLine($"✅ 分析成功: {result.Success}");
                Console.WriteLine($"⏱️ 處理時間: {result.ProcessingTimeMs:F2}ms");
                Console.WriteLine($"📄 版本: {result.Version}");
                Console.WriteLine($"📂 欄位數量: {result.Layout?.Count ?? 0}");
                
                if (result.Layout != null)
                {
                    int totalParagraphs = 0;
                    foreach (var column in result.Layout)
                    {
                        Console.WriteLine($"   📋 {column.Key}: {column.Value.Count}個段落");
                        totalParagraphs += column.Value.Count;
                    }
                    Console.WriteLine($"📑 總段落數: {totalParagraphs}");
                }

                // 獲取並顯示雙通道統計信息
                Console.WriteLine();
                Console.WriteLine("📈 === 雙通道處理統計 ===");
                var stats = layoutService.GetDualChannelStatistics();
                if (stats != null)
                {
                    Console.WriteLine(stats.FormatSummary());
                }
                else
                {
                    Console.WriteLine("📊 無統計信息可用");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 分析過程發生錯誤: {ex.Message}");
                Console.WriteLine($"🔍 錯誤詳情: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("=== 演示完成 ===");
        }

        /// <summary>
        /// 創建模擬的OCR結果，包含多種類型的內容
        /// </summary>
        private static OcrResult CreateMockOcrResult()
        {
            return new OcrResult
            {
                Lines = new LayoutLine[]
                {
                    // 標題行 - 大字體
                    new LayoutLine 
                    { 
                        Text = "MonLingo v4.0 雙通道架構", 
                        BoundingBox = new Rectangle(50, 20, 400, 30), 
                        LineHeight = 30,
                        Confidence = 0.95
                    },

                    // 正文段落1 - 標準間距
                    new LayoutLine 
                    { 
                        Text = "雙通道決策架構是一個革命性的版面分析技術，", 
                        BoundingBox = new Rectangle(50, 70, 450, 20), 
                        LineHeight = 20,
                        Confidence = 0.92
                    },
                    new LayoutLine 
                    { 
                        Text = "能夠根據數據品質智能選擇最適合的分析通道。", 
                        BoundingBox = new Rectangle(50, 95, 420, 20), 
                        LineHeight = 20,
                        Confidence = 0.91
                    },

                    // 段落間距較大 - 新段落
                    new LayoutLine 
                    { 
                        Text = "當統計樣本充足時，系統將使用雙峰統計通道，", 
                        BoundingBox = new Rectangle(50, 140, 440, 20), 
                        LineHeight = 20,
                        Confidence = 0.93
                    },
                    new LayoutLine 
                    { 
                        Text = "利用強大的統計分析能力進行精確的段落分割。", 
                        BoundingBox = new Rectangle(50, 165, 430, 20), 
                        LineHeight = 20,
                        Confidence = 0.90
                    },

                    // 列表項目 - 會被經驗規則通道識別
                    new LayoutLine 
                    { 
                        Text = "1. 智能通道選擇 - 根據數據品質自動決策", 
                        BoundingBox = new Rectangle(70, 210, 400, 18), 
                        LineHeight = 18,
                        Confidence = 0.89
                    },
                    new LayoutLine 
                    { 
                        Text = "2. 雙峰統計通道 - 處理高品質統計數據", 
                        BoundingBox = new Rectangle(70, 233, 390, 18), 
                        LineHeight = 18,
                        Confidence = 0.88
                    },
                    new LayoutLine 
                    { 
                        Text = "3. 經驗規則通道 - 處理低品質或結構化數據", 
                        BoundingBox = new Rectangle(70, 256, 410, 18), 
                        LineHeight = 18,
                        Confidence = 0.87
                    },

                    // 子標題 - 中等字體
                    new LayoutLine 
                    { 
                        Text = "系統特性", 
                        BoundingBox = new Rectangle(50, 300, 150, 24), 
                        LineHeight = 24,
                        Confidence = 0.94
                    },

                    // 最後一段
                    new LayoutLine 
                    { 
                        Text = "v4.0架構具備完整的向後兼容性，", 
                        BoundingBox = new Rectangle(50, 340, 320, 20), 
                        LineHeight = 20,
                        Confidence = 0.91
                    },
                    new LayoutLine 
                    { 
                        Text = "可以無縫整合到現有的MonLingo系統中。", 
                        BoundingBox = new Rectangle(50, 365, 360, 20), 
                        LineHeight = 20,
                        Confidence = 0.90
                    }
                }
            };
        }
    }

    /// <summary>
    /// 程序入口點 - 用於獨立運行調試演示
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                DualChannelDebugDemo.RunDebugDemo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序執行錯誤: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意鍵退出...");
            Console.ReadKey();
        }
    }
}