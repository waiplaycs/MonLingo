using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MonLingo.Core.View.Windows;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Events;
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
        private OcrDebugOverlay _ocrDebugOverlay; // 新增OCR調試覆蓋層
        private Rect _currentSelectedRegion; // 保存當前選中的區域座標
        
        // 服務依賴 (延遲初始化)
        private IOcrService _ocrService;
        private ITranslateService _translateService;
        private IDisplayService _displayService;
        private ILanguageConfigService _languageConfigService;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="mainBarWindow">主工具條視窗</param>
        public QuickTranslationService(Window mainBarWindow)
        {
            Logger.Info("🚀 QuickTranslationService(Service) 建構函數開始");
            Logger.Debug($"📋 主視窗引用: {(mainBarWindow != null ? mainBarWindow.GetType().Name : "null")}");
            _mainBarWindow = mainBarWindow;
            
            // 訂閱OCR調試事件
            OcrDebugEvents.HideDebugOverlay += HideOcrDebugInfo;
            
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
                
                // 在回合開始時清空舊輸出（只清一次）
                if (_subtitleWindow == null)
                {
                    Logger.Debug("🆕 [Round] 創建字幕視窗以顯示結果（預先，用於一鍵截圖回合清理）");
                    _subtitleWindow = new SubtitleWindow(_mainBarWindow);
                    if (_displayService != null)
                    {
                        var vm = _subtitleWindow.DataContext as MonLingo.Core.ViewModel.SubtitleViewModel;
                        _displayService.SetSubtitleViewModel(vm);
                    }
                    _subtitleWindow.ShowSubtitle();
                }
                if (_displayService != null)
                {
                    Logger.Info("[Round] StartNewRound via DisplayService (Quick)");
                    _displayService.StartNewRound();
                }
                else
                {
                    Logger.Info("[Round] ClearSubtitles via SubtitleWindow (Quick)");
                    _subtitleWindow.ClearSubtitles();
                }
                
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
        /// 針對已存在的選擇框區域，直接進行 OCR → 翻譯 → 字幕顯示（不再彈出選擇視窗）。
        /// </summary>
        /// <param name="regions">一組相對虛擬桌面座標的區域</param>
    public async Task StartRegionTranslationAsync(System.Collections.Generic.IEnumerable<System.Windows.Rect> regions)
        {
            Logger.Info("🎯 StartRegionTranslationAsync(Service) 開始執行");
            try
            {
                if (regions == null)
                {
                    Logger.Warn("StartRegionTranslationAsync 收到空的 regions");
                    return;
                }

                // 初始化服務
                EnsureServicesInitialized();

                // 先確保字幕視窗存在（共用快速翻譯的字幕視窗）
                if (_subtitleWindow == null)
                {
                    Logger.Debug("🆕 創建字幕視窗以顯示結果（預先）");
                    _subtitleWindow = new SubtitleWindow(_mainBarWindow);
                    if (_displayService != null)
                    {
                        var vm = _subtitleWindow.DataContext as MonLingo.Core.ViewModel.SubtitleViewModel;
                        _displayService.SetSubtitleViewModel(vm);
                    }
                    _subtitleWindow.ShowSubtitle();
                }

                // 在回合開始時清空舊輸出（只清一次）
                if (_displayService != null)
                {
                    Logger.Info("[Round] StartNewRound via DisplayService (Regions)");
                    _displayService.StartNewRound();
                }
                else
                {
                    Logger.Info("[Round] ClearSubtitles via SubtitleWindow (Regions)");
                    _subtitleWindow.ClearSubtitles();
                }

                var regionList = new System.Collections.Generic.List<System.Windows.Rect>(regions);
                Logger.Info($"[Regions] 本回合共 {regionList.Count} 個區域");
                for (int i = 0; i < regionList.Count; i++)
                {
                    var region = regionList[i];
                    try
                    {
                        Logger.Info($"[Regions] 開始處理第 {i+1}/{regionList.Count} 個區域: X={region.X}, Y={region.Y}, W={region.Width}, H={region.Height}");
                        await ProcessSelectedRegionAsync(region);
                        Logger.Info($"[Regions] 完成處理第 {i+1}/{regionList.Count} 個區域");
                    }
                    catch (Exception exOne)
                    {
                        Logger.Error(exOne, $"區域處理失敗: {region}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ StartRegionTranslationAsync 發生異常");
                System.Windows.MessageBox.Show($"區域翻譯出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _languageConfigService = Phase5ServiceContainer.GetService<ILanguageConfigService>();
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
                // 保存選中區域座標供調試使用
                _currentSelectedRegion = selectedRegion;
                
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
                // 注意：region.X/Y 是相對虛擬桌面的座標（可能含負值），需加上 VirtualScreenLeft/Top 再縮放
                var scaledRegion = new Rectangle(
                    (int)((region.X + SystemParameters.VirtualScreenLeft) * dpiScale),
                    (int)((region.Y + SystemParameters.VirtualScreenTop) * dpiScale),
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
                // 🎯 優先使用OCR服務的語言配置功能，自動記住用戶語言設定
                OcrResult ocrResult;
                
                if (_ocrService is RealOcrService configAwareOcrService)
                {
                    // 使用配置感知的OCR方法，自動記住語言設定
                    ocrResult = await configAwareOcrService.RecognizeTextWithConfigAsync(
                        imageData, image.Width, image.Height);
                }
                else
                {
                    // 後備方案：使用原有的OCR方法
                    ocrResult = await _ocrService.RecognizeTextAsync(
                        imageData, image.Width, image.Height);
                }
                
                if (ocrResult == null || ocrResult.Lines == null || ocrResult.Lines.Length == 0)
                {
                    return string.Empty; // 沒有識別到文字
                }

                // 🔍 調試功能：顯示OCR識別框
                ShowOcrDebugInfo(ocrResult);
                
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
                
                // 🎯 優先使用翻譯服務的語言配置功能，自動記住用戶語言設定
                string translatedText;
                
                if (_translateService is TranslateService configAwareService)
                {
                    // 使用配置感知的翻譯方法，自動記住語言設定
                    translatedText = await configAwareService.TranslateWithConfigAsync(text);
                }
                else
                {
                    // 後備方案：手動獲取語言配置
                    var sourceLanguage = await _languageConfigService.GetSourceLanguageAsync();
                    var targetLanguage = await _languageConfigService.GetTargetLanguageAsync();
                    translatedText = await _translateService.TranslateAsync(text, sourceLanguage, targetLanguage);
                }
                
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
                    Logger.Debug("✅ 使用 DisplayService 顯示翻譯結果 (SubtitleMode)");
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

        /// <summary>
        /// 直接處理選定區域的公共方法 (供工具條按鈕使用)
        /// </summary>
        /// <param name="selectedRegion">選定的區域</param>
        public async Task ProcessSelectedRegionDirectAsync(Rect selectedRegion)
        {
            Logger.Info($"🎯 ProcessSelectedRegionDirectAsync 開始處理區域: {selectedRegion}");
            
            // 確保服務已初始化
            EnsureServicesInitialized();
            
            // 調用內部處理方法
            await ProcessSelectedRegionAsync(selectedRegion);
            
            Logger.Info("✅ ProcessSelectedRegionDirectAsync 處理完成");
        }

        public void Dispose()
        {
            // 取消訂閱事件
            OcrDebugEvents.HideDebugOverlay -= HideOcrDebugInfo;
            
            _captureWindow?.Close();
            _subtitleWindow?.Close();
            _ocrDebugOverlay?.Close(); // 關閉調試覆蓋層
        }

        #region OCR 調試功能

        /// <summary>
        /// 顯示OCR調試信息
        /// </summary>
        /// <param name="ocrResult">OCR識別結果</param>
        private void ShowOcrDebugInfo(OcrResult ocrResult)
        {
            try
            {
                Logger.Info("🔍 顯示OCR調試可視化");
                
                // 創建調試覆蓋層（如果尚未創建）
                if (_ocrDebugOverlay == null)
                {
                    _ocrDebugOverlay = new OcrDebugOverlay();
                    Logger.Debug("📱 創建新的OCR調試覆蓋層");
                }

                // 計算座標轉換參數
                var dpiScale = GetDpiScale();
                var coordinateTransform = new CoordinateTransform
                {
                    SelectedRegion = _currentSelectedRegion,
                    DpiScale = dpiScale,
                    VirtualScreenLeft = SystemParameters.VirtualScreenLeft,
                    VirtualScreenTop = SystemParameters.VirtualScreenTop
                };

                // 顯示調試信息，傳遞座標轉換參數
                _ocrDebugOverlay.ShowOcrDebugInfo(ocrResult, coordinateTransform);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "顯示OCR調試信息時發生錯誤");
            }
        }

        /// <summary>
        /// 隱藏OCR調試信息
        /// 當字幕視窗關閉時調用
        /// </summary>
        public void HideOcrDebugInfo()
        {
            try
            {
                Logger.Info("🙈 隱藏OCR調試可視化");
                _ocrDebugOverlay?.HideDebugOverlay();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "隱藏OCR調試信息時發生錯誤");
            }
        }

        #endregion
    }
}
