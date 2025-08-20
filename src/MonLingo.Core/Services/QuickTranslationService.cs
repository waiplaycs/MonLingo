using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MonLingo.Core.Services;
using MonLingo.Core.View.Windows;
using MonLingo.Core.ViewModel;
using Point = System.Windows.Point;

namespace MonLingo.Core.Services
{
    /// <summary>
    /// 快速翻譯服務 - 整合截圖、OCR和翻譯功能
    /// </summary>
    public class QuickTranslationService
    {
        private readonly ScreenCaptureService _screenCapture;
        private readonly OcrService _ocrService;
        private readonly TranslateService _translateService;
        private SubtitleWindow _subtitleWindow;
        private readonly Window _mainBarWindow;

        public QuickTranslationService(Window mainBarWindow = null)
        {
            _screenCapture = new ScreenCaptureService();
            _ocrService = new OcrService();
            _translateService = new TranslateService();
            _mainBarWindow = mainBarWindow;
        }

        /// <summary>
        /// 開始快速翻譯流程
        /// </summary>
        public async Task StartQuickTranslationAsync()
        {
            try
            {
                // 第一步：顯示區域選擇器
                var regionSelector = new ScreenRegionSelector();
                var taskCompletionSource = new TaskCompletionSource<Rect?>();

                regionSelector.RegionSelected += (sender, region) =>
                {
                    taskCompletionSource.SetResult(region);
                };

                regionSelector.Closed += (sender, e) =>
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                        taskCompletionSource.SetResult(null);
                };

                regionSelector.Show();

                // 等待用戶選擇區域
                var selectedRegion = await taskCompletionSource.Task;

                if (selectedRegion == null)
                {
                    // 用戶取消了選擇
                    return;
                }

                // 第二步：截圖選中區域
                var region = selectedRegion.Value;
                var captureRect = new Rectangle(
                    (int)region.X,
                    (int)region.Y,
                    (int)region.Width,
                    (int)region.Height
                );

                var screenshot = _screenCapture.CaptureScreen(captureRect);
                if (screenshot == null)
                {
                    ShowError("截圖失敗，請重試。");
                    return;
                }

                // 第三步：使用新的字幕模式顯示翻譯結果
                System.Diagnostics.Debug.WriteLine("QuickTranslation: 開始創建字幕窗口");
                if (_subtitleWindow == null && _mainBarWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine("QuickTranslation: 使用主工具條引用創建字幕窗口");
                    _subtitleWindow = new SubtitleWindow(_mainBarWindow);
                }
                else if (_subtitleWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("QuickTranslation: 創建獨立字幕窗口");
                    _subtitleWindow = new SubtitleWindow();
                }

                // 顯示字幕窗口
                System.Diagnostics.Debug.WriteLine("QuickTranslation: 顯示字幕窗口");
                _subtitleWindow.Show();
                System.Diagnostics.Debug.WriteLine($"QuickTranslation: 字幕窗口可見性: {_subtitleWindow.IsVisible}");
                
                // 添加測試字幕行
                var testVm = _subtitleWindow.DataContext as SubtitleViewModel;
                testVm?.AddNewLine("測試", "字幕窗口已顯示");

                // 第四步：執行 OCR 識別
                System.Diagnostics.Debug.WriteLine("QuickTranslation: 開始 OCR 識別");
                var recognizedText = await _ocrService.RecognizeTextAsync(screenshot);
                System.Diagnostics.Debug.WriteLine($"QuickTranslation: OCR 結果: {recognizedText}");
                if (string.IsNullOrWhiteSpace(recognizedText))
                {
                    System.Diagnostics.Debug.WriteLine("QuickTranslation: OCR 無結果，顯示錯誤訊息");
                    var vm = _subtitleWindow.DataContext as SubtitleViewModel;
                    vm?.AddNewLine("未識別到文字", "無法識別圖片中的文字內容");
                    return;
                }

                // 第五步：執行翻譯
                System.Diagnostics.Debug.WriteLine("QuickTranslation: 開始翻譯");
                var translatedText = await _translateService.TranslateAsync(recognizedText, "auto", "zh-TW");
                System.Diagnostics.Debug.WriteLine($"QuickTranslation: 翻譯結果: {translatedText}");
                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    System.Diagnostics.Debug.WriteLine("QuickTranslation: 翻譯無結果，顯示錯誤訊息");
                    var vm = _subtitleWindow.DataContext as SubtitleViewModel;
                    vm?.AddNewLine(recognizedText, "翻譯失敗，請重試");
                    return;
                }

                // 第六步：顯示最終結果
                System.Diagnostics.Debug.WriteLine("QuickTranslation: 顯示最終翻譯結果");
                var viewModel = _subtitleWindow.DataContext as SubtitleViewModel;
                viewModel?.AddNewLine(recognizedText, translatedText);

                // 清理資源
                screenshot.Dispose();
            }
            catch (Exception ex)
            {
                ShowError($"快速翻譯過程中發生錯誤：{ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(message, "快速翻譯錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    /// <summary>
    /// 簡化的 OCR 服務 (用於測試)
    /// </summary>
    public partial class OcrService
    {
        public async Task<string> RecognizeTextAsync(Bitmap image)
        {
            // 模擬 OCR 處理時間
            await Task.Delay(1000);
            
            // 返回模擬的識別結果
            return "Sample text from image recognition";
        }
    }

    /// <summary>
    /// 簡化的翻譯服務 (用於測試)
    /// </summary>
    public partial class TranslateService
    {
        public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            // 模擬翻譯處理時間
            await Task.Delay(1500);
            
            // 返回模擬的翻譯結果
            return $"翻譯結果: {text}";
        }
    }
}
