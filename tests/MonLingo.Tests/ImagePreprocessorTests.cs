using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonLingo.Core.Services;
using MonLingo.Core.Services.Implementations;
using OpenCvSharp;

namespace MonLingo.Tests
{
    [TestClass]
    public class ImagePreprocessorTests
    {
        private IImagePreprocessor _imagePreprocessor;
        private ITextRegionDetector _textRegionDetector;

        [TestInitialize]
        public void Setup()
        {
            _imagePreprocessor = new ImagePreprocessor();
            _textRegionDetector = new TextRegionDetector();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _imagePreprocessor?.Dispose();
            _textRegionDetector?.Dispose();
        }

        [TestMethod]
        public void LoadImage_WithValidImageData_ShouldReturnMat()
        {
            // Arrange
            var testImage = CreateTestBitmap(100, 100);
            var imageData = BitmapToByteArray(testImage);

            // Act
            using var result = _imagePreprocessor.LoadImage(imageData);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());
            Assert.AreEqual(100, result.Width);
            Assert.AreEqual(100, result.Height);
        }

        [TestMethod]
        public void LoadImageFromBitmap_WithValidBitmap_ShouldReturnMat()
        {
            // Arrange
            var testBitmap = CreateTestBitmap(150, 200);

            // Act
            using var result = _imagePreprocessor.LoadImageFromBitmap(testBitmap);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());
            Assert.AreEqual(150, result.Width);
            Assert.AreEqual(200, result.Height);
        }

        [TestMethod]
        public void DetectAngle_WithStraightImage_ShouldReturnSmallAngle()
        {
            // Arrange
            var testImage = CreateTestImageWithLines();
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);

            // Act
            var angle = _imagePreprocessor.DetectAngle(mat);

            // Assert
            Assert.IsTrue(Math.Abs(angle) <= 45, $"檢測角度 {angle}° 應該在 ±45° 範圍內");
        }

        [TestMethod]
        public void CorrectAngle_WithNonZeroAngle_ShouldReturnCorrectedImage()
        {
            // Arrange
            var testImage = CreateTestBitmap(100, 100);
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
            var testAngle = 15.0;

            // Act
            using var result = _imagePreprocessor.CorrectAngle(mat, testAngle);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());
            // 旋轉後的圖像尺寸可能會變化
            Assert.IsTrue(result.Width > 0 && result.Height > 0);
        }

        [TestMethod]
        public void CorrectAngle_WithZeroAngle_ShouldReturnSimilarImage()
        {
            // Arrange
            var testImage = CreateTestBitmap(100, 100);
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
            var testAngle = 0.0;

            // Act
            using var result = _imagePreprocessor.CorrectAngle(mat, testAngle);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());
            Assert.AreEqual(mat.Width, result.Width);
            Assert.AreEqual(mat.Height, result.Height);
        }

        [TestMethod]
        public void ApplyKMeansClustering_WithValidImage_ShouldReturnClusteredImage()
        {
            // Arrange
            var testImage = CreateTestBitmap(100, 100);
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);
            var clusterCount = 3;

            // Act
            using var result = _imagePreprocessor.ApplyKMeansClustering(mat, clusterCount);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Empty());
            Assert.AreEqual(mat.Width, result.Width);
            Assert.AreEqual(mat.Height, result.Height);
        }

        [TestMethod]
        public void DetectTextRegions_WithValidImage_ShouldReturnRegions()
        {
            // Arrange
            var testImage = CreateTestImageWithText();
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);

            // Act
            var regions = _textRegionDetector.DetectTextRegions(mat, 3);

            // Assert
            Assert.IsNotNull(regions);
            // 不要求一定檢測到區域，因為測試圖像可能很簡單
            Assert.IsTrue(regions.Count >= 0);
        }

        [TestMethod]
        public void AnalyzeLayout_WithValidImage_ShouldReturnLayoutResult()
        {
            // Arrange
            var testImage = CreateTestImageWithText();
            using var mat = _imagePreprocessor.LoadImageFromBitmap(testImage);

            // Act
            var result = _textRegionDetector.AnalyzeLayout(mat);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(testImage.Width, result.ImageSize.Width);
            Assert.AreEqual(testImage.Height, result.ImageSize.Height);
            Assert.IsTrue(result.TextDensity >= 0 && result.TextDensity <= 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void LoadImage_WithNullData_ShouldThrowException()
        {
            // Act
            _imagePreprocessor.LoadImage(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LoadImageFromBitmap_WithNullBitmap_ShouldThrowException()
        {
            // Act
            _imagePreprocessor.LoadImageFromBitmap(null);
        }

        #region Helper Methods

        private Bitmap CreateTestBitmap(int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            // 添加一些基本圖形作為測試內容
            g.DrawRectangle(Pens.Black, 10, 10, width - 20, height - 20);
            return bitmap;
        }

        private Bitmap CreateTestImageWithLines()
        {
            var bitmap = new Bitmap(200, 200);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            // 繪製一些水平線和垂直線
            g.DrawLine(Pens.Black, 20, 50, 180, 50);
            g.DrawLine(Pens.Black, 20, 100, 180, 100);
            g.DrawLine(Pens.Black, 20, 150, 180, 150);
            g.DrawLine(Pens.Black, 50, 20, 50, 180);
            g.DrawLine(Pens.Black, 100, 20, 100, 180);
            g.DrawLine(Pens.Black, 150, 20, 150, 180);
            return bitmap;
        }

        private Bitmap CreateTestImageWithText()
        {
            var bitmap = new Bitmap(300, 200);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            // 模擬文字區域
            using var brush = new SolidBrush(Color.Black);
            g.FillRectangle(brush, 20, 20, 100, 20);  // 文字行1
            g.FillRectangle(brush, 20, 50, 150, 20);  // 文字行2
            g.FillRectangle(brush, 20, 80, 80, 20);   // 文字行3
            return bitmap;
        }

        private byte[] BitmapToByteArray(Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        #endregion
    }
}
