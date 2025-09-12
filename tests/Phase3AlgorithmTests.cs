using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonLingo.Core.Service;

namespace MonLingo.Tests
{
    /// <summary>
    /// MonLingo v3階段三演算法測試：混合模式段落分割
    /// 驗證多指標加權決策系統和自適應閾值計算
    /// </summary>
    [TestClass]
    public class Phase3AlgorithmTests
    {
        private LayoutAnalysisService _layoutService;

        [TestInitialize]
        public void Setup()
        {
            _layoutService = new LayoutAnalysisService
            {
                EnableDebugMode = true // 啟用調試模式以查看詳細日誌
            };
        }

        /// <summary>
        /// 測試v3文檔中的範例案例：完美合併案例
        /// 距離得分 2.8 + 字體得分 1.2 + 對齊得分 0.6 = 4.6 > 2.5 → 強力合併
        /// </summary>
        [TestMethod]
        public void TestV3_PerfectMergeCase()
        {
            // 建立測試數據：兩行非常接近且字體高度相同的文字
            var testLines = new List<OcrLine>
            {
                new OcrLine { Text = "這是第一行文字內容", BoundingBox = new Rectangle(50, 100, 200, 20), Confidence = 0.95 },
                new OcrLine { Text = "這是第二行文字內容", BoundingBox = new Rectangle(50, 122, 200, 20), Confidence = 0.95 } // 距離22px，接近標準行距
            };

            // 執行版面分析
            var result = _layoutService.AnalyzeLayout(testLines);

            // 驗證結果
            Assert.IsTrue(result.Success, "版面分析應該成功");
            Assert.AreEqual(1, result.Layout.Count, "應該有1個欄位");
            
            var column = result.Layout.First().Value;
            Assert.AreEqual(1, column.Count, "應該有1個段落（兩行合併）");
            Assert.AreEqual(2, column[0].Lines.Count, "段落應該包含2行文字");
            
            Console.WriteLine($"✅ 完美合併案例測試通過：2行合併為1段落");
        }

        /// <summary>
        /// 測試v3文檔中的範例案例：典型分割案例
        /// 距離得分 0.5 + 字體得分 0 + 對齊得分 0 = 0.5 < 2.5 → 分割
        /// </summary>
        [TestMethod]
        public void TestV3_TypicalSplitCase()
        {
            // 建立測試數據：兩行距離很遠的文字
            var testLines = new List<OcrLine>
            {
                new OcrLine { Text = "這是第一段的內容", BoundingBox = new Rectangle(50, 100, 200, 20), Confidence = 0.95 },
                new OcrLine { Text = "這是第二段的內容", BoundingBox = new Rectangle(50, 180, 200, 15), Confidence = 0.95 } // 距離60px，字體高度不同
            };

            // 執行版面分析
            var result = _layoutService.AnalyzeLayout(testLines);

            // 驗證結果
            Assert.IsTrue(result.Success, "版面分析應該成功");
            Assert.AreEqual(1, result.Layout.Count, "應該有1個欄位");
            
            var column = result.Layout.First().Value;
            Assert.AreEqual(2, column.Count, "應該有2個段落（兩行分割）");
            Assert.AreEqual(1, column[0].Lines.Count, "第一個段落應該包含1行文字");
            Assert.AreEqual(1, column[1].Lines.Count, "第二個段落應該包含1行文字");
            
            Console.WriteLine($"✅ 典型分割案例測試通過：2行分割為2段落");
        }

        /// <summary>
        /// 測試v3重疊獎勵機制：重疊特殊案例
        /// 距離得分 1.0 + 重疊得分 2.0 + 字體得分 0.4 = 3.4 > 2.5 → 強制合併
        /// </summary>
        [TestMethod]
        public void TestV3_OverlapBonusCase()
        {
            // 建立測試數據：垂直重疊的兩行文字（OCR錯誤導致）
            var testLines = new List<OcrLine>
            {
                new OcrLine { Text = "重疊的第一行", BoundingBox = new Rectangle(50, 100, 150, 20), Confidence = 0.95 },
                new OcrLine { Text = "重疊的第二行", BoundingBox = new Rectangle(50, 115, 150, 18), Confidence = 0.95 } // Y=115，與第一行重疊5px
            };

            // 執行版面分析
            var result = _layoutService.AnalyzeLayout(testLines);

            // 驗證結果
            Assert.IsTrue(result.Success, "版面分析應該成功");
            
            var column = result.Layout.First().Value;
            Assert.AreEqual(1, column.Count, "重疊行應該合併為1個段落");
            Assert.AreEqual(2, column[0].Lines.Count, "段落應該包含2行重疊的文字");
            
            Console.WriteLine($"✅ 重疊獎勵案例測試通過：重疊行強制合併");
        }

