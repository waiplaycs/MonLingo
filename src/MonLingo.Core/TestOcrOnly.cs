using System;
using System.IO;
using System.Threading.Tasks;
using MonLingo.Core.Service;
using System.Drawing;
using System.Drawing.Imaging;

namespace MonLingo.Core
{
    public class TestOcrOnly
    {
        [STAThread]
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== PaddleOCR 測試開始 ===");
                
                var ocrService = new RealOcrService();
                
                // 初始化 OCR 服務
                Console.WriteLine("正在初始化 OCR 服務...");
                await ocrService.InitializeAsync();
                Console.WriteLine("✓ OCR 服務初始化完成");
                
                // 創建一個簡單的測試圖片（白底黑字）
                Console.WriteLine("正在創建測試圖片...");
                int imageWidth = 400;
                int imageHeight = 100;
                var testImageBytes = CreateTestImage(imageWidth, imageHeight);
                Console.WriteLine($"✓ 測試圖片創建完成，大小: {testImageBytes.Length} bytes");
                
                // 執行 OCR 識別
                Console.WriteLine("正在執行 OCR 識別...");
                var result = await ocrService.RecognizeTextAsync(testImageBytes, imageWidth, imageHeight);
                
                Console.WriteLine($"✓ OCR 識別完成！");
                Console.WriteLine($"整體文字: '{result.Text}'");
                Console.WriteLine($"信心度: {result.Confidence:F2}");
                Console.WriteLine($"邊界框: ({result.BoundingBox.X}, {result.BoundingBox.Y}, {result.BoundingBox.Width}, {result.BoundingBox.Height})");
                
                if (result.Lines != null && result.Lines.Length > 0)
                {
                    Console.WriteLine($"行數: {result.Lines.Length}");
                    for (int i = 0; i < result.Lines.Length; i++)
                    {
                        var line = result.Lines[i];
                        Console.WriteLine($"行 {i + 1}: '{line.Text}' (信心度: {line.Confidence:F2})");
                        
                        if (line.Words != null && line.Words.Length > 0)
                        {
                            for (int j = 0; j < line.Words.Length; j++)
                            {
                                var word = line.Words[j];
                                Console.WriteLine($"  詞 {j + 1}: '{word.Text}' (信心度: {word.Confidence:F2})");
                            }
                        }
                    }
                }
                
                Console.WriteLine("=== OCR 測試成功完成！===");
                ocrService.Dispose();
                Console.WriteLine("OCR 服務已清理");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OCR 測試失敗: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }
            
            Console.WriteLine("按任意鍵退出...");
            Console.ReadKey();
        }
        
        private static byte[] CreateTestImage(int width, int height)
        {
            // 創建一個簡單的測試圖片，包含 "Hello World" 文字
            using (var bitmap = new Bitmap(width, height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // 白色背景
                graphics.Clear(Color.White);
                
                // 黑色文字
                using (var font = new Font("Arial", 24, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Black))
                {
                    graphics.DrawString("Hello World", font, brush, 50, 30);
                }
                
                // 轉換為 byte array
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }
    }
}
