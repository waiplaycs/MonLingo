using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MonLingo.Core.View.Windows;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 快速翻譯服務
    /// 負責處理螢幕截圖、OCR識別和翻譯的完整流程
    /// </summary>
    public class QuickTranslationService
    {
        private CaptureRegionWindow _captureWindow;
        private SubtitleWindow _subtitleWindow;
        private Window _mainBarWindow;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="mainBarWindow">主工具條視窗</param>
        public QuickTranslationService(Window mainBarWindow)
        {
            _mainBarWindow = mainBarWindow;
        }

        public async Task StartQuickTranslationAsync()
        {
            try
            {
                // 步驟1: 顯示區域選擇視窗
                await ShowRegionSelectionAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"快速翻譯出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Task ShowRegionSelectionAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            bool isCompleted = false;

            // 創建區域選擇視窗
            _captureWindow = new CaptureRegionWindow();
            
            // 訂閱區域選擇事件
            _captureWindow.RegionSelected += async (sender, selectedRegion) =>
            {
                if (isCompleted) return;
                isCompleted = true;

                try
                {
                    await ProcessSelectedRegionAsync(selectedRegion);
                    if (!tcs.Task.IsCompleted)
                    {
                        tcs.SetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    if (!tcs.Task.IsCompleted)
                    {
                        tcs.SetException(ex);
                    }
                }
            };

            // 視窗關閉事件
            _captureWindow.Closed += (sender, e) =>
            {
                if (!isCompleted && !tcs.Task.IsCompleted)
                {
                    tcs.SetResult(false);
                }
            };

            // 顯示視窗
            _captureWindow.Show();

            return tcs.Task;
        }

        private async Task ProcessSelectedRegionAsync(Rect selectedRegion)
        {
            try
            {
                // 步驟2: 截圖選擇的區域
                var screenshot = CaptureScreenRegion(selectedRegion);
                
                // 步驟3: OCR識別文字
                var recognizedText = await PerformOcrAsync(screenshot);
                
                // 步驟4: 翻譯文字
                var translatedText = await TranslateTextAsync(recognizedText);
                
                // 步驟5: 顯示結果視窗
                ShowTranslationResult(recognizedText, translatedText, selectedRegion);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"處理選擇區域時出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Bitmap CaptureScreenRegion(Rect region)
        {
            try
            {
                // 獲取螢幕DPI縮放比例
                var dpiScale = GetDpiScale();
                
                // 調整區域座標以適應DPI縮放
                var scaledRegion = new Rectangle(
                    (int)(region.X * dpiScale),
                    (int)(region.Y * dpiScale),
                    (int)(region.Width * dpiScale),
                    (int)(region.Height * dpiScale)
                );

                // 截圖
                var bitmap = new Bitmap(scaledRegion.Width, scaledRegion.Height, PixelFormat.Format24bppRgb);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(scaledRegion.Location, System.Drawing.Point.Empty, scaledRegion.Size);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"截圖失敗: {ex.Message}");
            }
        }

        private double GetDpiScale()
        {
            // 獲取系統DPI設定
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.DpiX / 96.0; // 96 DPI是100%縮放
            }
        }

        private async Task<string> PerformOcrAsync(Bitmap image)
        {
            // 暫時模擬OCR處理
            await Task.Delay(1000); // 模擬處理時間
            
            // TODO: 整合真正的OCR引擎 (PaddleOCR)
            // 這裡先返回模擬結果
            return "This is a sample text recognition result. OCR engine integration is needed.";
        }

        private async Task<string> TranslateTextAsync(string text)
        {
            // 暫時模擬翻譯處理
            await Task.Delay(800); // 模擬翻譯時間
            
            // TODO: 整合真正的翻譯服務
            // 這裡先返回模擬結果
            if (text.Contains("sample"))
            {
                return "這是一個範例文字識別結果。需要整合OCR引擎。";
            }
            return $"[翻譯結果] {text}";
        }

        private void ShowTranslationResult(string originalText, string translatedText, Rect sourceRegion)
        {
            try
            {
                // 確保字幕視窗存在
                if (_subtitleWindow == null)
                {
                    _subtitleWindow = new SubtitleWindow(_mainBarWindow);
                    _subtitleWindow.ShowSubtitle();
                }

                // 在字幕視窗中顯示翻譯結果
                _subtitleWindow.AddSubtitleLine(originalText, translatedText);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"顯示翻譯結果時出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            _captureWindow?.Close();
            _subtitleWindow?.Close();
        }
    }
}