        /// <summary>
        /// 測試v3自適應閾值系統：密集文本vs稀疏文本
        /// </summary>
        [TestMethod]
        public void TestV3_AdaptiveThreshold()
        {
            // 測試1：密集文本（小字體，緊密排列）
            var denseLines = new List<OcrLine>
            {
                new OcrLine { Text = "密集文本第1行", BoundingBox = new Rectangle(50, 100, 120, 12), Confidence = 0.95 },
                new OcrLine { Text = "密集文本第2行", BoundingBox = new Rectangle(50, 114, 120, 12), Confidence = 0.95 },
                new OcrLine { Text = "密集文本第3行", BoundingBox = new Rectangle(50, 128, 120, 12), Confidence = 0.95 },
                new OcrLine { Text = "密集文本第4行", BoundingBox = new Rectangle(50, 142, 120, 12), Confidence = 0.95 }
            };

            // 測試2：稀疏文本（大字體，寬鬆排列）
            var sparseLines = new List<OcrLine>
            {
                new OcrLine { Text = "稀疏文本第1行", BoundingBox = new Rectangle(50, 100, 200, 24), Confidence = 0.95 },
                new OcrLine { Text = "稀疏文本第2行", BoundingBox = new Rectangle(50, 130, 200, 24), Confidence = 0.95 },
                new OcrLine { Text = "稀疏文本第3行", BoundingBox = new Rectangle(50, 160, 200, 24), Confidence = 0.95 },
                new OcrLine { Text = "稀疏文本第4行", BoundingBox = new Rectangle(50, 190, 200, 24), Confidence = 0.95 }
            };

            // 執行分析
            var denseResult = _layoutService.AnalyzeLayout(denseLines);
            var sparseResult = _layoutService.AnalyzeLayout(sparseLines);

            // 驗證結果
            Assert.IsTrue(denseResult.Success && sparseResult.Success, "兩個測試都應該成功");
            
            var denseColumn = denseResult.Layout.First().Value;
            var sparseColumn = sparseResult.Layout.First().Value;
            
            Console.WriteLine($"📊 密集文本結果：{denseColumn.Count}個段落");
            Console.WriteLine($"📊 稀疏文本結果：{sparseColumn.Count}個段落");
            
            // 自適應閾值應該讓密集文本更容易合併（段落數更少）
            Assert.IsTrue(denseColumn.Count <= sparseColumn.Count, 
                "密集文本的段落數應該不多於稀疏文本（自適應閾值效果）");
            
            Console.WriteLine($"✅ 自適應閾值測試通過：密集文本{denseColumn.Count}段 vs 稀疏文本{sparseColumn.Count}段");
        }

        /// <summary>
        /// 測試v3列表項目檢測
        /// </summary>
        [TestMethod]
        public void TestV3_ListItemDetection()
        {
            // 建立列表項目測試數據
            var listLines = new List<OcrLine>
            {
                new OcrLine { Text = "• 第一個列表項目", BoundingBox = new Rectangle(50, 100, 150, 16), Confidence = 0.95 },
                new OcrLine { Text = "• 第二個列表項目", BoundingBox = new Rectangle(50, 120, 150, 16), Confidence = 0.95 },
                new OcrLine { Text = "• 第三個列表項目", BoundingBox = new Rectangle(50, 140, 150, 16), Confidence = 0.95 },
                new OcrLine { Text = "1. 編號列表項目", BoundingBox = new Rectangle(50, 160, 150, 16), Confidence = 0.95 },
                new OcrLine { Text = "2. 編號列表項目", BoundingBox = new Rectangle(50, 180, 150, 16), Confidence = 0.95 }
            };

            // 執行版面分析
            var result = _layoutService.AnalyzeLayout(listLines);

            // 驗證結果
            Assert.IsTrue(result.Success, "版面分析應該成功");
            
            var column = result.Layout.First().Value;
            
            // 列表檢測應該將每個項目視為獨立段落
            Assert.AreEqual(5, column.Count, "應該有5個段落（每個列表項目一個）");
            
            foreach (var paragraph in column)
            {
                Assert.AreEqual(1, paragraph.Lines.Count, "每個列表項目應該是單行段落");
            }
            
            Console.WriteLine($"✅ 列表項目檢測測試通過：5個列表項目分割為5個段落");
        }

