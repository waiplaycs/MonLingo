using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MonLingo.Core.View.Windows;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Events;
using MonLingo.Core.Model;
using MonLingo.Core.Service.AI;
using MonLingo.ViewModel;
using NLog;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 快速翻譯服務
    /// 負責處理螢幕截圖、OCR識別和翻譯的完整流程
    /// 實現完整的文字合併 → 翻譯 → 顯示流程
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
        private IAITranslationService _aiTranslationService; // AI翻譯服務

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
            
            // 添加字幕視窗狀態調試
            if (_subtitleWindow != null)
            {
                Logger.Debug($"🔍 字幕視窗已存在狀態: Visibility={_subtitleWindow.Visibility}, IsVisible={_subtitleWindow.IsVisible}");
            }
            
            try
            {
                // 🔧 首次使用時初始化服務
                Logger.Info("🔧 確保服務已初始化");
                EnsureServicesInitialized();
                
                // 清理現有的字幕視窗內容（如果存在），但不在此階段顯示視窗
                if (_subtitleWindow != null)
                {
                    Logger.Debug("🧹 清理現有字幕視窗內容，但不顯示視窗");
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
                    // 確保隱藏字幕視窗
                    if (_subtitleWindow.Visibility == Visibility.Visible)
                    {
                        Logger.Debug("❌ 隱藏字幕視窗（框選階段不應顯示）");
                        _subtitleWindow.Hide();
                    }
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
        /// 確保服務已初始化(只在需要時才初始化)
        /// </summary>
        private void EnsureServicesInitialized()
        {
            if (_ocrService == null)
            {
                Logger.Info("[QuickTranslationService] 初始化服務...");
                // 獲取服務實例
                _ocrService = Phase5ServiceContainer.GetService<IOcrService>();
                _translateService = Phase5ServiceContainer.GetService<ITranslateService>();
                _displayService = Phase5ServiceContainer.GetService<IDisplayService>();
                _languageConfigService = Phase5ServiceContainer.GetService<ILanguageConfigService>();
                _aiTranslationService = Phase5ServiceContainer.GetService<IAITranslationService>();
                
                Logger.Info($"[QuickTranslationService] DisplayService 獲取完成: {_displayService != null}");
                Logger.Info($"[QuickTranslationService] AITranslationService 獲取完成: {_aiTranslationService != null}");
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
                Logger.Info("*** 🎯 ProcessSelectedRegionAsync 開始執行 ***");
                Logger.Info($"*** 選中區域: {selectedRegion} ***");
                
                // 保存選中區域座標供調試使用
                _currentSelectedRegion = selectedRegion;
                
                // 步驟2: 截圖選擇的區域
                Logger.Info("*** 📸 開始截圖 ***");
                var screenshot = CaptureScreenRegion(selectedRegion);
                Logger.Info("*** ✅ 截圖完成 ***");
                
                // 步驟3: OCR識別文字 (同時獲取OCR結果)
                Logger.Info("*** 🔍 開始OCR識別 ***");
                var (recognizedText, ocrResult) = await PerformOcrWithResultAsync(screenshot);
                Logger.Info($"*** ✅ OCR完成，識別文字: {recognizedText} ***");
                
                // 步驟4: 翻譯文字
                Logger.Info("*** 🌐 開始翻譯 ***");
                var translatedText = await TranslateTextAsync(recognizedText);
                Logger.Info($"*** ✅ 翻譯完成，譯文: {translatedText} ***");
                
                // 步驟5: 顯示結果視窗 (傳遞OCR結果)
                Logger.Info("*** 📺 開始顯示結果 ***");
                ShowTranslationResult(recognizedText, translatedText, selectedRegion, ocrResult);
                Logger.Info("*** ✅ ProcessSelectedRegionAsync 執行完成 ***");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "*** ❌ ProcessSelectedRegionAsync 發生異常 ***");
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
            var (text, _) = await PerformOcrWithResultAsync(image);
            return text;
        }

        private async Task<(string recognizedText, OcrResult ocrResult)> PerformOcrWithResultAsync(Bitmap image)
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
                    return (string.Empty, null); // 沒有識別到文字
                }

                // ============ PHASE 2: AI驅動的智能版面分析與翻譯 ============
                // 📝 階段一：橫向行合併
                // 📝 階段二：智能分欄
                // 🤖 AI翻譯層：段落合併 + 智能翻譯
                
                var startMsg = "🚀🚀🚀 開始執行版面分析V2 (橫向合併 + 智能分欄) 🚀🚀🚀";
                Logger.Info(startMsg);
                Console.WriteLine(startMsg);
                
                var inputMsg = $"📥 OCR輸入：{ocrResult.Lines.Length} 個文字行";
                Logger.Info(inputMsg);
                Console.WriteLine(inputMsg);
                
                var layoutAnalysis = new LayoutAnalysisService();
                
                // 使用簡化版面分析(僅階段一+二)
                var layoutResultV2 = layoutAnalysis.AnalyzeLayoutV2(ocrResult);
                
                var resultMsg = $"📤 版面分析V2結果：Success={layoutResultV2.Success}, Columns={layoutResultV2.Columns?.Count ?? 0}";
                Logger.Info(resultMsg);
                Console.WriteLine(resultMsg);
                
                if (layoutResultV2.Success && layoutResultV2.Columns != null && layoutResultV2.Columns.Count > 0)
                {
                    var completeMsg = $"✅✅✅ 版面分析V2完成：耗時 {layoutResultV2.ProcessingTimeMs:F1}ms，檢測到 {layoutResultV2.Columns.Count} 個欄位";
                    Logger.Info(completeMsg);
                    Console.WriteLine(completeMsg);
                    
                    // 🎨 立即顯示調試視覺化 (在AI翻譯之前)
                    var debugMsg1 = "🎨 準備顯示版面分析調試視覺化...";
                    Logger.Info(debugMsg1);
                    Console.WriteLine(debugMsg1);
                    ShowLayoutAnalysisDebugInfoV2(ocrResult, layoutResultV2);
                    var debugMsg2 = "🎨 調試視覺化已觸發";
                    Logger.Info(debugMsg2);
                    Console.WriteLine(debugMsg2);
                    
                    // 🤖 使用AI翻譯服務進行智能段落合併和翻譯
                    if (_aiTranslationService != null)
                    {
                        try
                        {
                            // 獲取目標語言配置
                            var targetLanguage = await _languageConfigService.GetTargetLanguageAsync();
                            
                            var aiMsg = $"🤖 開始AI智能翻譯，目標語言: {targetLanguage}";
                            Logger.Info(aiMsg);
                            Console.WriteLine(aiMsg);
                            
                            // 使用多欄位一次性翻譯(新方法)
                            var columnLines = layoutResultV2.Columns
                                .Select(col => col.Lines)
                                .ToList();
                            
                            var aiResults = await _aiTranslationService.SmartTranslateMultiColumnAsync(
                                columnLines,
                                sourceLanguage: "auto",
                                targetLanguage: targetLanguage
                            );
                            
                            // 合併所有欄位的翻譯結果,保持欄位分隔
                            var columnOutputs = new List<string>();
                            
                            for (int i = 0; i < aiResults.Count; i++)
                            {
                                var result = aiResults[i];
                                if (result.Success && result.Paragraphs != null && result.Paragraphs.Count > 0)
                                {
                                    // 為每個欄位構建獨立的輸出
                                    var outputLines = new List<string>();
                                    outputLines.Add($"[欄位 {i + 1}]");
                                    
                                    // 按行索引排序段落,確保順序正確
                                    var sortedParagraphs = result.Paragraphs
                                        .OrderBy(p => p.LineIndices.FirstOrDefault())
                                        .ToList();
                                    
                                    // 逐個段落添加翻譯文本
                                    foreach (var para in sortedParagraphs)
                                    {
                                        outputLines.Add(para.TranslatedText);
                                    }
                                    
                                    // 組合這個欄位的所有行
                                    columnOutputs.Add(string.Join("\n", outputLines));
                                    
                                    var detailMsg = $"✅ 欄位{i + 1}翻譯成功 - 檢測語言: {result.DetectedLanguage}, 段落數: {result.Paragraphs.Count}";
                                    Logger.Info(detailMsg);
                                    Console.WriteLine($"   {detailMsg}");
                                }
                                else
                                {
                                    var errorMsg = $"❌ 欄位{i + 1}翻譯失敗: {result.ErrorMessage ?? "無段落"}";
                                    Logger.Warn(errorMsg);
                                    Console.WriteLine($"   {errorMsg}");
                                }
                            }
                            
                            if (columnOutputs.Count > 0)
                            {
                                // 使用雙換行分隔不同欄位
                                var analyzedText = string.Join("\n\n", columnOutputs);
                                
                                Console.WriteLine();
                                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                                Console.WriteLine("📄 翻譯結果 (按欄位輸出):");
                                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                                Console.WriteLine(analyzedText);
                                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                                
                                Logger.Info($"🎯 AI翻譯完成，共{columnOutputs.Count}個欄位");
                                
                                // 調試視覺化已在版面分析完成後立即顯示,不需要重複
                                
                                return (analyzedText, ocrResult);
                            }
                        }
                        catch (Exception aiEx)
                        {
                            Logger.Error(aiEx, "❌ AI翻譯失敗，回退到傳統翻譯流程");
                        }
                    }
                    else
                    {
                        Logger.Warn("⚠️ AITranslationService未初始化，回退到傳統翻譯流程");
                    }
                }
                else
                {
                    Logger.Warn($"⚠️ 版面分析V2失敗或無欄位，回退到傳統文字合併");
                }
                
                // ============ PHASE 2B: 【後備方案】傳統文字合併 ============
                // 將零散的文字行合併成連貫的句子
                string mergedText = TextMerger.MergeForSubtitle(ocrResult);
                
                return (mergedText, ocrResult);
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

        private void ShowTranslationResult(string originalText, string translatedText, Rect sourceRegion, OcrResult ocrResult = null)
        {
            try
            {
                Logger.Info("*** 🎯 ShowTranslationResult 開始執行 ***");
                Logger.Info($"*** 📝 原文: {originalText} ***");
                Logger.Info($"*** 🌐 譯文: {translatedText} ***");
                
                // ============ PHASE 4: 顯示結果分發 ============
                // 檢查是否為Cover模式，如果是則跳過字幕視窗創建
                bool isCoverModeEnabled = false;
                Logger.Info($"*** 🔍 檢查Cover模式 - _mainBarWindow: {_mainBarWindow != null} ***");
                Logger.Info($"*** 🔍 檢查Cover模式 - DataContext: {_mainBarWindow?.DataContext != null} ***");
                Logger.Info($"*** 🔍 檢查Cover模式 - DataContext類型: {_mainBarWindow?.DataContext?.GetType()?.Name} ***");
                
                if (_mainBarWindow?.DataContext is WorkingMainBarWindowViewModel viewModel)
                {
                    isCoverModeEnabled = viewModel.IsCoverModeEnabled;
                    Logger.Info($"*** 🎛️ Cover模式狀態: {isCoverModeEnabled} ***");
                }
                else
                {
                    Logger.Warn("*** ⚠️ 無法獲取Cover模式狀態 - DataContext類型不匹配 ***");
                }
                
                // 只有在非Cover模式下才創建和顯示字幕視窗
                if (!isCoverModeEnabled)
                {
                    Logger.Debug("📺 字幕模式 - 確保字幕視窗存在並設置 DisplayService");
                    
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
                                var subtitleViewModel = _subtitleWindow.DataContext as MonLingo.Core.ViewModel.SubtitleViewModel;
                                _displayService.SetSubtitleViewModel(subtitleViewModel);
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
                }
                else
                {
                    Logger.Info("*** 🎭 Cover模式啟用 - 跳過字幕視窗創建 ***");
                }

                // 使用 DisplayService 顯示翻譯結果
                Logger.Info("*** 📊 檢查 DisplayService 狀態 ***");
                Logger.Info($"*** _displayService 是否為 null: {_displayService == null} ***");
                
                if (_displayService != null)
                {
                    // 設置覆蓋模式所需的 OCR 結果和區域資訊
                    if (ocrResult != null)
                    {
                        Logger.Info("*** 設置 OCR 上下文 ***");
                        _displayService.SetOcrContext(ocrResult, sourceRegion);
                    }
                    
                    Logger.Info("*** ✅ 調用 DisplayService.Show() ***");
                    _displayService.Show(originalText, translatedText);
                    Logger.Info("*** ✅ DisplayService.Show() 調用完成 ***");
                }
                else
                {
                    Logger.Info("*** 🔄 DisplayService 不可用，直接調用字幕視窗 ***");
                    _subtitleWindow.AddSubtitleLine(originalText, translatedText);
                    Logger.Info("*** ✅ AddSubtitleLine() 調用完成 ***");
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

        /// <summary>
        /// 專門用於版面分析的方法（只執行OCR和版面分析，不進行翻譯）
        /// </summary>
        /// <param name="selectedRegion">選定的區域</param>
        public async Task ProcessSelectedRegionForLayoutAnalysisAsync(Rect selectedRegion)
        {
            Logger.Info($"🔍 ProcessSelectedRegionForLayoutAnalysisAsync 開始版面分析: {selectedRegion}");
            
            try
            {
                // 確保服務已初始化
                EnsureServicesInitialized();
                
                // 保存當前選中的區域（用於座標轉換）
                _currentSelectedRegion = selectedRegion;

                // 步驟1: 截圖
                Logger.Info("📸 執行區域截圖");
                var screenshot = CaptureScreenRegion(selectedRegion);
                
                // 步驟2: OCR識別
                Logger.Info("🔤 執行OCR識別");
                var originalText = await PerformOcrAsync(screenshot);
                
                Logger.Info($"✅ 版面分析完成 - 識別到文字: {originalText?.Length ?? 0} 字符");
                
                // 不進行翻譯和顯示，只進行OCR版面分析和調試視覺化
                // OCR調試信息已經在PerformOcrAsync中顯示了
                
                screenshot?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ 版面分析處理失敗");
                throw new Exception($"版面分析失敗: {ex.Message}", ex);
            }
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
                
                // 檢查多螢幕環境
                var isMultiScreen = MultiScreenHelper.IsMultiScreenEnvironment();
                Logger.Info($"🖥️ 多螢幕環境：{(isMultiScreen ? "是" : "否")}");
                
                if (isMultiScreen)
                {
                    Logger.Info(MultiScreenHelper.GetAllScreensInfo());
                }
                
                // 創建調試覆蓋層（如果尚未創建）
                if (_ocrDebugOverlay == null)
                {
                    _ocrDebugOverlay = new OcrDebugOverlay();
                    Logger.Debug("📱 創建新的OCR調試覆蓋層");
                }

                // 檢測目標螢幕（基於當前選中區域）
                var targetScreen = MultiScreenHelper.GetScreenContainingRegion(
                    new System.Drawing.Rectangle(
                        (int)_currentSelectedRegion.X,
                        (int)_currentSelectedRegion.Y,
                        (int)_currentSelectedRegion.Width,
                        (int)_currentSelectedRegion.Height
                    )
                );
                
                Logger.Info($"🎯 OCR區域：({_currentSelectedRegion.X},{_currentSelectedRegion.Y},{_currentSelectedRegion.Width},{_currentSelectedRegion.Height})");
                Logger.Info($"🖥️ 目標螢幕：({targetScreen.X},{targetScreen.Y},{targetScreen.Width},{targetScreen.Height})");
                
                // 設置覆蓋層到目標螢幕
                _ocrDebugOverlay.SetTargetScreen(targetScreen);

                // 計算座標轉換參數
                var dpiScale = GetDpiScale();
                var coordinateTransform = new CoordinateTransform
                {
                    SelectedRegion = _currentSelectedRegion,
                    DpiScale = dpiScale,
                    VirtualScreenLeft = SystemParameters.VirtualScreenLeft,
                    VirtualScreenTop = SystemParameters.VirtualScreenTop
                };

                Logger.Info($"📐 座標轉換參數：DPI={dpiScale}, 虛擬螢幕偏移=({SystemParameters.VirtualScreenLeft},{SystemParameters.VirtualScreenTop})");

                // 顯示調試信息，傳遞座標轉換參數（沒有版面分析結果）
                _ocrDebugOverlay.ShowOcrDebugInfo(ocrResult, coordinateTransform, null);
                
                Logger.Info("✅ OCR調試可視化顯示完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "顯示OCR調試信息時發生錯誤");
            }
        }

        /// <summary>
        /// 顯示版面分析調試信息
        /// </summary>
        /// <param name="ocrResult">OCR識別結果</param>
        /// <param name="layoutResult">版面分析結果</param>
        private void ShowLayoutAnalysisDebugInfo(OcrResult ocrResult, LayoutAnalysisResult layoutResult)
        {
            try
            {
                Logger.Info("🔍 顯示版面分析調試可視化");
                
                // 檢查多螢幕環境
                var isMultiScreen = MultiScreenHelper.IsMultiScreenEnvironment();
                Logger.Info($"🖥️ 多螢幕環境：{(isMultiScreen ? "是" : "否")}");
                
                if (isMultiScreen)
                {
                    Logger.Info(MultiScreenHelper.GetAllScreensInfo());
                }
                
                // 顯示版面分析結果詳情
                if (layoutResult?.Success == true)
                {
                    Logger.Info($"📊 版面分析成功：{layoutResult.Layout.Count} 個欄位");
                    if (layoutResult.DebugInfo?.MergedOriginalIndices != null)
                    {
                        Logger.Info($"🔗 合併資訊：{layoutResult.DebugInfo.MergedOriginalIndices.Count} 個原始索引被合併");
                        Logger.Info($"🔗 合併索引列表：[{string.Join(", ", layoutResult.DebugInfo.MergedOriginalIndices)}]");
                    }
                }
                else
                {
                    Logger.Warn($"⚠️ 版面分析失敗：{layoutResult?.ErrorMessage ?? "未知錯誤"}");
                }
                
                // 創建調試覆蓋層（如果尚未創建）
                if (_ocrDebugOverlay == null)
                {
                    _ocrDebugOverlay = new OcrDebugOverlay();
                    Logger.Debug("📱 創建新的OCR調試覆蓋層");
                }

                // 檢測目標螢幕（基於當前選中區域）
                var targetScreen = MultiScreenHelper.GetScreenContainingRegion(
                    new System.Drawing.Rectangle(
                        (int)_currentSelectedRegion.X,
                        (int)_currentSelectedRegion.Y,
                        (int)_currentSelectedRegion.Width,
                        (int)_currentSelectedRegion.Height
                    )
                );
                
                Logger.Info($"🎯 OCR區域：({_currentSelectedRegion.X},{_currentSelectedRegion.Y},{_currentSelectedRegion.Width},{_currentSelectedRegion.Height})");
                Logger.Info($"🖥️ 目標螢幕：({targetScreen.X},{targetScreen.Y},{targetScreen.Width},{targetScreen.Height})");
                
                // 設置覆蓋層到目標螢幕
                _ocrDebugOverlay.SetTargetScreen(targetScreen);

                // 計算座標轉換參數
                var dpiScale = GetDpiScale();
                var coordinateTransform = new CoordinateTransform
                {
                    SelectedRegion = _currentSelectedRegion,
                    DpiScale = dpiScale,
                    VirtualScreenLeft = SystemParameters.VirtualScreenLeft,
                    VirtualScreenTop = SystemParameters.VirtualScreenTop
                };

                Logger.Info($"📐 座標轉換參數：DPI={dpiScale}, 虛擬螢幕偏移=({SystemParameters.VirtualScreenLeft},{SystemParameters.VirtualScreenTop})");

                // 顯示版面分析調試信息，傳遞座標轉換參數
                _ocrDebugOverlay.ShowLayoutAnalysisDebugInfo(ocrResult, layoutResult, coordinateTransform);
                
                Logger.Info("✅ 版面分析調試可視化顯示完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "顯示版面分析調試信息時發生錯誤");
            }
        }

        /// <summary>
        /// 從版面分析結果中提取文字
        /// 按照版面結構（欄位→段落→行）重組文字內容
        /// </summary>
        /// <param name="layoutResult">版面分析結果</param>
        /// <returns>重組後的文字內容</returns>
        private string ExtractTextFromLayout(LayoutAnalysisResult layoutResult)
        {
            try
            {
                var textBuilder = new System.Text.StringBuilder();
                
                foreach (var column in layoutResult.Layout)
                {
                    Logger.Debug($"📂 處理欄位：{column.Key}（{column.Value.Count} 個段落）");
                    
                    foreach (var paragraph in column.Value)
                    {
                        Logger.Debug($"📝 處理段落：{paragraph.ParagraphId}（{paragraph.Lines.Count} 行）");
                        
                        // 將段落中的所有行合併為一個段落
                        var paragraphText = string.Join(" ", paragraph.Lines.Select(line => line.Text.Trim()));
                        
                        if (!string.IsNullOrEmpty(paragraphText))
                        {
                            textBuilder.AppendLine(paragraphText);
                            Logger.Debug($"💬 段落文字：{paragraphText}");
                        }
                    }
                    
                    // 欄位之間添加額外的換行
                    textBuilder.AppendLine();
                }
                
                var result = textBuilder.ToString().Trim();
                Logger.Info($"📄 版面分析文字提取完成：{result.Length} 字符");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "從版面分析結果提取文字時出錯");
                return string.Empty;
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

        /// <summary>
        /// 顯示版面分析V2調試信息(AI驅動架構)
        /// </summary>
        private void ShowLayoutAnalysisDebugInfoV2(OcrResult ocrResult, LayoutAnalysisResultV2 layoutResult)
        {
            try
            {
                Logger.Info("🔍 顯示版面分析V2調試可視化(AI驅動)");
                
                // 檢查多螢幕環境
                var isMultiScreen = MultiScreenHelper.IsMultiScreenEnvironment();
                Logger.Info($"🖥️ 多螢幕環境：{(isMultiScreen ? "是" : "否")}");
                
                if (isMultiScreen)
                {
                    Logger.Info(MultiScreenHelper.GetAllScreensInfo());
                }
                
                // 顯示版面分析結果詳情
                if (layoutResult?.Success == true)
                {
                    Logger.Info($"📊 版面分析V2成功：{layoutResult.Columns?.Count ?? 0} 個欄位");
                    Logger.Info($"⏱️ 處理時間：{layoutResult.ProcessingTimeMs:F1}ms");
                    Logger.Info($"📝 總行數：{layoutResult.TotalLines}");
                }
                else
                {
                    Logger.Warn($"⚠️ 版面分析V2失敗");
                    return; // 失敗則不顯示調試視覺化
                }
                
                // 創建調試覆蓋層(如果尚未創建)
                if (_ocrDebugOverlay == null)
                {
                    _ocrDebugOverlay = new OcrDebugOverlay();
                    Logger.Debug("📱 創建新的OCR調試覆蓋層");
                }

                // 檢測目標螢幕(基於當前選中區域)
                var targetScreen = MultiScreenHelper.GetScreenContainingRegion(
                    new System.Drawing.Rectangle(
                        (int)_currentSelectedRegion.X,
                        (int)_currentSelectedRegion.Y,
                        (int)_currentSelectedRegion.Width,
                        (int)_currentSelectedRegion.Height
                    )
                );
                
                Logger.Info($"🎯 OCR區域：({_currentSelectedRegion.X},{_currentSelectedRegion.Y},{_currentSelectedRegion.Width},{_currentSelectedRegion.Height})");
                Logger.Info($"🖥️ 目標螢幕：({targetScreen.X},{targetScreen.Y},{targetScreen.Width},{targetScreen.Height})");
                
                // 設置覆蓋層到目標螢幕
                _ocrDebugOverlay.SetTargetScreen(targetScreen);

                // 計算座標轉換參數
                var dpiScale = GetDpiScale();
                var coordinateTransform = new CoordinateTransform
                {
                    SelectedRegion = _currentSelectedRegion,
                    DpiScale = dpiScale,
                    VirtualScreenLeft = SystemParameters.VirtualScreenLeft,
                    VirtualScreenTop = SystemParameters.VirtualScreenTop
                };

                Logger.Info($"📐 座標轉換參數：DPI={dpiScale}, 虛擬螢幕偏移=({SystemParameters.VirtualScreenLeft},{SystemParameters.VirtualScreenTop})");

                // 顯示版面分析V2調試可視化(用顏色標示不同欄位)
                _ocrDebugOverlay.ShowLayoutAnalysisDebugInfoV2(ocrResult, layoutResult, coordinateTransform);
                
                Logger.Info("✅ 版面分析V2調試可視化顯示完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "顯示版面分析V2調試信息時發生錯誤");
            }
        }

        #endregion
    }
}
