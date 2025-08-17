using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Services;
using MonLingo.Core.Services.Implementations;
using OpenCvSharp;

namespace MonLingo.Tests
{
    /// <summary>
    /// Phase 2 KPI 達成驗證測試套件
    /// 根據 Phase_2_Recovery_Action_Plan.md 的 KPI 要求進行驗證
    /// </summary>
    [TestClass]
    public class Phase2PerformanceTests
    {
        private IImagePreprocessor _imagePreprocessor;
        private ITextRegionDetector _textRegionDetector;
        private List<TestImageData> _angleTestImages;
        private List<TestImageData> _performanceTestImages;

        [TestInitialize]
        public async Task Setup()
        {
            // 初始化服務
            _imagePreprocessor = new ImagePreprocessor();
            _textRegionDetector = new TextRegionDetector();

            // 準備測試資料集
            await PrepareTestDataAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _imagePreprocessor?.Dispose();
            _textRegionDetector?.Dispose();
        }

        #region KPI 驗證測試

        /// <summary>
        /// KPI: 角度校正成功率 ≥ 95% (基準集)
        /// </summary>
        [TestMethod]
        [TestCategory("KPI")]
        public void ImagePreprocessing_Angle_Detection_Should_Meet_KPI()
        {
            // Arrange
            var kpiTarget = 0.95; // 95% 成功率
            var toleranceAngle = 2.0; // ±2° 容忍度
            var successCount = 0;

            // Act & Assert
            foreach (var testImage in _angleTestImages)
            {
                using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage.Image);
                var detectedAngle = _imagePreprocessor.DetectAngle(mat);
                
                var angleDifference = Math.Abs(detectedAngle - testImage.ExpectedAngle);
                if (angleDifference <= toleranceAngle)
                {
                    successCount++;
                }

                Console.WriteLine($"圖像: {testImage.Name}, 期望角度: {testImage.ExpectedAngle:F2}°, " +
                                $"檢測角度: {detectedAngle:F2}°, 差異: {angleDifference:F2}°");
            }

            var successRate = (double)successCount / _angleTestImages.Count;
            
            Console.WriteLine($"角度檢測成功率: {successRate:P2} ({successCount}/{_angleTestImages.Count})");
            Console.WriteLine($"KPI 要求: ≥ {kpiTarget:P2}");

            Assert.IsTrue(successRate >= kpiTarget, 
                $"角度檢測成功率 {successRate:P2} 低於 KPI 要求 ({kpiTarget:P2})");
        }

