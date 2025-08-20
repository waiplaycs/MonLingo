using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 臨時 OCR 測試實現 - 用於測試 OCR 管線而不依賴實際的 Native.dll
    /// </summary>
    public class MockOcrService : IOcrService
    {
        private bool _isInitialized = false;
        
        public bool IsInitialized => _isInitialized;
        
        public async Task<bool> InitializeAsync()
        {
            try
            {
                // 模擬初始化過程
                await Task.Delay(100);
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize OCR: {ex.Message}", ex);
            }
        }
        
        public async Task<OcrResult> RecognizeTextAsync(byte[] imageData, int width, int height)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("OCR not initialized. Call InitializeAsync first.");
            }
            
            try
            {
                // 模擬 OCR 處理時間
                await Task.Delay(200);
                
                // 創建模擬的 OCR 結果
                var mockResult = new OcrResult
                {
                    Text = "測試 OCR 識別文字\nMonLingo OCR 整合測試",
                    Confidence = 0.93,
                    BoundingBox = new Rectangle(10, 10, 250, 70),
                    Lines = new OcrLine[]
                    {
                        new OcrLine
                        {
                            Text = "測試 OCR 識別文字",
                            Confidence = 0.95,
                            BoundingBox = new Rectangle(10, 10, 200, 30),
                            Words = new OcrWord[]
                            {
                                new OcrWord { Text = "測試", Confidence = 0.96, BoundingBox = new Rectangle(10, 10, 40, 30) },
                                new OcrWord { Text = "OCR", Confidence = 0.94, BoundingBox = new Rectangle(55, 10, 60, 30) },
                                new OcrWord { Text = "識別", Confidence = 0.95, BoundingBox = new Rectangle(120, 10, 60, 30) },
                                new OcrWord { Text = "文字", Confidence = 0.96, BoundingBox = new Rectangle(185, 10, 60, 30) }
                            }
                        },
                        new OcrLine
                        {
                            Text = "MonLingo OCR 整合測試",
                            Confidence = 0.92,
                            BoundingBox = new Rectangle(10, 50, 250, 30),
                            Words = new OcrWord[]
                            {
                                new OcrWord { Text = "MonLingo", Confidence = 0.93, BoundingBox = new Rectangle(10, 50, 80, 30) },
                                new OcrWord { Text = "OCR", Confidence = 0.91, BoundingBox = new Rectangle(95, 50, 50, 30) },
                                new OcrWord { Text = "整合", Confidence = 0.92, BoundingBox = new Rectangle(150, 50, 50, 30) },
                                new OcrWord { Text = "測試", Confidence = 0.93, BoundingBox = new Rectangle(205, 50, 50, 30) }
                            }
                        }
                    }
                };
                
                return mockResult;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"OCR recognition failed: {ex.Message}", ex);
            }
        }
        
        public void Dispose()
        {
            try
            {
                if (_isInitialized)
                {
                    _isInitialized = false;
                }
            }
            catch (Exception ex)
            {
                // 記錄但不拋出異常
                Console.WriteLine($"Error disposing OCR service: {ex.Message}");
            }
        }
    }
}
