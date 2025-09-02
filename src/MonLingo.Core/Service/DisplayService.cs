using System;
using System.Collections.Generic;
using System.Windows;
using MonLingo.Core.ViewModel;
using MonLingo.ViewModel;
using MonLingo.Core.View.Windows;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 顯示服務 - 負責管理不同顯示模式的 UI 更新
    /// 根據文檔 "(畫面輸出)字幕模式連接真正的ocr.md" 實現
    /// 支援字幕模式和覆蓋模式切換
    /// </summary>
    public class DisplayService : IDisplayService
    {
        private readonly IConfigService _configService;
        private SubtitleViewModel _subtitleViewModel;
        private WorkingMainBarWindowViewModel _mainBarViewModel;
        private OverlayDisplayManager _overlayManager;
        private OcrResult _lastOcrResult;
        private Rect _lastRegion;
        
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        public DisplayService(IConfigService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _overlayManager = new OverlayDisplayManager();
        }
        
        /// <summary>
        /// 設置字幕 ViewModel 引用
        /// </summary>
        /// <param name="subtitleViewModel">字幕 ViewModel</param>
        public void SetSubtitleViewModel(SubtitleViewModel subtitleViewModel)
        {
            _subtitleViewModel = subtitleViewModel;
        }
        
        /// <summary>
        /// 設置主工具條 ViewModel 引用
        /// </summary>
        /// <param name="mainBarViewModel">主工具條 ViewModel</param>
        public void SetMainBarViewModel(WorkingMainBarWindowViewModel mainBarViewModel)
        {
            _mainBarViewModel = mainBarViewModel;
            Logger.Info($"[DisplayService] SetMainBarViewModel: ViewModel已設置, IsCoverModeEnabled={mainBarViewModel?.IsCoverModeEnabled}");
        }
        
        /// <summary>
        /// 設置 OCR 結果和區域資訊（用於覆蓋模式）
        /// </summary>
        /// <param name="ocrResult">OCR 結果</param>
        /// <param name="region">區域資訊</param>
        public void SetOcrContext(OcrResult ocrResult, Rect region)
        {
            _lastOcrResult = ocrResult;
            _lastRegion = region;
        }
        
        /// <summary>
        /// 顯示翻譯結果
        /// 根據當前顯示模式決定要更新哪個 UI
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="translatedText">譯文</param>
        public void Show(string originalText, string translatedText)
        {
            Logger.Info($"[DisplayService] *** Show 方法被調用 ***");
            Logger.Info($"[DisplayService] 原文: {originalText}");
            Logger.Info($"[DisplayService] 譯文: {translatedText}");
            Logger.Info($"[DisplayService] _mainBarViewModel: {_mainBarViewModel != null}");
            
            var currentMode = GetCurrentDisplayMode();
            Logger.Info($"[DisplayService] 顯示模式: {currentMode}");
            
            switch (currentMode)
            {
                case DisplayMode.Subtitle:
                    ShowInSubtitleMode(originalText, translatedText);
                    break;
                    
                case DisplayMode.Overlay:
                    ShowInOverlayMode(originalText, translatedText);
                    break;
                    
                case DisplayMode.Popup:
                    ShowInPopupMode(originalText, translatedText);
                    break;
                    
                default:
                    // 默認使用字幕模式
                    ShowInSubtitleMode(originalText, translatedText);
                    break;
            }
        }

        /// <summary>
        /// 開始新的顯示回合：清空字幕面板一次，並清理覆蓋模式視窗
        /// </summary>
        public void StartNewRound()
        {
            Logger.Info("[DisplayService] StartNewRound() invoked");
            
            // 清理覆蓋模式視窗
            _overlayManager?.ClearOverlay();
            
            // 清理字幕模式
            if (_subtitleViewModel == null)
            {
                Logger.Warn("[DisplayService] StartNewRound skipped: SubtitleViewModel is null");
                return;
            }

            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Logger.Info("[DisplayService] StartNewRound -> VM.StartNewRound()");
                    _subtitleViewModel.StartNewRound();
                });
            }
            else
            {
                Logger.Info("[DisplayService] StartNewRound -> VM.StartNewRound() (no dispatcher)");
                _subtitleViewModel.StartNewRound();
            }
        }
        
        /// <summary>
        /// 在字幕模式中顯示
        /// </summary>
        private void ShowInSubtitleMode(string originalText, string translatedText)
        {
            if (_subtitleViewModel == null)
            {
                throw new InvalidOperationException("SubtitleViewModel is not set. Call SetSubtitleViewModel first.");
            }
            
            // 【連接點】呼叫 SubtitleViewModel 的公共方法
            // 需要確保此操作在 UI 執行緒上執行
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _subtitleViewModel.AddNewLine(originalText, translatedText);
                });
            }
            else
            {
                // 如果不在 WPF 應用程式中，直接調用
                _subtitleViewModel.AddNewLine(originalText, translatedText);
            }
        }
        
        /// <summary>
        /// 在覆蓋模式中顯示
        /// </summary>
        private void ShowInOverlayMode(string originalText, string translatedText)
        {
            try
            {
                Logger.Info($"[DisplayService] 顯示覆蓋模式: {translatedText}");
                Logger.Debug($"[DisplayService] _overlayManager={_overlayManager != null}");
                Logger.Debug($"[DisplayService] _lastOcrResult={_lastOcrResult != null}");
                Logger.Debug($"[DisplayService] OCR Lines Count={_lastOcrResult?.Lines?.Length ?? 0}");
                
                if (_lastOcrResult?.Lines == null || _lastOcrResult.Lines.Length == 0)
                {
                    Logger.Warn("[DisplayService] 缺少 OCR 結果，無法顯示覆蓋模式，降級到字幕模式");
                    ShowInSubtitleMode(originalText, translatedText);
                    return;
                }
                
                // 為每個 OCR 行創建對應的翻譯文字
                var translatedTexts = new List<string>();
                
                Logger.Info($"[DisplayService] 處理 {_lastOcrResult.Lines.Length} 個 OCR 行的翻譯");
                
                foreach (var ocrLine in _lastOcrResult.Lines)
                {
                    // 為每個 OCR 行的文字單獨翻譯
                    if (!string.IsNullOrWhiteSpace(ocrLine.Text))
                    {
                        // 使用 OCR 行的文字作為翻譯源
                        // 注意：這裡使用 ocrLine.Text 而不是完整的 translatedText
                        Logger.Debug($"[DisplayService] OCR行文字: '{ocrLine.Text}' -> 翻譯使用對應部分");
                        
                        // 由於我們只有完整翻譯結果，這裡需要改進
                        // 暫時使用原始OCR文字（這樣用戶可以看到每行的原始內容）
                        // 實際生產中應該對每行單獨進行翻譯
                        translatedTexts.Add(ocrLine.Text);
                    }
                    else
                    {
                        translatedTexts.Add("");
                    }
                }
                
                Logger.Info($"[DisplayService] 調用 OverlayManager.ShowOverlay，翻譯文字數量: {translatedTexts.Count}");
                
                // 使用覆蓋管理器顯示
                _overlayManager.ShowOverlay(_lastOcrResult, translatedTexts, _lastRegion);
                
                Logger.Info("[DisplayService] 覆蓋模式顯示完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[DisplayService] 覆蓋模式顯示失敗");
                // 發生錯誤時降級到字幕模式
                ShowInSubtitleMode(originalText, translatedText);
            }
        }
        
        /// <summary>
        /// 在彈出模式中顯示（未實現）
        /// </summary>
        private void ShowInPopupMode(string originalText, string translatedText)
        {
            // TODO: 實現彈出模式的 UI 更新邏輯
            // 可能需要顯示 TranslationResultWindow
        }
        
        /// <summary>
        /// 獲取當前顯示模式
        /// </summary>
        private DisplayMode GetCurrentDisplayMode()
        {
            try
            {
                Logger.Info($"[DisplayService] *** GetCurrentDisplayMode 被調用 ***");
                Logger.Info($"[DisplayService] _mainBarViewModel 是否為 null: {_mainBarViewModel == null}");
                
                if (_mainBarViewModel != null)
                {
                    Logger.Info($"[DisplayService] IsCoverModeEnabled: {_mainBarViewModel.IsCoverModeEnabled}");
                }
                
                // 從主工具條 ViewModel 獲取覆蓋模式設置
                if (_mainBarViewModel?.IsCoverModeEnabled == true)
                {
                    Logger.Info("[DisplayService] *** 使用覆蓋模式 (Overlay Mode) ***");
                    return DisplayMode.Overlay;
                }
                
                // 預設使用字幕模式
                Logger.Info("[DisplayService] *** 使用字幕模式 (Subtitle Mode) ***");
                return DisplayMode.Subtitle;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[DisplayService] GetCurrentDisplayMode 發生錯誤");
                return DisplayMode.Subtitle;
            }
        }
    }
    
    /// <summary>
    /// 顯示模式枚舉
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>
        /// 字幕模式 - 在字幕視窗中顯示
        /// </summary>
        Subtitle,
        
        /// <summary>
        /// 覆蓋模式 - 在原文位置覆蓋顯示
        /// </summary>
        Overlay,
        
        /// <summary>
        /// 彈出模式 - 彈出視窗顯示
        /// </summary>
        Popup
    }
}