        /// <summary>
        /// KPI: 前處理延遲 p95 ≤ 20ms
        /// </summary>
        [TestMethod]
        [TestCategory("KPI")]
        public async Task ImagePreprocessing_Latency_Should_Meet_KPI()
        {
            // Arrange
            var kpiTarget = 20; // 20ms
            var testIterations = 100;
            var latencies = new List<double>();

            // Warm up
            for (int i = 0; i < 5; i++)
            {
                using var warmupImage = CreateTestBitmap(800, 600);
                using var warmupMat = _imagePreprocessor.LoadImageFromBitmap(warmupImage);
                _imagePreprocessor.DetectAngle(warmupMat);
            }

            // Act - 測量前處理延遲
            for (int i = 0; i < testIterations; i++)
            {
                using var testImage = CreateTestBitmap(800, 600);
                
                var stopwatch = Stopwatch.StartNew();
                
                // 完整的前處理流程
                using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
                var angle = _imagePreprocessor.DetectAngle(mat);
                using var corrected = _imagePreprocessor.CorrectAngle(mat, angle);
                
                stopwatch.Stop();
                latencies.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            // Assert - 計算 P95
            latencies.Sort();
            var p95Index = (int)Math.Ceiling(0.95 * latencies.Count) - 1;
            var p95Latency = latencies[p95Index];
            var avgLatency = latencies.Average();

            Console.WriteLine($"前處理延遲統計 (基於 {testIterations} 次測試):");
            Console.WriteLine($"  平均延遲: {avgLatency:F2}ms");
            Console.WriteLine($"  P95 延遲: {p95Latency:F2}ms");
            Console.WriteLine($"  最小延遲: {latencies.Min():F2}ms");
            Console.WriteLine($"  最大延遲: {latencies.Max():F2}ms");
            Console.WriteLine($"  KPI 要求: P95 ≤ {kpiTarget}ms");

            Assert.IsTrue(p95Latency <= kpiTarget, 
                $"前處理 P95 延遲 {p95Latency:F2}ms 超過 KPI 要求 ({kpiTarget}ms)");
        }

        /// <summary>
        /// KPI: K-means 聚類正常工作
        /// </summary>
        [TestMethod]
        [TestCategory("KPI")]
        public void KMeans_Clustering_Should_Work_Correctly()
        {
            // Arrange
            var testClusters = new[] { 2, 3, 4, 5 };
            var testImages = CreateVariedTestImages();

            // Act & Assert
            foreach (var clusterCount in testClusters)
            {
                foreach (var testImage in testImages)
                {
                    using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage.Image);
                    
                    var stopwatch = Stopwatch.StartNew();
                    using var clusteredImage = _imagePreprocessor.ApplyKMeansClustering(mat, clusterCount);
                    stopwatch.Stop();

                    // 驗證聚類結果
                    Assert.IsNotNull(clusteredImage, $"K-means 聚類結果不應為 null (clusters: {clusterCount})");
                    Assert.IsFalse(clusteredImage.Empty(), $"K-means 聚類結果不應為空 (clusters: {clusterCount})");
                    Assert.AreEqual(mat.Width, clusteredImage.Width, "聚類後圖像寬度應保持不變");
                    Assert.AreEqual(mat.Height, clusteredImage.Height, "聚類後圖像高度應保持不變");

                    Console.WriteLine($"K-means 聚類 (clusters: {clusterCount}, 圖像: {testImage.Name}) " +
                                    $"處理時間: {stopwatch.ElapsedMilliseconds}ms");
                }
            }
        }

