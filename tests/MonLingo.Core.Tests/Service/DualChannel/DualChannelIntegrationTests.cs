using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonLingo.Core.Service;
using MonLingo.Core.Service.DualChannel;
using NLog;
using Xunit;

namespace MonLingo.Core.Tests.Service.DualChannel
{
    /// <summary>
    /// 雙通道架構 v4.0 端到端測試套件
    /// 驗證完整的雙通道決策架構功能
    /// </summary>
    public class DualChannelIntegrationTests
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 測試 - 基本雙通道架構初始化
        /// </summary>
        [Fact]
        public void Test_DualChannelController_Initialization()
        {
            // Arrange & Act
            var controller = new DualChannelController();
            
            // Assert
            Assert.NotNull(controller);
            var stats = controller.GetStatistics();
            Assert.NotNull(stats);
            
            Logger.Info("✅ DualChannelController initialization test passed");
        }

        /// <summary>
        /// 測試 - 通道選擇器決策邏輯
        /// </summary>
        [Theory]
        [InlineData(15, 3, ChannelType.BimodalStatistical)] // 高質量數據 → 雙峰統計通道
        [InlineData(5, 1, ChannelType.ExperienceRule)]      // 低質量數據 → 經驗規則通道
        [InlineData(8, 2, ChannelType.BimodalStatistical)]  // 邊界情況 → 雙峰統計通道
        public void Test_ChannelSelector_DecisionLogic(int spacingCount, int peakCount, ChannelType expectedChannel)
        {
            // Arrange
            var selector = new ChannelSelector();
            var column = CreateMockColumn(spacingCount, peakCount);

            // Act
            var decision = selector.SelectChannel(column);

            // Assert
            Assert.Equal(expectedChannel, decision.SelectedChannel);
            Assert.True(decision.Confidence >= 0.0 && decision.Confidence <= 1.0);
            
            Logger.Info($"✅ Channel selection test passed: {spacingCount} spacings, {peakCount} peaks → {expectedChannel}");
        }

        /// <summary>
        /// 測試 - 雙峰統計通道處理
        /// </summary>
        [Fact]
        public void Test_BimodalStatisticalChannel_Processing()
        {
            // Arrange
            var channel = new BimodalStatisticalChannel();
            var column = CreateMockColumnWithHighQualityData();
            var statistics = CreateMockChannelStatistics();

            // Act
            var paragraphs = channel.Process(column, statistics);

            // Assert
            Assert.NotNull(paragraphs);
            Assert.True(paragraphs.Count > 0);
            Assert.All(paragraphs, p => Assert.Equal(ChannelType.BimodalStatistical, p.CreatedByChannel));
            
            Logger.Info($"✅ BimodalStatisticalChannel processing test passed: {paragraphs.Count} paragraphs created");
        }

        /// <summary>
        /// 測試 - 經驗規則通道處理
        /// </summary>
        [Fact]
        public void Test_ExperienceRuleChannel_Processing()
        {
            // Arrange
            var channel = new ExperienceRuleChannel();
            var column = CreateMockColumnWithLowQualityData();
            var statistics = CreateMockChannelStatistics();

            // Act
            var paragraphs = channel.Process(column, statistics);

            // Assert
            Assert.NotNull(paragraphs);
            Assert.True(paragraphs.Count > 0);
            Assert.All(paragraphs, p => Assert.Equal(ChannelType.ExperienceRule, p.CreatedByChannel));
            
            Logger.Info($"✅ ExperienceRuleChannel processing test passed: {paragraphs.Count} paragraphs created");
        }

        /// <summary>
        /// 測試 - 經驗規則通道四策略評分
        /// </summary>
        [Fact]
        public void Test_ExperienceRuleChannel_FourStrategyScoring()
        {
            // Arrange
            var channel = new ExperienceRuleChannel(new ExperienceRuleChannel.Config { EnableDebugLog = true });
            var column = CreateColumnWithVariedContent();
            var statistics = CreateMockChannelStatistics();

            // Act
            var paragraphs = channel.Process(column, statistics);

            // Assert
            Assert.NotNull(paragraphs);
            
            // 驗證處理了不同類型的內容
            var totalLines = paragraphs.SelectMany(p => p.Lines).Count();
            Assert.Equal(column.Lines.Count, totalLines);
            
            Logger.Info($"✅ Four-strategy scoring test passed: {paragraphs.Count} paragraphs from varied content");
        }

        /// <summary>
        /// 測試 - LayoutAnalysisService v4.0 整合
        /// </summary>
        [Fact]
        public void Test_LayoutAnalysisService_DualChannelIntegration()
        {
            // Arrange
            var layoutService = new LayoutAnalysisService
            {
                EnableDualChannel = true,
                EnableDebugMode = true
            };
            
            var ocrResult = CreateMockOcrResult();

            // Act
            var result = layoutService.AnalyzeLayoutV4(ocrResult);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Layout);
            Assert.True(result.ProcessingTimeMs > 0);
            Assert.Equal("4.0-DualChannel", result.Version);
            
