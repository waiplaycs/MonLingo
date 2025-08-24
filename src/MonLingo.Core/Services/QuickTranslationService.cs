using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MonLingo.Core.Services;
using MonLingo.Core.View.Windows;
using MonLingo.Core.ViewModel;
using Point = System.Windows.Point;
using NLog;

namespace MonLingo.Core.Services
{
    /// <summary>
    /// 快速翻譯服務 - 整合截圖、OCR和翻譯功能
    /// </summary>
    public class QuickTranslationService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ScreenCaptureService _screenCapture;
        private readonly OcrService _ocrService;
        private readonly TranslateService _translateService;
        private SubtitleWindow _subtitleWindow;
        private readonly Window _mainBarWindow;

        public QuickTranslationService(Window mainBarWindow = null)
        {
            Logger.Info("🚀 QuickTranslationService 建構函數開始");
            Logger.Debug($"📋 主視窗引用: {(mainBarWindow != null ? mainBarWindow.GetType().Name : "null")}");
            
            _screenCapture = new ScreenCaptureService();
            _ocrService = new OcrService();
            _translateService = new TranslateService();
            _mainBarWindow = mainBarWindow;
            
            Logger.Info("✅ QuickTranslationService 建構完成");
        }

        /// <summary>
        /// 開始快速翻譯流程
        /// </summary>
        public async Task StartQuickTranslationAsync()
        {
            Logger.Info("🎯 StartQuickTranslationAsync 開始執行");
            Logger.Debug($"📋 服務狀態: _mainBarWindow={((_mainBarWindow == null) ? "null" : "已設定")}, _subtitleWindow={((_subtitleWindow == null) ? "null" : "已存在")}");
            
            try
            {
                // 第一步：顯示區域選擇器
                Logger.Info("📐 創建區域選擇器");
                var regionSelector = new ScreenRegionSelector();
                var taskCompletionSource = new TaskCompletionSource<Rect?>();

                regionSelector.RegionSelected += (sender, region) =>
                {
                    Logger.Info($"✅ 用戶選擇了區域: {region}");
                    taskCompletionSource.SetResult(region);
                };

                regionSelector.Closed += (sender, e) =>
                {
                    Logger.Info("❌ 區域選擇器被關閉");
                    if (!taskCompletionSource.Task.IsCompleted)
                        taskCompletionSource.SetResult(null);
                };

                Logger.Info("🖱️ 顯示區域選擇器");
                regionSelector.Show();

                // 等待用戶選擇區域
                Logger.Info("⏳ 等待用戶選擇區域");
                var selectedRegion = await taskCompletionSource.Task;

                if (selectedRegion == null)
                {
                    Logger.Info("🚫 用戶取消了區域選擇，退出快速翻譯");
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
                    Logger.Error("❌ 截圖失敗");
                    ShowError("截圖失敗，請重試。");
                    return;
                }
                Logger.Info("✅ 截圖成功完成");

                // 第三步：使用新的字幕模式顯示翻譯結果
                Logger.Info("📺 開始處理字幕視窗顯示");
                Logger.Debug($"🔍 字幕視窗狀態檢查: _subtitleWindow={((_subtitleWindow == null) ? "null" : "已存在")}, _mainBarWindow={((_mainBarWindow == null) ? "null" : _mainBarWindow.GetType().Name)}");
                
                if (_subtitleWindow == null && _mainBarWindow != null)
                {
                    Logger.Info("🔨 使用主工具條引用創建字幕視窗");
                    _subtitleWindow = new SubtitleWindow(_mainBarWindow);
                    Logger.Debug($"✅ 字幕視窗創建完成: {_subtitleWindow.GetType().Name}");
                }
                else if (_subtitleWindow == null)
                {
                    Logger.Info("🔨 創建獨立字幕視窗");
                    _subtitleWindow = new SubtitleWindow();
                    Logger.Debug($"✅ 獨立字幕視窗創建完成: {_subtitleWindow.GetType().Name}");
                }
                else
                {
                    Logger.Info("♻️ 重複使用現有字幕視窗");
                }

                // 確保字幕窗口可見 - 處理之前被隱藏的情況
                Logger.Info("👁️ 確保字幕視窗可見性");
                Logger.Debug($"📊 視窗狀態檢查: IsVisible={_subtitleWindow.IsVisible}, Visibility={_subtitleWindow.Visibility}, WindowState={_subtitleWindow.WindowState}");
                
                if (!_subtitleWindow.IsVisible)
                {
                    Logger.Info("🎬 執行 Show() 顯示字幕視窗");
                    _subtitleWindow.Show();
                }
                if (_subtitleWindow.Visibility != Visibility.Visible)
                {
                    Logger.Info("🔄 設定 Visibility = Visible");
                    _subtitleWindow.Visibility = Visibility.Visible;
                }
                
                // 將視窗置前
                Logger.Debug("⬆️ 執行 Activate() 將視窗置前");
                _subtitleWindow.Activate();
                Logger.Info($"📊 最終視窗狀態: IsVisible={_subtitleWindow.IsVisible}, Visibility={_subtitleWindow.Visibility}");

                // 顯示後立即清空舊內容
                Logger.Debug("🧹 清空字幕視窗舊內容");
                _subtitleWindow.ClearSubtitles();

                // 第四步：執行 OCR 識別
                Logger.Info("🔍 開始 OCR 文字識別");
                var recognizedText = await _ocrService.RecognizeTextAsync(screenshot);
                Logger.Info($"📝 OCR 識別結果: {recognizedText}");
                if (string.IsNullOrWhiteSpace(recognizedText))
                {
                    Logger.Warn("⚠️ OCR 無法識別文字內容");
                    var vm = _subtitleWindow.DataContext as SubtitleViewModel;
                    vm?.AddNewLine("未識別到文字", "無法識別圖片中的文字內容");
                    return;
                }

                // 第五步：執行翻譯
                Logger.Info("🌐 開始翻譯文字");
                var translatedText = await _translateService.TranslateAsync(recognizedText, "auto", "zh-TW");
                Logger.Info($"✅ 翻譯結果: {translatedText}");
                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    Logger.Warn("⚠️ 翻譯服務未返回結果");
                    var vm = _subtitleWindow.DataContext as SubtitleViewModel;
                    vm?.AddNewLine(recognizedText, "翻譯失敗，請重試");
                    return;
                }

                // 第六步：顯示最終結果
                Logger.Info("🎭 在字幕視窗中顯示翻譯結果");
                var viewModel = _subtitleWindow.DataContext as SubtitleViewModel;
                viewModel?.AddNewLine(recognizedText, translatedText);

                // 清理資源
                Logger.Debug("🧹 清理截圖資源");
                screenshot.Dispose();
                Logger.Info("✅ QuickTranslationService 執行完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ QuickTranslationService.StartQuickTranslationAsync 發生異常");
                Logger.Error($"🔍 異常詳情: {ex.Message}");
                Logger.Error($"📍 異常堆疊: {ex.StackTrace}");
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