        /// <summary>
        /// KPI: 文字區域檢測功能驗證
        /// </summary>
        [TestMethod]
        [TestCategory("KPI")]
        public void TextRegion_Detection_Should_Work_Correctly()
        {
            // Arrange
            var textImages = CreateTextTestImages();

            // Act & Assert
            foreach (var testImage in textImages)
            {
                using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage.Image);
                
                var stopwatch = Stopwatch.StartNew();
                var regions = _textRegionDetector.DetectTextRegions(mat, 3);
                var layoutResult = _textRegionDetector.AnalyzeLayout(mat);
                stopwatch.Stop();

                // 驗證檢測結果
                Assert.IsNotNull(regions, "文字區域檢測結果不應為 null");
                Assert.IsNotNull(layoutResult, "版面分析結果不應為 null");
                Assert.AreEqual(testImage.Image.Width, layoutResult.ImageSize.Width, "版面分析圖像寬度應正確");
                Assert.AreEqual(testImage.Image.Height, layoutResult.ImageSize.Height, "版面分析圖像高度應正確");
                Assert.IsTrue(layoutResult.TextDensity >= 0 && layoutResult.TextDensity <= 1, 
                    "文字密度應在 0-1 範圍內");

                Console.WriteLine($"文字區域檢測 (圖像: {testImage.Name}):");
                Console.WriteLine($"  檢測到區域: {regions.Count} 個");
                Console.WriteLine($"  文字密度: {layoutResult.TextDensity:P2}");
                Console.WriteLine($"  主要方向: {layoutResult.PrimaryOrientation}");
                Console.WriteLine($"  處理時間: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// 記憶體使用量驗證
        /// </summary>
        [TestMethod]
        [TestCategory("KPI")]
        public void Memory_Usage_Should_Be_Reasonable()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var testIterations = 50;

            // Act - 執行多次圖像處理操作
            for (int i = 0; i < testIterations; i++)
            {
                using var testImage = CreateTestBitmap(1920, 1080); // 1080p 圖像
                using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
                
                var angle = _imagePreprocessor.DetectAngle(mat);
                using var corrected = _imagePreprocessor.CorrectAngle(mat, angle);
                using var clustered = _imagePreprocessor.ApplyKMeansClustering(corrected, 3);
                
                var regions = _textRegionDetector.DetectTextRegions(clustered);
                var layout = _textRegionDetector.AnalyzeLayout(clustered);

                // 每 10 次迭代強制垃圾回收
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // Assert - 檢查記憶體使用量
            var finalMemory = GC.GetTotalMemory(true);
            var memoryDifference = finalMemory - initialMemory;
            var memoryDifferenceMB = memoryDifference / (1024.0 * 1024.0);

            Console.WriteLine($"記憶體使用統計:");
            Console.WriteLine($"  初始記憶體: {initialMemory / (1024.0 * 1024.0):F2} MB");
            Console.WriteLine($"  最終記憶體: {finalMemory / (1024.0 * 1024.0):F2} MB");
            Console.WriteLine($"  記憶體差異: {memoryDifferenceMB:F2} MB");
            Console.WriteLine($"  測試迭代: {testIterations} 次");

            // 記憶體增長應該合理 (< 100MB)
            Assert.IsTrue(memoryDifferenceMB < 100, 
                $"記憶體使用量增長過多: {memoryDifferenceMB:F2} MB");
        }

        #endregion

        #region 輔助方法

        private async Task PrepareTestDataAsync()
        {
            _angleTestImages = CreateAngleTestImages();
            _performanceTestImages = CreatePerformanceTestImages();
        }

        private List<TestImageData> CreateAngleTestImages()
        {
            var testImages = new List<TestImageData>();

            // 創建不同角度的測試圖像
            var testAngles = new[] { 0, 5, 10, 15, -5, -10, -15, 30, -30 };
            
            foreach (var angle in testAngles)
            {
                var bitmap = CreateRotatedTestImage(angle);
                testImages.Add(new TestImageData
                {
                    Name = $"Rotated_{angle}deg",
                    Image = bitmap,
                    ExpectedAngle = angle
                });
            }

            return testImages;
        }

        private List<TestImageData> CreatePerformanceTestImages()
        {
            var testImages = new List<TestImageData>();
            
            // 不同尺寸的性能測試圖像
            var sizes = new[] 
            { 
                new { Width = 640, Height = 480, Name = "VGA" },
                new { Width = 800, Height = 600, Name = "SVGA" },
                new { Width = 1024, Height = 768, Name = "XGA" },
                new { Width = 1920, Height = 1080, Name = "1080p" }
            };

            foreach (var size in sizes)
            {
                var bitmap = CreateTestBitmap(size.Width, size.Height);
                testImages.Add(new TestImageData
                {
                    Name = size.Name,
                    Image = bitmap,
                    ExpectedAngle = 0
                });
            }

            return testImages;
        }

        private List<TestImageData> CreateVariedTestImages()
        {
            var testImages = new List<TestImageData>();

            // 不同類型的測試圖像
            testImages.Add(new TestImageData
            {
                Name = "Simple_Shapes",
                Image = CreateTestBitmap(400, 300),
                ExpectedAngle = 0
            });

            testImages.Add(new TestImageData
            {
                Name = "Complex_Lines",
                Image = CreateComplexLineImage(),
                ExpectedAngle = 0
            });

            testImages.Add(new TestImageData
            {
                Name = "Text_Regions",
                Image = CreateTextRegionImage(),
                ExpectedAngle = 0
            });

            return testImages;
        }

        private List<TestImageData> CreateTextTestImages()
        {
            var testImages = new List<TestImageData>();

            testImages.Add(new TestImageData
            {
                Name = "Horizontal_Text",
                Image = CreateHorizontalTextImage(),
                ExpectedAngle = 0
            });

            testImages.Add(new TestImageData
            {
                Name = "Mixed_Layout",
                Image = CreateMixedLayoutImage(),
                ExpectedAngle = 0
            });

            testImages.Add(new TestImageData
            {
                Name = "Dense_Text",
                Image = CreateDenseTextImage(),
                ExpectedAngle = 0
            });

            return testImages;
        }

        private Bitmap CreateTestBitmap(int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            
            // 添加一些基本圖形
            g.DrawRectangle(Pens.Black, 10, 10, width - 20, height - 20);
            g.DrawLine(Pens.Red, 0, height / 2, width, height / 2);
            g.DrawLine(Pens.Blue, width / 2, 0, width / 2, height);
            
            return bitmap;
        }

        private Bitmap CreateRotatedTestImage(double angle)
        {
            var bitmap = new Bitmap(400, 300);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            // 繪製一些明顯的直線用於角度檢測
            using var pen = new Pen(Color.Black, 2);
            
            // 水平線
            g.DrawLine(pen, 50, 100, 350, 100);
            g.DrawLine(pen, 50, 150, 350, 150);
            g.DrawLine(pen, 50, 200, 350, 200);
            
            // 垂直線
            g.DrawLine(pen, 100, 50, 100, 250);
            g.DrawLine(pen, 200, 50, 200, 250);
            g.DrawLine(pen, 300, 50, 300, 250);

            // 創建實際旋轉的圖像來模擬真實場景
            if (Math.Abs(angle) > 0.1)
            {
                var rotated = new Bitmap(500, 400);
                using var gr = Graphics.FromImage(rotated);
                gr.Clear(Color.White);
                gr.TranslateTransform(250, 200);
                gr.RotateTransform((float)angle);
                gr.TranslateTransform(-200, -150);
                gr.DrawImage(bitmap, 0, 0);
                bitmap.Dispose();
                return rotated;
            }

            return bitmap;
        }

        private Bitmap CreateComplexLineImage()
        {
            var bitmap = new Bitmap(600, 400);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            // 複雜的線條圖案
            for (int i = 0; i < 10; i++)
            {
                g.DrawLine(Pens.Black, i * 60, 0, i * 60, 400);
                g.DrawLine(Pens.Black, 0, i * 40, 600, i * 40);
            }

            return bitmap;
        }

        private Bitmap CreateTextRegionImage()
        {
            var bitmap = new Bitmap(500, 300);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            // 模擬文字區域
            using var brush = new SolidBrush(Color.Black);
            g.FillRectangle(brush, 50, 50, 200, 25);   // 標題
            g.FillRectangle(brush, 50, 100, 180, 15);  // 段落1
            g.FillRectangle(brush, 50, 125, 220, 15);  // 段落2
            g.FillRectangle(brush, 50, 150, 160, 15);  // 段落3
            g.FillRectangle(brush, 300, 50, 150, 200); // 側欄

            return bitmap;
        }

        private Bitmap CreateHorizontalTextImage()
        {
            var bitmap = new Bitmap(600, 200);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            // 水平文字行
            using var brush = new SolidBrush(Color.Black);
            for (int i = 0; i < 8; i++)
            {
                g.FillRectangle(brush, 50, 30 + i * 20, 500, 12);
            }

            return bitmap;
        }

        private Bitmap CreateMixedLayoutImage()
        {
            var bitmap = new Bitmap(800, 600);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            // 混合版面
            using var brush = new SolidBrush(Color.Black);
            
            // 標題區域
            g.FillRectangle(brush, 50, 50, 700, 30);
            
            // 雙欄文字
            for (int i = 0; i < 15; i++)
            {
                g.FillRectangle(brush, 50, 120 + i * 18, 300, 12);
                g.FillRectangle(brush, 400, 120 + i * 18, 300, 12);
            }

            return bitmap;
        }

        private Bitmap CreateDenseTextImage()
        {
            var bitmap = new Bitmap(400, 600);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);

            // 密集文字
            using var brush = new SolidBrush(Color.Black);
            for (int i = 0; i < 35; i++)
            {
                g.FillRectangle(brush, 20, 20 + i * 16, 360, 10);
            }

            return bitmap;
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
        public double ExpectedAngle { get; set; }

        public void Dispose()
        {
            Image?.Dispose();
        }
    }
}
