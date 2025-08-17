using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using NLog;

namespace MonLingo.Core.Services.Implementations
{
    /// <summary>
    /// 圖像前處理服務 - OpenCV 實現
    /// 提供角度檢測、校正、K-means 聚類等功能
    /// </summary>
    public class ImagePreprocessor : IImagePreprocessor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 從位元組陣列載入圖像
        /// </summary>
        /// <param name="imageData">圖像位元組陣列</param>
        /// <returns>OpenCV Mat 物件</returns>
        public Mat LoadImage(byte[] imageData)
        {
            try
            {
                if (imageData == null || imageData.Length == 0)
                {
                    throw new ArgumentException("圖像資料不能為空", nameof(imageData));
                }

                var mat = Cv2.ImDecode(imageData, ImreadModes.Color);
                if (mat.Empty())
                {
                    throw new InvalidOperationException("無法解碼圖像資料");
                }

                Logger.Info($"成功載入圖像，大小: {mat.Width}x{mat.Height}");
                return mat;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "載入圖像失敗");
                throw;
            }
        }

        /// <summary>
        /// 從 System.Drawing.Bitmap 轉換為 OpenCV Mat
        /// </summary>
        /// <param name="bitmap">System.Drawing.Bitmap 物件</param>
        /// <returns>OpenCV Mat 物件</returns>
        public Mat LoadImageFromBitmap(Bitmap bitmap)
        {
            try
            {
                if (bitmap == null)
                {
                    throw new ArgumentNullException(nameof(bitmap));
                }

                using var stream = new System.IO.MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                var imageData = stream.ToArray();
                
                return LoadImage(imageData);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "從 Bitmap 載入圖像失敗");
                throw;
            }
        }

        /// <summary>
        /// 檢測圖像的旋轉角度
        /// 使用 Hough Transform 檢測直線並計算角度
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <returns>檢測到的角度（度數）</returns>
        public double DetectAngle(Mat image)
        {
            try
            {
                if (image.Empty())
                {
                    throw new ArgumentException("輸入圖像不能為空", nameof(image));
                }

                using var gray = new Mat();
                using var edges = new Mat();

                // 轉為灰階
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

                // 高斯模糊去噪
                Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(5, 5), 0);

                // Canny 邊緣檢測
                Cv2.Canny(gray, edges, 50, 150, 3);

                // Hough 直線檢測
                var linesArray = Cv2.HoughLines(edges, 1, Math.PI / 180, 100);

                var angles = new List<double>();

                // 分析檢測到的直線
                foreach (var line in linesArray)
                {
                    float rho = line.Rho;
                    float theta = line.Theta;
                    
                    // 轉換為角度（度數）
                    double angle = theta * 180.0 / Math.PI;
                    
                    // 正規化角度到 [-45, 45] 範圍
                    if (angle > 90)
                        angle -= 180;
                    else if (angle < -90)
                        angle += 180;

                    // 過濾接近水平或垂直的線
                    if (Math.Abs(angle) < 45)
                    {
                        angles.Add(angle);
                    }
                }

                if (angles.Count == 0)
                {
                    Logger.Warn("未檢測到有效的直線，假設角度為 0");
                    return 0.0;
                }

                // 使用中位數作為最終角度
                angles.Sort();
                double detectedAngle = angles[angles.Count / 2];

                Logger.Info($"檢測到圖像角度: {detectedAngle:F2}°，基於 {angles.Count} 條直線");
                return detectedAngle;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "角度檢測失敗");
                return 0.0; // 返回預設角度
            }
        }

        /// <summary>
        /// 校正圖像角度
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <param name="angle">要校正的角度（度數）</param>
        /// <returns>校正後的圖像</returns>
        public Mat CorrectAngle(Mat image, double angle)
        {
            try
            {
                if (image.Empty())
                {
                    throw new ArgumentException("輸入圖像不能為空", nameof(image));
                }

                if (Math.Abs(angle) < 0.1) // 角度太小，不需要校正
                {
                    Logger.Debug("角度小於 0.1°，跳過校正");
                    return image.Clone();
                }

                // 計算旋轉中心和旋轉矩陣
                var center = new Point2f(image.Width / 2f, image.Height / 2f);
                var rotationMatrix = Cv2.GetRotationMatrix2D(center, -angle, 1.0);

                // 計算新的圖像邊界
                var corners = new Point2f[]
                {
                    new Point2f(0, 0),
                    new Point2f(image.Width, 0),
                    new Point2f(image.Width, image.Height),
                    new Point2f(0, image.Height)
                };

                var transformedCorners = corners.Select(corner =>
                {
                    var x = rotationMatrix.At<double>(0, 0) * corner.X + rotationMatrix.At<double>(0, 1) * corner.Y + rotationMatrix.At<double>(0, 2);
                    var y = rotationMatrix.At<double>(1, 0) * corner.X + rotationMatrix.At<double>(1, 1) * corner.Y + rotationMatrix.At<double>(1, 2);
                    return new Point2f((float)x, (float)y);
                }).ToArray();

                var minX = transformedCorners.Min(p => p.X);
                var maxX = transformedCorners.Max(p => p.X);
                var minY = transformedCorners.Min(p => p.Y);
                var maxY = transformedCorners.Max(p => p.Y);

                var newWidth = (int)(maxX - minX);
                var newHeight = (int)(maxY - minY);

                // 調整旋轉矩陣以確保所有內容都在新圖像內
                rotationMatrix.Set(0, 2, rotationMatrix.At<double>(0, 2) - minX);
                rotationMatrix.Set(1, 2, rotationMatrix.At<double>(1, 2) - minY);

                // 執行仿射變換
                var result = new Mat();
                Cv2.WarpAffine(image, result, rotationMatrix, new OpenCvSharp.Size(newWidth, newHeight), 
                              InterpolationFlags.Linear, BorderTypes.Constant, Scalar.White);

                Logger.Info($"完成角度校正: {angle:F2}°，新尺寸: {newWidth}x{newHeight}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"角度校正失敗，角度: {angle}°");
                return image.Clone(); // 返回原圖像的副本
            }
        }

        /// <summary>
        /// 使用 K-means 進行圖像區域聚類
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <param name="clusterCount">聚類數量</param>
        /// <returns>聚類後的圖像</returns>
        public Mat ApplyKMeansClustering(Mat image, int clusterCount = 3)
        {
            try
            {
                if (image.Empty())
                {
                    throw new ArgumentException("輸入圖像不能為空", nameof(image));
                }

                if (clusterCount < 2 || clusterCount > 8)
                {
                    throw new ArgumentException("聚類數量必須在 2-8 之間", nameof(clusterCount));
                }

                // 將圖像資料重新整理為 K-means 輸入格式
                using var data = new Mat();
                image.Reshape(1, image.Rows * image.Cols).ConvertTo(data, MatType.CV_32F);

                // K-means 聚類參數
                var labels = new Mat();
                var centers = new Mat();
                var criteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 10, 1.0);

                // 執行 K-means 聚類
                Cv2.Kmeans(data, clusterCount, labels, criteria, 3, KMeansFlags.RandomCenters, centers);

                // 重建聚類後的圖像
                var clusteredData = new Mat(data.Rows, data.Cols, MatType.CV_32F);
                for (int i = 0; i < data.Rows; i++)
                {
                    int clusterIdx = labels.At<int>(i);
                    if (image.Channels() == 1)
                    {
                        clusteredData.Set(i, 0, centers.At<float>(clusterIdx, 0));
                    }
                    else
                    {
                        for (int ch = 0; ch < image.Channels(); ch++)
                        {
                            clusteredData.Set(i, ch, centers.At<float>(clusterIdx, ch));
                        }
                    }
                }

                // 轉換回原始圖像格式
                var result = new Mat();
                clusteredData.ConvertTo(result, image.Type());
                result = result.Reshape(image.Channels(), image.Rows);

                Logger.Info($"完成 K-means 聚類，聚類數: {clusterCount}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"K-means 聚類失敗，聚類數: {clusterCount}");
                return image.Clone();
            }
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            // OpenCV Mat 物件通常使用 using 語句自動釋放
            // 這裡不需要特別的清理動作
            Logger.Debug("ImagePreprocessor 已釋放");
        }
    }
}