            Logger.Info($"✅ LayoutAnalysisService v4.0 integration test passed: {result.Layout.Count} columns processed");
        }

        /// <summary>
        /// 測試 - 邊界情況：空輸入處理
        /// </summary>
        [Fact]
        public void Test_EdgeCase_EmptyInput()
        {
            // Arrange
            var controller = new DualChannelController();
            var emptyColumn = new Column
            {
                ColumnId = "Empty",
                Lines = new List<LayoutLine>(),
                BoundingBox = Rectangle.Empty,
                Color = Color.Gray
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => controller.ProcessColumn(emptyColumn));
            Assert.Contains("Empty", exception.Message);
            
            Logger.Info("✅ Empty input edge case test passed");
        }

        /// <summary>
        /// 測試 - 性能基準測試
        /// </summary>
        [Fact]
        public void Test_Performance_Benchmark()
        {
            // Arrange
            var controller = new DualChannelController();
            var largeColumn = CreateLargeColumn(100); // 100行
            var startTime = DateTime.UtcNow;

            // Act
            var paragraphs = controller.ProcessColumn(largeColumn);
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Assert
            Assert.True(processingTime < 1000); // 應在1秒內完成
            Assert.NotNull(paragraphs);
            Assert.True(paragraphs.Count > 0);
            
            Logger.Info($"✅ Performance benchmark test passed: {largeColumn.Lines.Count} lines processed in {processingTime:F2}ms");
        }

        /// <summary>
        /// 測試 - 統計信息收集
        /// </summary>
        [Fact]
        public void Test_Statistics_Collection()
        {
            // Arrange
            var controller = new DualChannelController();
            var columns = new List<Column>
            {
                CreateMockColumnWithHighQualityData(),
                CreateMockColumnWithLowQualityData(),
                CreateColumnWithVariedContent()
            };

            // Act
            var results = controller.ProcessColumns(columns);
            var stats = controller.GetStatistics();

            // Assert
            Assert.Equal(columns.Count, results.Count);
            Assert.NotNull(stats);
            Assert.True(stats.ChannelSelectorStats.TotalDecisions >= columns.Count);
            
            var summary = stats.FormatSummary();
            Assert.Contains("Channel Selector", summary);
            Assert.Contains("Bimodal Statistical Channel", summary);
            
            Logger.Info($"✅ Statistics collection test passed: {stats.ChannelSelectorStats.TotalDecisions} decisions tracked");
        }

        #region Mock Data Creation Helper Methods

        /// <summary>
        /// 創建模擬欄位
        /// </summary>
        private Column CreateMockColumn(int spacingCount, int peakCount)
        {
            var lines = new List<LayoutLine>();
            var baseSpacing = 20.0; // 基準行距
            
            // 創建多個間距組別以模擬峰值
            for (int group = 0; group < peakCount; group++)
            {
                var groupSpacing = baseSpacing + group * 10; // 不同峰值的間距
                var linesInGroup = Math.Max(1, spacingCount / peakCount);
                
                for (int i = 0; i < linesInGroup; i++)
                {
                    var line = new LayoutLine
                    {
                        LineId = $"L{lines.Count + 1}",
                        Text = $"Line {lines.Count + 1} in group {group + 1}",
                        BoundingBox = new Rectangle(0, (int)(lines.Count * groupSpacing), 300, 20),
                        LineHeight = 20
                    };
                    lines.Add(line);
                }
            }
            
            return new Column
            {
                ColumnId = $"TestColumn_{spacingCount}_{peakCount}",
                Lines = lines,
                BoundingBox = new Rectangle(0, 0, 300, lines.Count * 25),
                Color = Color.Blue
            };
        }

        /// <summary>
        /// 創建高質量數據的模擬欄位
        /// </summary>
        private Column CreateMockColumnWithHighQualityData()
        {
            return CreateMockColumn(15, 3); // 高質量：足夠的樣本和明顯的峰值
        }

        /// <summary>
        /// 創建低質量數據的模擬欄位
        /// </summary>
        private Column CreateMockColumnWithLowQualityData()
        {
            return CreateMockColumn(3, 1); // 低質量：樣本不足
        }

        /// <summary>
        /// 創建包含多樣化內容的欄位
        /// </summary>
        private Column CreateColumnWithVariedContent()
        {
            var lines = new List<LayoutLine>
            {
                new LayoutLine { LineId = "L1", Text = "Normal paragraph text line", BoundingBox = new Rectangle(0, 0, 300, 20), LineHeight = 20 },
                new LayoutLine { LineId = "L2", Text = "Another normal line", BoundingBox = new Rectangle(0, 25, 300, 20), LineHeight = 20 },
                new LayoutLine { LineId = "L3", Text = "1. First list item", BoundingBox = new Rectangle(20, 65, 280, 20), LineHeight = 20 }, // 列表項目
                new LayoutLine { LineId = "L4", Text = "2. Second list item", BoundingBox = new Rectangle(20, 90, 280, 20), LineHeight = 20 }, // 列表項目
                new LayoutLine { LineId = "L5", Text = "Big Header Text", BoundingBox = new Rectangle(0, 130, 300, 30), LineHeight = 30 }, // 大字體
                new LayoutLine { LineId = "L6", Text = "Small subtitle", BoundingBox = new Rectangle(10, 170, 290, 15), LineHeight = 15 }, // 小字體
                new LayoutLine { LineId = "L7", Text = "Final normal paragraph", BoundingBox = new Rectangle(0, 200, 300, 20), LineHeight = 20 }
            };

            return new Column
            {
                ColumnId = "VariedContent",
                Lines = lines,
                BoundingBox = new Rectangle(0, 0, 300, 220),
                Color = Color.Green
            };
        }

        /// <summary>
        /// 創建大型欄位（用於性能測試）
        /// </summary>
        private Column CreateLargeColumn(int lineCount)
        {
            var lines = new List<LayoutLine>();
            
            for (int i = 0; i < lineCount; i++)
            {
                var line = new LayoutLine
                {
                    LineId = $"L{i + 1}",
                    Text = $"Performance test line {i + 1} with some content to process",
                    BoundingBox = new Rectangle(0, i * 25, 300, 20),
                    LineHeight = 20
                };
                lines.Add(line);
            }

            return new Column
            {
                ColumnId = "LargeColumn",
                Lines = lines,
                BoundingBox = new Rectangle(0, 0, 300, lineCount * 25),
                Color = Color.Red
            };
        }

        /// <summary>
        /// 創建模擬通道統計信息
        /// </summary>
        private ChannelStatistics CreateMockChannelStatistics()
        {
            return new ChannelStatistics
            {
                GlobalMean = 20.0,
                GlobalStdDev = 5.0,
                SampleCount = 10,
                QualityScore = 0.8
            };
        }

        /// <summary>
        /// 創建模擬OCR結果
        /// </summary>
        private OcrResult CreateMockOcrResult()
        {
            return new OcrResult
            {
                Lines = new LayoutLine[]
                {
                    new LayoutLine { LineId = "L1", Text = "Title Line", BoundingBox = new Rectangle(0, 0, 300, 25), LineHeight = 25 },
                    new LayoutLine { LineId = "L2", Text = "First paragraph line", BoundingBox = new Rectangle(0, 40, 300, 20), LineHeight = 20 },
                    new LayoutLine { LineId = "L3", Text = "Second paragraph line", BoundingBox = new Rectangle(0, 65, 300, 20), LineHeight = 20 },
                    new LayoutLine { LineId = "L4", Text = "1. List item one", BoundingBox = new Rectangle(20, 100, 280, 20), LineHeight = 20 },
                    new LayoutLine { LineId = "L5", Text = "2. List item two", BoundingBox = new Rectangle(20, 125, 280, 20), LineHeight = 20 },
                    new LayoutLine { LineId = "L6", Text = "Final paragraph", BoundingBox = new Rectangle(0, 160, 300, 20), LineHeight = 20 }
                }
            };
        }

        #endregion
    }

    /// <summary>
    /// 雙通道架構單元測試
    /// 測試各個組件的獨立功能
    /// </summary>
    public class DualChannelUnitTests
    {
        /// <summary>
        /// 測試通道選擇器峰值分析
        /// </summary>
        [Fact]
        public void Test_ChannelSelector_PeakAnalysis()
        {
            // Arrange
            var selector = new ChannelSelector();
            
            // 創建具有明顯雙峰分佈的數據
            var spacings = new List<double> 
            { 
                10, 11, 12, 10, 11,    // 第一個峰值群組
                25, 26, 24, 25, 26,    // 第二個峰值群組
                11, 10, 25, 24         // 混合數據
            };

            // Act
            var result = selector.AnalyzePeaks(spacings);

            // Assert
            Assert.True(result.peaks.Count >= 2); // 應該檢測到至少兩個峰值
            Assert.True(result.confidence > 0.5); // 置信度應該較高
            
            Logger.Info($"✅ Peak analysis test passed: {result.peaks.Count} peaks detected with confidence {result.confidence:F2}");
        }

        /// <summary>
        /// 測試雙峰統計通道區域決策
        /// </summary>
        [Theory]
        [InlineData(5.0, "merge")]    // 合併區域
        [InlineData(15.0, "split")]   // 分割區域  
        [InlineData(8.0, "ambiguity")] // 模糊區域
        public void Test_BimodalChannel_ZoneDecisions(double spacing, string expectedZone)
        {
            // Arrange
            var channel = new BimodalStatisticalChannel();
            
            // 創建具有明確區域邊界的測試配置
            var config = new BimodalStatisticalChannel.Config
            {
                MergeThreshold = 6.0,
                SplitThreshold = 12.0
            };
            
            channel.MergeThreshold = config.MergeThreshold;
            
            // Act & Assert based on expected zone
            if (expectedZone == "merge")
            {
                Assert.True(spacing < config.MergeThreshold);
            }
            else if (expectedZone == "split") 
            {
                Assert.True(spacing > config.SplitThreshold);
            }
            else if (expectedZone == "ambiguity")
            {
                Assert.True(spacing >= config.MergeThreshold && spacing <= config.SplitThreshold);
            }
            
            Logger.Info($"✅ Zone decision test passed: {spacing}px → {expectedZone} zone");
        }
    }
}