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
using MonLingo.Core.Services.Implementations;

namespace MonLingo.Tests.EndToEnd
{
    /// <summary>
    /// 端到端工作流程測試套件
    /// 測試完整的用戶使用場景和系統整合
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    public class E2EWorkflowTests
    {
        private IImagePreprocessor _imagePreprocessor;
        private List<TestImageData> _testImages;

        [TestInitialize]
        public async Task Setup()
        {
            // 初始化服務
            _imagePreprocessor = new ImagePreprocessor();
            
            // 準備測試圖像數據
            _testImages = new List<TestImageData>();
            
            await Task.Delay(10); // 模擬初始化
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            // 清理資源
            _imagePreprocessor?.Dispose();
            
            await Task.Delay(10); // 模擬清理
        }

        /// <summary>
        /// 測試基本的翻譯工作流程
        /// 模擬用戶從截圖到翻譯的完整流程
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Critical")]
        [TestCategory("Translation")]
        public async Task BasicTranslationWorkflow_ShouldCompleteSuccessfully()
        {
            // Arrange
            var captureService = new CaptureService(null);
            var translateService = new MonLingo.Core.Service.TranslateService();
            
            var testRegion = new System.Drawing.Rectangle(100, 100, 300, 100);
            
            // Act
            var captureResult = await captureService.CaptureScreenRegionAsync(testRegion);
            Assert.IsTrue(captureResult.Success, "截圖應該成功");
            
            // 模擬 OCR 文字提取
            string extractedText = "Hello World";
            
            var translationResult = await translateService.TranslateAsync(extractedText, "en", "zh-TW");
            
            // Assert
            Assert.IsTrue(translationResult.IsSuccess, "翻譯應該成功");
            Assert.IsNotNull(translationResult.TranslatedText, "翻譯結果不應為空");
            Assert.AreNotEqual(extractedText, translationResult.TranslatedText, "翻譯文字應與原文不同");
        }

        /// <summary>
        /// 測試多語言支援的端到端工作流程
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Multilingual")]
        public async Task MultilingualWorkflow_ShouldHandleVariousLanguages()
        {
            // Arrange
            var translateService = new MonLingo.Core.Service.TranslateService();
            var languageDetectionService = new LanguageDetectionService();
            
            var testTexts = new Dictionary<string, string>
            {
                { "Hello World", "en" },
                { "你好世界", "zh" },
                { "こんにちは世界", "ja" }
            };
            
            // Act & Assert
            foreach (var testCase in testTexts)
            {
                string text = testCase.Key;
                string expectedLang = testCase.Value;
                
                // 語言檢測
                var detectedLang = await languageDetectionService.DetectLanguageAsync(text);
                
                // 翻譯
                var translationResult = await translateService.TranslateAsync(text, detectedLang, "zh-TW");
                
                Assert.IsTrue(translationResult.IsSuccess, $"翻譯 '{text}' 應該成功");
                Assert.IsNotNull(translationResult.TranslatedText, $"翻譯結果 '{text}' 不應為空");
            }
        }

        /// <summary>
        /// 性能測試：批量翻譯工作流程
        /// </summary>
        [TestMethod]
        [TestCategory("E2E")]
        [TestCategory("Performance")]
        public async Task BatchTranslationWorkflow_ShouldMeetPerformanceRequirements()
        {
            // Arrange
            var translateService = new TranslationService();
            var batchTexts = Enumerable.Range(1, 10)
                .Select(i => $"Test text {i}")
                .ToArray();
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var results = await translateService.TranslateBatchAsync(batchTexts, "en", "zh-TW");
            
            stopwatch.Stop();
            
            // Assert
            Assert.IsNotNull(results, "批量翻譯結果不應為空");
            Assert.AreEqual(batchTexts.Length, results.Length, "翻譯結果數量應與輸入相符");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, "批量翻譯應在5秒內完成");
            
            foreach (var result in results)
            {
                Assert.IsTrue(result.IsSuccess, "每個翻譯結果都應該成功");
            }
        }
    }

    /// <summary>
    /// 測試圖像數據結構
    /// </summary>
    public class TestImageData
    {
        public string Name { get; set; }
        public byte[] ImageData { get; set; }
        public string ExpectedText { get; set; }
        public string Language { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
