using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using NLog;
using MonLingo.Core.Services;

namespace MonLingo.Core.Services.Implementations
{
    /// <summary>
    /// 文字區域檢測器
    /// 使用 K-means 聚類和形態學操作檢測文字區域
    /// </summary>
    public class TextRegionDetector : ITextRegionDetector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 檢測圖像中的文字區域
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <param name="clusterCount">K-means 聚類數量</param>
        /// <returns>檢測到的文字區域列表</returns>
        public List<Rect> DetectTextRegions(Mat image, int clusterCount = 3)
        {
            try
            {
                if (image.Empty())
                {
                    throw new ArgumentException("輸入圖像不能為空", nameof(image));
                }

                var regions = new List<Rect>();

                using var gray = new Mat();
                using var binary = new Mat();

                // 轉換為灰階
                if (image.Channels() == 3)
                {
                    Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    image.CopyTo(gray);
                }

                // 應用高斯模糊以減少噪聲
                Cv2.GaussianBlur(gray, gray, new Size(3, 3), 0);

                // 使用 OTSU 閾值化
                Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                // 形態學操作以連接文字
                var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                using var morphed = new Mat();
                Cv2.MorphologyEx(binary, morphed, MorphTypes.Close, kernel);

                // 尋找輪廓
                Cv2.FindContours(morphed, out Point[][] contours, out HierarchyIndex[] hierarchy, 
                                 RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                // 過濾和分析輪廓
                foreach (var contour in contours)
                {
                    var boundingRect = Cv2.BoundingRect(contour);
                    
                    // 過濾太小的區域
                    if (boundingRect.Width < 10 || boundingRect.Height < 10)
                        continue;

                    // 過濾長寬比不合理的區域（可能不是文字）
                    double aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                    if (aspectRatio < 0.1 || aspectRatio > 20)
                        continue;

                    // 計算輪廓面積
                    double contourArea = Cv2.ContourArea(contour);
                    double boundingArea = boundingRect.Width * boundingRect.Height;
                    double fillRatio = contourArea / boundingArea;

                    // 過濾填充比例太低的區域
                    if (fillRatio < 0.1)
                        continue;

                    regions.Add(boundingRect);
                }

                // 如果使用 K-means，進一步處理區域
                if (clusterCount > 1 && regions.Count > clusterCount)
                {
                    regions = ApplyKMeansToRegions(regions, clusterCount);
                }

                // 按位置排序（從上到下，從左到右）
                regions = regions.OrderBy(r => r.Y).ThenBy(r => r.X).ToList();

                Logger.Info($"檢測到 {regions.Count} 個文字區域");
                return regions;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "文字區域檢測失敗");
                return new List<Rect>();
            }
        }

        /// <summary>
        /// 對檢測到的區域進行 K-means 聚類
        /// </summary>
        /// <param name="regions">原始區域列表</param>
        /// <param name="clusterCount">聚類數量</param>
        /// <returns>聚類後的代表性區域</returns>
        private List<Rect> ApplyKMeansToRegions(List<Rect> regions, int clusterCount)
        {
            try
            {
                if (regions.Count <= clusterCount)
                    return regions;

                // 準備 K-means 資料（使用區域中心點）
                var data = new Mat(regions.Count, 2, MatType.CV_32F);
                for (int i = 0; i < regions.Count; i++)
                {
                    var rect = regions[i];
                    data.Set(i, 0, rect.X + rect.Width / 2f);  // 中心 X
                    data.Set(i, 1, rect.Y + rect.Height / 2f); // 中心 Y
                }

                // 執行 K-means
                var labels = new Mat();
                var centers = new Mat();
                var criteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 10, 1.0);

                Cv2.Kmeans(data, clusterCount, labels, criteria, 3, KMeansFlags.RandomCenters, centers);

                // 為每個聚類計算合併的邊界框
                var clusteredRegions = new List<Rect>();
                for (int cluster = 0; cluster < clusterCount; cluster++)
                {
                    var clusterRects = new List<Rect>();
                    for (int i = 0; i < regions.Count; i++)
                    {
                        if (labels.At<int>(i) == cluster)
                        {
                            clusterRects.Add(regions[i]);
                        }
                    }

                    if (clusterRects.Count > 0)
                    {
                        // 計算包含所有區域的最小邊界框
                        var minX = clusterRects.Min(r => r.X);
                        var minY = clusterRects.Min(r => r.Y);
                        var maxX = clusterRects.Max(r => r.X + r.Width);
                        var maxY = clusterRects.Max(r => r.Y + r.Height);

                        clusteredRegions.Add(new Rect(minX, minY, maxX - minX, maxY - minY));
                    }
                }

                Logger.Debug($"K-means 聚類將 {regions.Count} 個區域合併為 {clusteredRegions.Count} 個");
                return clusteredRegions;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "K-means 區域聚類失敗");
                return regions; // 返回原始區域
            }
        }

        /// <summary>
        /// 分析圖像版面結構
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <returns>版面分析結果</returns>
        public LayoutAnalysisResult AnalyzeLayout(Mat image)
        {
            try
            {
                var textRegions = DetectTextRegions(image);
                
                var result = new LayoutAnalysisResult
                {
                    TextRegions = textRegions,
                    ImageSize = new Size(image.Width, image.Height),
                    RegionCount = textRegions.Count
                };

                // 分析文字密度
                var totalTextArea = textRegions.Sum(r => r.Width * r.Height);
                var totalImageArea = image.Width * image.Height;
                result.TextDensity = totalImageArea > 0 ? (double)totalTextArea / totalImageArea : 0;

                // 檢測主要方向（水平或垂直）
                result.PrimaryOrientation = DetectPrimaryOrientation(textRegions);

                Logger.Info($"版面分析完成: {textRegions.Count} 個區域，文字密度: {result.TextDensity:P2}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "版面分析失敗");
                return new LayoutAnalysisResult
                {
                    TextRegions = new List<Rect>(),
                    ImageSize = new Size(image.Width, image.Height),
                    RegionCount = 0,
                    TextDensity = 0,
                    PrimaryOrientation = TextOrientation.Horizontal
                };
            }
        }

        /// <summary>
        /// 檢測文字的主要方向
        /// </summary>
        /// <param name="regions">文字區域列表</param>
        /// <returns>主要文字方向</returns>
        private TextOrientation DetectPrimaryOrientation(List<Rect> regions)
        {
            if (regions.Count == 0)
                return TextOrientation.Horizontal;

            int horizontalCount = 0;
            int verticalCount = 0;

            foreach (var rect in regions)
            {
                double aspectRatio = (double)rect.Width / rect.Height;
                if (aspectRatio > 1.5)
                    horizontalCount++; // 寬度明顯大於高度，可能是水平文字
                else if (aspectRatio < 0.67)
                    verticalCount++;   // 高度明顯大於寬度，可能是垂直文字
            }

            return verticalCount > horizontalCount ? TextOrientation.Vertical : TextOrientation.Horizontal;
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            Logger.Debug("TextRegionDetector 已釋放");
        }
    }
}
