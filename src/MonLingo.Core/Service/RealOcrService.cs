using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PaddleOCRSharp;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 基於PaddleOCR的真實OCR服務實現
    /// 替換MockOcrService，連接真正的PaddleOCR引擎
    /// </summary>
    public class RealOcrService : IOcrService, IDisposable
    {
        private PaddleOCREngine _ocrEngine;
        private bool _isInitialized = false;
        private bool _disposed = false;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化PaddleOCR引擎 (async版本)
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) return true;

            try
            {
                return await Task.Run(() =>
                {
                    // 初始化PaddleOCR引擎，配置為CPU模式
                    var parameter = new OCRParameter
                    {
                        use_gpu = false,
                        cpu_math_library_num_threads = Environment.ProcessorCount,
                        enable_mkldnn = true,
                        det_db_thresh = 0.3f,
                        det_db_box_thresh = 0.5f,
                        det_db_unclip_ratio = 1.6f,
                        cls_thresh = 0.9f
                    };

                    _ocrEngine = new PaddleOCREngine(null, parameter);
                    _isInitialized = true;
                    
                    Console.WriteLine("✅ PaddleOCR引擎初始化成功");
                    return true;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PaddleOCR引擎初始化失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 識別圖片中的文字 (async版本)
        /// </summary>
        public async Task<OcrResult> RecognizeTextAsync(byte[] imageData, int width, int height)
        {
            if (!_isInitialized || _ocrEngine == null)
            {
                throw new InvalidOperationException("OCR not initialized. Call InitializeAsync first.");
            }

            try
            {
                return await Task.Run(() =>
                {
                    // 處理原始像素數據：來自Native.dll的是RGBA格式
                    using var bitmap = ConvertRawDataToBitmap(imageData, width, height);
                    
                    // 執行OCR識別
                    var ocrResult = _ocrEngine.DetectText(bitmap);
                    
                    // 轉換為我們的OcrResult格式
                    if (ocrResult?.TextBlocks != null && ocrResult.TextBlocks.Count > 0)
                    {
                        var lines = new OcrLine[ocrResult.TextBlocks.Count];
                        var allText = new System.Text.StringBuilder();
                        double totalConfidence = 0.0;
                        
                        for (int i = 0; i < ocrResult.TextBlocks.Count; i++)
                        {
                            var textBlock = ocrResult.TextBlocks[i];
                            
                            var boundingBox = new System.Drawing.Rectangle(
                                (int)textBlock.BoxPoints[0].X,
                                (int)textBlock.BoxPoints[0].Y,
                                (int)(textBlock.BoxPoints[2].X - textBlock.BoxPoints[0].X),
                                (int)(textBlock.BoxPoints[2].Y - textBlock.BoxPoints[0].Y)
                            );
                            
                            // 創建詞語數組
                            var words = new OcrWord[]
                            {
                                new OcrWord
                                {
                                    Text = textBlock.Text,
                                    Confidence = textBlock.Score,
                                    BoundingBox = boundingBox
                                }
                            };
                            
                            lines[i] = new OcrLine
                            {
                                Text = textBlock.Text,
                                Confidence = textBlock.Score,
                                BoundingBox = boundingBox,
                                Words = words
                            };
                            
                            allText.AppendLine(textBlock.Text);
                            totalConfidence += textBlock.Score;
                        }
                        
                        var result = new OcrResult
                        {
                            Text = allText.ToString().Trim(),
                            Confidence = totalConfidence / ocrResult.TextBlocks.Count,
                            Lines = lines,
                            BoundingBox = CalculateOverallBoundingBox(lines)
                        };
                        
                        Console.WriteLine($"🔍 OCR識別完成: 找到 {lines.Length} 行文字");
                        return result;
                    }
                    else
                    {
                        return new OcrResult 
                        { 
                            Text = string.Empty, 
                            Confidence = 0.0,
                            Lines = new OcrLine[0],
                            BoundingBox = new System.Drawing.Rectangle()
                        };
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OCR識別失敗: {ex.Message}");
                throw new InvalidOperationException($"OCR recognition failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 將原始RGBA數據轉換為Bitmap
        /// </summary>
        private Bitmap ConvertRawDataToBitmap(byte[] rawData, int width, int height)
        {
            Console.WriteLine($"[DEBUG] ConvertRawDataToBitmap: rawData.Length={rawData?.Length}, width={width}, height={height}");
            
            // 驗證參數
            if (width <= 0 || height <= 0)
            {
                Console.WriteLine($"[ERROR] Invalid dimensions: width={width}, height={height}");
                return new Bitmap(1, 1); // 返回最小的有效Bitmap
            }
            
            if (rawData == null || rawData.Length == 0)
            {
                Console.WriteLine("[DEBUG] Raw data is null or empty, creating blank bitmap");
                return new Bitmap(width, height);
            }
            
            // 檢查數據大小是否符合預期
            int expectedSize = width * height * 4; // RGBA = 4 bytes per pixel
            
            if (rawData.Length < expectedSize)
            {
                // 如果數據太小，可能是其他格式，嘗試作為圖片流處理
                try
                {
                    using var ms = new MemoryStream(rawData);
                    return new Bitmap(ms);
                }
                catch
                {
                    // 如果都失敗，創建一個空白圖片
                    Console.WriteLine($"⚠️ 圖片數據格式不符，創建空白圖片 ({width}x{height})");
                    return new Bitmap(width, height);
                }
            }

            // 創建Bitmap並複製像素數據
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                // 複製數據到Bitmap
                Marshal.Copy(rawData, 0, bitmapData.Scan0, Math.Min(rawData.Length, expectedSize));
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        /// <summary>
        /// 計算整體邊界框
        /// </summary>
        private System.Drawing.Rectangle CalculateOverallBoundingBox(OcrLine[] lines)
        {
            if (lines == null || lines.Length == 0)
                return new System.Drawing.Rectangle();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var line in lines)
            {
                var rect = line.BoundingBox;
                minX = Math.Min(minX, rect.X);
                minY = Math.Min(minY, rect.Y);
                maxX = Math.Max(maxX, rect.X + rect.Width);
                maxY = Math.Max(maxY, rect.Y + rect.Height);
            }

            return new System.Drawing.Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 釋放OCR引擎資源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (_ocrEngine != null)
                    {
                        _ocrEngine.Dispose();
                        _ocrEngine = null;
                        Console.WriteLine("🔄 PaddleOCR引擎資源已釋放");
                    }
                    _isInitialized = false;
                    _disposed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 釋放OCR資源時發生錯誤: {ex.Message}");
                }
            }
        }
    }
}
