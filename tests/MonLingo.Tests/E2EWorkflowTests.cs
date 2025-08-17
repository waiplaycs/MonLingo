using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonLingo.Core;
using MonLingo.Core.Services;

namespace MonLingo.Tests.EndToEnd
{
    /// <summary>
    /// 端到端工作流程測試套件
    /// 測試完整的用戶使用場景和系統整合
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    public class EndToEndWorkflowTests
    {
        private IImagePreprocessor _imagePreprocessor;
        private IOcrEngine _ocrEngine;
        private List<TestImageData> _testImages;

        [TestInitialize]
        public async Task Setup()
        {
            // 初始化服務
            _imagePreprocessor = new ImagePreprocessor();
            _ocrEngine = new OcrEngine();

            // 初始化 Native Bridge
            var success = await NativeBridge.InitializeAsync();
            Assert.IsTrue(success, "Native DLL 應該成功初始化");

            // 準備測試圖像
            _testImages = CreateTestImages();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await NativeBridge.CleanupAsync();
            _imagePreprocessor?.Dispose();
            _ocrEngine?.Dispose();
            
            foreach (var testImage in _testImages)
            {
                testImage?.Dispose();
            }
        }

        /// <summary>
        /// 端到端 OCR 翻譯工作流程測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Critical")]
        public async Task E2E_OCR_Translation_Workflow_Should_Complete_Successfully()
        {
            // Arrange
            var testImage = CreateTestBitmapWithText("測試中文文字識別", "zh");
            var expectedTextContains = "測試";

            var stopwatch = Stopwatch.StartNew();

            // Act - Step 1: 圖像預處理
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
            var angle = _imagePreprocessor.DetectAngle(mat);
            using var correctedImage = _imagePreprocessor.CorrectAngle(mat, angle);

            // Act - Step 2: OCR 文字識別
            var ocrResults = await _ocrEngine.RecognizeTextAsync(correctedImage);

            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(ocrResults, "OCR 識別應該完成");
            Assert.IsTrue(ocrResults.Any(), "應該識別出文字內容");
            
            var recognizedText = string.Join(" ", ocrResults.Select(r => r.Text));
            Assert.IsTrue(recognizedText.Contains(expectedTextContains), 
                $"識別結果應該包含 '{expectedTextContains}'，實際識別: {recognizedText}");

            // 性能驗證
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000, 
                $"完整工作流程應該在 3 秒內完成，實際用時: {stopwatch.ElapsedMilliseconds}ms");

            Console.WriteLine($"端到端測試完成:");
            Console.WriteLine($"  OCR 識別: {recognizedText}");
            Console.WriteLine($"  總處理時間: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  角度校正: {angle}度");
        }