        /// <summary>
        /// 測試v3單行捷徑優化
        /// </summary>
        [TestMethod]
        public void TestV3_SingleLineShortcut()
        {
            // 建立單行測試數據
            var singleLine = new List<OcrLine>
            {
                new OcrLine { Text = "這是唯一的一行文字", BoundingBox = new Rectangle(50, 100, 180, 20), Confidence = 0.95 }
            };

            // 執行版面分析
            var result = _layoutService.AnalyzeLayout(singleLine);

            // 驗證結果
            Assert.IsTrue(result.Success, "版面分析應該成功");
            Assert.AreEqual(1, result.Layout.Count, "應該有1個欄位");
            
            var column = result.Layout.First().Value;
            Assert.AreEqual(1, column.Count, "應該有1個段落");
            Assert.AreEqual(1, column[0].Lines.Count, "段落應該包含1行文字");
            
            Console.WriteLine($"✅ 單行捷徑測試通過：1行直接成為1個段落");
        }

        /// <summary>
        /// 壓力測試：複雜混合內容
        /// </summary>
        [TestMethod]
        public void TestV3_ComplexMixedContent()
        {
            // 建立複雜混合內容：包含標題、段落、列表等
            var complexLines = new List<OcrLine>
            {
                // 標題（大字體，居中）
                new OcrLine { Text = "文檔標題", BoundingBox = new Rectangle(100, 50, 100, 24), Confidence = 0.95 },
                
                // 第一段落（普通文字）
                new OcrLine { Text = "這是第一段的第一行文字內容", BoundingBox = new Rectangle(50, 100, 200, 16), Confidence = 0.95 },
                new OcrLine { Text = "這是第一段的第二行文字內容", BoundingBox = new Rectangle(50, 118, 200, 16), Confidence = 0.95 },
                
                // 空行間距
                
                // 第二段落（普通文字）
                new OcrLine { Text = "這是第二段的內容，與上段有較大間距", BoundingBox = new Rectangle(50, 160, 220, 16), Confidence = 0.95 },
                
                // 列表項目
                new OcrLine { Text = "• 列表項目一", BoundingBox = new Rectangle(50, 200, 120, 14), Confidence = 0.95 },
                new OcrLine { Text = "• 列表項目二", BoundingBox = new Rectangle(50, 216, 120, 14), Confidence = 0.95 },
                new OcrLine { Text = "• 列表項目三", BoundingBox = new Rectangle(50, 232, 120, 14), Confidence = 0.95 },
            };

            // 執行版面分析
            var result = _layoutService.AnalyzeLayout(complexLines);

            // 驗證結果
            Assert.IsTrue(result.Success, "複雜內容版面分析應該成功");
            
            var column = result.Layout.First().Value;
            
            // 應該正確識別和分割不同類型的內容
            Assert.IsTrue(column.Count >= 4, $"應該至少有4個段落，實際：{column.Count}");
            Assert.IsTrue(column.Count <= 7, $"應該最多有7個段落，實際：{column.Count}");
            
            Console.WriteLine($"✅ 複雜混合內容測試通過：7行內容分割為{column.Count}個段落");
            
            // 輸出詳細分割結果
            for (int i = 0; i < column.Count; i++)
            {
                var paragraph = column[i];
                Console.WriteLine($"   段落{i + 1}：{paragraph.Lines.Count}行 - 「{paragraph.Lines[0].Text}」");
            }
        }
    }
}
