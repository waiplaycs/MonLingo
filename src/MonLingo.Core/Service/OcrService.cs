using System;
using System.Threading.Tasks;
using System.Text;
using System.Drawing;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// OCR 服務實現
    /// 基於 PRD §2.3.1 Native.dll OCR API 設計
    /// </summary>
    public class OcrService : IOcrService
    {
        private bool _isInitialized = false;
        
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 初始化 OCR 引擎
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_isInitialized) return true;
                
                // 使用離線模式初始化 OCR
                var success = NativeBridge.ocr_init(fullOffline: true, timeStamp: 0);
                
                if (success)
                {
                    _isInitialized = true;
                }
                
                return success;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize OCR: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 識別圖像中的文字
        /// </summary>
        public async Task<OcrResult> RecognizeTextAsync(byte[] imageData, int width, int height)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("OCR service is not initialized");
            }
            
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data is null or empty");
            }
            
            try
            {
                // 調用 Native.dll 的 OCR 管線
                var resultPtr = NativeBridge.ocr_run_pipeline(imageData, imageData.Length, width, height);
                
                if (resultPtr == IntPtr.Zero)
                {
                    return new OcrResult { Text = string.Empty, Confidence = 0.0 };
                }
                
                try
                {
                    // 解析 OCR 結果
                    return await ParseOcrResultAsync(resultPtr);
                }
                finally
                {
                    // 釋放 Native 記憶體
                    NativeBridge.ocr_release_result(resultPtr);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"OCR recognition failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 解析 OCR 結果
        /// </summary>
        private async Task<OcrResult> ParseOcrResultAsync(IntPtr resultPtr)
        {
            var lineCount = NativeBridge.ocr_get_line_count(resultPtr);
            if (lineCount <= 0)
            {
                return new OcrResult { Text = string.Empty, Confidence = 0.0 };
            }
            
            var lines = new OcrLine[lineCount];
            var allText = new StringBuilder();
            double totalConfidence = 0.0;
            int totalWords = 0;
            
            for (int i = 0; i < lineCount; i++)
            {
                var lineContent = new StringBuilder(256);
                if (NativeBridge.ocr_get_line_content(resultPtr, i, lineContent, 256))
                {
                    var lineText = lineContent.ToString();
                    allText.AppendLine(lineText);
                    
                    // 獲取行邊界框
                    var quad8 = new int[8];
                    var hasBoundingBox = NativeBridge.ocr_get_line_bounding_box(resultPtr, i, quad8);
                    var boundingBox = hasBoundingBox ? 
                        new Rectangle(quad8[0], quad8[1], quad8[4] - quad8[0], quad8[5] - quad8[1]) : 
                        Rectangle.Empty;
                    
                    // 獲取行詳細資訊
                    var linePtr = NativeBridge.ocr_get_line(resultPtr, i);
                    var words = await ParseLineWordsAsync(linePtr);
                    
                    // 計算行置信度
                    double lineConfidence = 0.0;
                    if (words.Length > 0)
                    {
                        lineConfidence = CalculateAverageConfidence(words);
                        totalConfidence += lineConfidence * words.Length;
                        totalWords += words.Length;
                    }
                    
                    lines[i] = new OcrLine
                    {
                        Text = lineText,
                        Confidence = lineConfidence,
                        BoundingBox = boundingBox,
                        Words = words
                    };
                }
            }
            
            var averageConfidence = totalWords > 0 ? totalConfidence / totalWords : 0.0;
            
            return new OcrResult
            {
                Text = allText.ToString().Trim(),
                Confidence = averageConfidence,
                BoundingBox = CalculateOverallBoundingBox(lines),
                Lines = lines
            };
        }
        
        /// <summary>
        /// 解析行中的詞語
        /// </summary>
        private async Task<OcrWord[]> ParseLineWordsAsync(IntPtr linePtr)
        {
            if (linePtr == IntPtr.Zero) return new OcrWord[0];
            
            var wordCount = NativeBridge.ocr_get_line_word_count(linePtr);
            if (wordCount <= 0) return new OcrWord[0];
            
            var words = new OcrWord[wordCount];
            
            for (int i = 0; i < wordCount; i++)
            {
                var wordPtr = NativeBridge.ocr_get_line_word(linePtr, i);
                if (wordPtr == IntPtr.Zero) continue;
                
                // 獲取詞語內容
                var wordContent = new StringBuilder(64);
                var hasContent = NativeBridge.ocr_get_word_content(wordPtr, wordContent, 64);
                
                // 獲取詞語置信度
                var confidence = NativeBridge.ocr_get_word_confidence(wordPtr);
                
                // 獲取詞語邊界框
                var quad8 = new int[8];
                var hasBoundingBox = NativeBridge.ocr_get_word_bounding_box(wordPtr, quad8);
                var boundingBox = hasBoundingBox ? 
                    new Rectangle(quad8[0], quad8[1], quad8[4] - quad8[0], quad8[5] - quad8[1]) : 
                    Rectangle.Empty;
                
                words[i] = new OcrWord
                {
                    Text = hasContent ? wordContent.ToString() : string.Empty,
                    Confidence = confidence,
                    BoundingBox = boundingBox
                };
            }
            
            return words;
        }
        
        /// <summary>
        /// 計算平均置信度
        /// </summary>
        private double CalculateAverageConfidence(OcrWord[] words)
        {
            if (words == null || words.Length == 0) return 0.0;
            
            double total = 0.0;
            foreach (var word in words)
            {
                total += word.Confidence;
            }
            
            return total / words.Length;
        }
        
        /// <summary>
        /// 計算整體邊界框
        /// </summary>
        private Rectangle CalculateOverallBoundingBox(OcrLine[] lines)
        {
            if (lines == null || lines.Length == 0) return Rectangle.Empty;
            
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            
            foreach (var line in lines)
            {
                if (line.BoundingBox.IsEmpty) continue;
                
                minX = Math.Min(minX, line.BoundingBox.X);
                minY = Math.Min(minY, line.BoundingBox.Y);
                maxX = Math.Max(maxX, line.BoundingBox.Right);
                maxY = Math.Max(maxY, line.BoundingBox.Bottom);
            }
            
            if (minX == int.MaxValue) return Rectangle.Empty;
            
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
        
        /// <summary>
        /// 釋放 OCR 資源
        /// </summary>
        public void Dispose()
        {
            if (_isInitialized)
            {
                try
                {
                    NativeBridge.ocr_destroy();
                    _isInitialized = false;
                }
                catch (Exception ex)
                {
                    // 記錄錯誤但不拋出例外
                    System.Diagnostics.Debug.WriteLine($"OCR cleanup error: {ex.Message}");
                }
            }
        }
    }
}