        /// <summary>
        /// 多語言支援端到端測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Multilingual")]
        public async Task E2E_Multilingual_Support_Should_Work_Correctly()
        {
            // Arrange
            var testCases = new[]
            {
                new { Language = "zh", Text = "中文測試", ExpectedContains = "中文" },
                new { Language = "ja", Text = "日本語テスト", ExpectedContains = "日本" },
                new { Language = "ko", Text = "한국어 테스트", ExpectedContains = "한국" },
                new { Language = "en", Text = "English Test", ExpectedContains = "English" }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"測試語言: {testCase.Language}");

                // Act - 創建測試圖像
                var testImage = CreateTestBitmapWithText(testCase.Text, testCase.Language);
                
                // Act - 執行完整流程
                using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
                var angle = _imagePreprocessor.DetectAngle(mat);
                using var correctedImage = _imagePreprocessor.CorrectAngle(mat, angle);
                var ocrResults = await _ocrEngine.RecognizeTextAsync(correctedImage);

                // Assert
                Assert.IsNotNull(ocrResults, $"{testCase.Language} OCR 應該完成");
                Assert.IsTrue(ocrResults.Any(), $"{testCase.Language} 應該識別出文字");

                var recognizedText = string.Join(" ", ocrResults.Select(r => r.Text));
                Console.WriteLine($"  識別結果: {recognizedText}");
                Console.WriteLine($"  信心度: {ocrResults.Average(r => r.Confidence):P2}");

                testImage.Dispose();
            }
        }

        /// <summary>
        /// 錯誤處理與恢復流程測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("ErrorHandling")]
        public async Task E2E_Error_Handling_And_Recovery_Should_Work_Correctly()
        {
            // Test Case 1: 空圖像處理
            await TestEmptyImageScenario();

            // Test Case 2: 損壞圖像處理
            await TestCorruptedImageScenario();

            // Test Case 3: 極大圖像處理
            await TestLargeImageScenario();
        }

        /// <summary>
        /// 性能壓力測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Performance")]
        public async Task E2E_Performance_Stress_Test_Should_Meet_Requirements()
        {
            // Arrange
            const int testIterations = 20;
            var latencies = new List<TimeSpan>();
            var initialMemory = GC.GetTotalMemory(true);

            // Act - 執行壓力測試
            for (int i = 0; i < testIterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                await ExecuteFullWorkflowAsync();
                stopwatch.Stop();
                latencies.Add(stopwatch.Elapsed);

                // 每 5 次迭代強制垃圾回收
                if (i % 5 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // Assert - 性能要求驗證
            var avgLatency = latencies.Average(t => t.TotalMilliseconds);
            var p95Latency = latencies.OrderBy(t => t.TotalMilliseconds)
                                   .Skip((int)(latencies.Count * 0.95))
                                   .First().TotalMilliseconds;

            Assert.IsTrue(avgLatency < 2000, $"平均端到端延遲應該小於 2 秒，實際: {avgLatency:F2}ms");
            Assert.IsTrue(p95Latency < 3000, $"P95 端到端延遲應該小於 3 秒，實際: {p95Latency:F2}ms");

            // 記憶體洩漏檢查
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryGrowth = (finalMemory - initialMemory) / 1024.0 / 1024.0;

            Assert.IsTrue(memoryGrowth < 50, $"記憶體增長應該小於 50MB，實際: {memoryGrowth:F2}MB");

            Console.WriteLine($"壓力測試結果:");
            Console.WriteLine($"  測試次數: {testIterations}");
            Console.WriteLine($"  平均延遲: {avgLatency:F2}ms");
            Console.WriteLine($"  P95 延遲: {p95Latency:F2}ms");
            Console.WriteLine($"  記憶體增長: {memoryGrowth:F2}MB");
        }

        /// <summary>
        /// 系統整合測試
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Integration")]
        public async Task E2E_System_Integration_Should_Work_Correctly()
        {
            // Test Native Bridge 初始化
            var isInitialized = await NativeBridge.IsInitializedAsync();
            Assert.IsTrue(isInitialized, "Native Bridge 應該已初始化");

            // Test 服務間協作
            var testImage = CreateTestBitmapWithText("整合測試", "zh");
            
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
            var preprocessorWorking = mat != null;
            Assert.IsTrue(preprocessorWorking, "圖像預處理器應該正常工作");

            var ocrResults = await _ocrEngine.RecognizeTextAsync(mat);
            var ocrWorking = ocrResults != null && ocrResults.Any();
            Assert.IsTrue(ocrWorking, "OCR 引擎應該正常工作");

            Console.WriteLine("系統整合測試通過:");
            Console.WriteLine($"  Native Bridge: ✓");
            Console.WriteLine($"  圖像預處理: ✓");
            Console.WriteLine($"  OCR 引擎: ✓");

            testImage.Dispose();
        }

        #region 輔助方法

        private async Task<bool> ExecuteFullWorkflowAsync()
        {
            try
            {
                var testImage = CreateTestBitmapWithText("性能測試文字", "zh");
                
                using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
                var angle = _imagePreprocessor.DetectAngle(mat);
                using var correctedImage = _imagePreprocessor.CorrectAngle(mat, angle);
                var ocrResults = await _ocrEngine.RecognizeTextAsync(correctedImage);
                
                testImage.Dispose();
                return ocrResults != null && ocrResults.Any();
            }
            catch
            {
                return false;
            }
        }

        private async Task TestEmptyImageScenario()
        {
            try
            {
                var emptyImage = new Bitmap(1, 1);
                using var mat = _imagePreprocessor.LoadImageFromBitmap(emptyImage);
                var ocrResults = await _ocrEngine.RecognizeTextAsync(mat);
                
                // 應該優雅處理空圖像，不拋出異常
                Assert.IsNotNull(ocrResults, "空圖像應該返回空結果而不是 null");
                emptyImage.Dispose();
            }
            catch (Exception ex)
            {
                Assert.Fail($"空圖像處理不應該拋出異常: {ex.Message}");
            }
        }

        private async Task TestCorruptedImageScenario()
        {
            try
            {
                // 創建一個"損壞"的圖像（純黑色）
                var corruptedImage = new Bitmap(100, 100);
                using (var graphics = Graphics.FromImage(corruptedImage))
                {
                    graphics.Clear(Color.Black);
                }

                using var mat = _imagePreprocessor.LoadImageFromBitmap(corruptedImage);
                var ocrResults = await _ocrEngine.RecognizeTextAsync(mat);
                
                // 應該優雅處理損壞圖像
                Assert.IsNotNull(ocrResults, "損壞圖像應該返回結果而不是 null");
                corruptedImage.Dispose();
            }
            catch (Exception ex)
            {
                Assert.Fail($"損壞圖像處理不應該拋出異常: {ex.Message}");
            }
        }

        private async Task TestLargeImageScenario()
        {
            try
            {
                // 創建大尺寸圖像
                var largeImage = CreateTestBitmapWithText("大圖像測試", "zh", 2048, 1536);
                
                var stopwatch = Stopwatch.StartNew();
                using var mat = _imagePreprocessor.LoadImageFromBitmap(largeImage);
                var ocrResults = await _ocrEngine.RecognizeTextAsync(mat);
                stopwatch.Stop();

                // 應該在合理時間內處理大圖像
                Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, 
                    $"大圖像處理應該在 10 秒內完成，實際: {stopwatch.ElapsedMilliseconds}ms");
                Assert.IsNotNull(ocrResults, "大圖像應該正常處理");
                
                largeImage.Dispose();
            }
            catch (Exception ex)
            {
                Assert.Fail($"大圖像處理不應該拋出異常: {ex.Message}");
            }
        }

        private List<TestImageData> CreateTestImages()
        {
            var testImages = new List<TestImageData>();

            // 創建不同場景的測試圖像
            testImages.Add(new TestImageData
            {
                Name = "Chinese_Text",
                Image = CreateTestBitmapWithText("中文測試文字", "zh"),
                ExpectedText = "中文測試文字"
            });

            testImages.Add(new TestImageData
            {
                Name = "English_Text", 
                Image = CreateTestBitmapWithText("English Test Text", "en"),
                ExpectedText = "English Test Text"
            });

            return testImages;
        }

        private Bitmap CreateTestBitmapWithText(string text, string language, int width = 800, int height = 600)
        {
            var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.Clear(Color.White);
            
            var font = GetFontForLanguage(language);
            var brush = new SolidBrush(Color.Black);
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            graphics.DrawString(text, font, brush, new RectangleF(0, 0, width, height), format);
            
            return bitmap;
        }

        private Font GetFontForLanguage(string language)
        {
            return language switch
            {
                "zh" => new Font("Microsoft YaHei", 36, FontStyle.Regular),
                "ja" => new Font("MS Gothic", 36, FontStyle.Regular),
                "ko" => new Font("Malgun Gothic", 36, FontStyle.Regular),
                _ => new Font("Arial", 36, FontStyle.Regular)
            };
        }

        #endregion
    }

    /// <summary>
    /// 測試圖像資料
    /// </summary>
    public class TestImageData : IDisposable
    {
        public string Name { get; set; }
        public Bitmap Image { get; set; }
        public string ExpectedText { get; set; }

        public void Dispose()
        {
            Image?.Dispose();
        }
    }
}
