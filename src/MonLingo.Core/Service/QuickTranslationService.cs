using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MonLingo.Core.View.Windows;
using MonLingo.Core.Infrastructure;
using NLog;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 快速翻譯服務
    /// 負責處理螢幕截圖、OCR識別和翻譯的完整流程
    /// 實現完整的 OCR → 文字合併 → 翻譯 → 顯示流程
    /// </summary>
    public class QuickTranslationService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private CaptureRegionWindow _captureWindow;
        private SubtitleWindow _subtitleWindow;
        private Window _mainBarWindow;
        
        // 服務依賴 (延遲初始化)
        private IOcrService _ocrService;
        private ITranslateService _translateService;
        private IDisplayService _displayService;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="mainBarWindow">主工具條視窗</param>
        public QuickTranslationService(Window mainBarWindow)
        {
            Logger.Info("🚀 QuickTranslationService(Service) 建構函數開始");
            Logger.Debug($"📋 主視窗引用: {(mainBarWindow != null ? mainBarWindow.GetType().Name : "null")}");
            _mainBarWindow = mainBarWindow;
            
            // 🛑 延遲服務初始化，避免在建構函數中觸發自動測試
            // 服務將在第一次使用時才初始化
            Logger.Info("✅ QuickTranslationService(Service) 建構完成");
        }

        public async Task StartQuickTranslationAsync()
        {
            Logger.Info("🎯 StartQuickTranslationAsync(Service) 開始執行");
            Logger.Debug($"📋 服務狀態: _mainBarWindow={((_mainBarWindow == null) ? "null" : "已設定")}, _subtitleWindow={((_subtitleWindow == null) ? "null" : "已存在")}");
            
            try
            {
                // 🔧 首次使用時初始化服務
                Logger.Info("🔧 確保服務已初始化");
                EnsureServicesInitialized();
                
                // 步驟1: 顯示區域選擇視窗
                Logger.Info("📐 開始顯示區域選擇");
                await ShowRegionSelectionAsync();
                Logger.Info("✅ StartQuickTranslationAsync(Service) 執行完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ QuickTranslationService(Service).StartQuickTranslationAsync 發生異常");
                Logger.Error($"🔍 異常詳情: {ex.Message}");
                Logger.Error($"📍 異常堆疊: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"快速翻譯出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 確保服務已初始化（只在需要時才初始化）
        /// </summary>
        private void EnsureServicesInitialized()
        {
            if (_ocrService == null)
            {
                // 獲取服務實例
                _ocrService = Phase5ServiceContainer.GetService<IOcrService>();
                _translateService = Phase5ServiceContainer.GetService<ITranslateService>();
                _displayService = Phase5ServiceContainer.GetService<IDisplayService>();
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
            try
            {
                // 初始化 OCR 服務（如果需要）
                if (!_ocrService.IsInitialized)
                {
                    await _ocrService.InitializeAsync();
                }
                
                // 將 Bitmap 轉換為 byte[]
                byte[] imageData;
                using (var stream = new MemoryStream())
                {
                    image.Save(stream, ImageFormat.Png);
                    imageData = stream.ToArray();
                }
                
                // ============ PHASE 1: OCR 處理 ============
                var ocrResult = await _ocrService.RecognizeTextAsync(
                    imageData, image.Width, image.Height);
                
                if (ocrResult == null || ocrResult.Lines == null || ocrResult.Lines.Length == 0)
                {
                    return string.Empty; // 沒有識別到文字
                }
                
                // ============ PHASE 2: 【字幕模式特殊邏輯】文字合併 ============
                // 將零散的文字行合併成連貫的句子
                string mergedText = TextMerger.MergeForSubtitle(ocrResult);
                
                return mergedText;
            }
            catch (Exception ex)
            {
                throw new Exception($"OCR識別失敗: {ex.Message}", ex);
            }
        }

        private async Task<string> TranslateTextAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return string.Empty;
                }
                
                // 使用真正的翻譯服務
                // TODO: 從配置服務獲取語言設定
                var sourceLanguage = "auto"; // 自動檢測
                var targetLanguage = "zh-TW"; // 繁體中文
                
                var translatedText = await _translateService.TranslateAsync(
                    text, sourceLanguage, targetLanguage);
                
                return translatedText ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"翻譯失敗: {ex.Message}", ex);
            }
        }

        private void ShowTranslationResult(string originalText, string translatedText, Rect sourceRegion)
        {
            try
            {
                Logger.Debug("🎯 ShowTranslationResult 開始執行");
                Logger.Debug($"📝 原文: {originalText}");
                Logger.Debug($"🌐 譯文: {translatedText}");
                
                // ============ PHASE 4: 顯示結果分發 ============
                // 確保字幕視窗存在並設置 DisplayService
                if (_subtitleWindow == null)
                {
                    Logger.Debug("🆕 創建新的字幕視窗");
                    
                    try
                    {
                        Logger.Debug("🔨 開始創建 SubtitleWindow 實例");
                        _subtitleWindow = new SubtitleWindow(_mainBarWindow);
                        Logger.Debug("✅ SubtitleWindow 實例創建成功");
                        
                        // 設置 DisplayService 的 SubtitleViewModel 引用
                        if (_displayService != null)
                        {
                            Logger.Debug("🔗 設置 DisplayService 的 SubtitleViewModel 引用");
                            var viewModel = _subtitleWindow.DataContext as MonLingo.Core.ViewModel.SubtitleViewModel;
                            _displayService.SetSubtitleViewModel(viewModel);
                            Logger.Debug("✅ DisplayService 設置完成");
                        }
                        else
                        {
                            Logger.Debug("⚠️ DisplayService 為 null，跳過設置");
                        }
                        
                        Logger.Debug("🎬 開始調用 ShowSubtitle()");
                        _subtitleWindow.ShowSubtitle();
                        Logger.Debug("✅ 新字幕視窗已顯示");
                    }
                    catch (Exception createEx)
                    {
                        Logger.Error(createEx, "❌ 創建字幕視窗時發生錯誤");
                        throw new Exception($"創建字幕視窗失敗: {createEx.Message}", createEx);
                    }
                }
                else
                {
                    Logger.Debug($"♻️ 重複使用現有字幕視窗，當前狀態: Visibility={_subtitleWindow.Visibility}, IsVisible={_subtitleWindow.IsVisible}");
                    
                    // 如果字幕視窗已存在但被隱藏，重新顯示它
                    if (_subtitleWindow.Visibility == Visibility.Hidden || !_subtitleWindow.IsVisible)
                    {
                        Logger.Debug("🔄 重新顯示隱藏的字幕視窗");
                        _subtitleWindow.ShowSubtitle();
                        Logger.Debug($"✅ 字幕視窗重新顯示完成，新狀態: Visibility={_subtitleWindow.Visibility}, IsVisible={_subtitleWindow.IsVisible}");
                    }
                    else
                    {
                        Logger.Debug("ℹ️ 字幕視窗已經是顯示狀態，無需重新顯示");
                    }
                }

                // 使用 DisplayService 顯示翻譯結果
                Logger.Debug("📊 檢查 DisplayService 狀態");
                if (_displayService != null)
                {
                    Logger.Debug("✅ 使用 DisplayService 顯示翻譯結果");
                    _displayService.Show(originalText, translatedText);
                    Logger.Debug("✅ DisplayService.Show() 調用完成");
                }
                else
                {
                    Logger.Debug("🔄 DisplayService 不可用，直接調用字幕視窗");
                    _subtitleWindow.AddSubtitleLine(originalText, translatedText);
                    Logger.Debug("✅ AddSubtitleLine() 調用完成");
                }
                
                Logger.Debug("🎊 ShowTranslationResult 執行完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ ShowTranslationResult 執行時發生錯誤");
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
