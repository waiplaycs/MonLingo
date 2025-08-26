using System;
using System.Windows;
using MonLingo.Core.ViewModel;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 顯示服務 - 負責管理不同顯示模式的 UI 更新
    /// 根據文檔 "(畫面輸出)字幕模式連接真正的ocr.md" 實現
    /// </summary>
    public class DisplayService : IDisplayService
    {
        private readonly IConfigService _configService;
        private SubtitleViewModel _subtitleViewModel;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        public DisplayService(IConfigService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
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
        /// 顯示翻譯結果
        /// 根據當前顯示模式決定要更新哪個 UI
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="translatedText">譯文</param>
        public void Show(string originalText, string translatedText)
        {
            Logger.Debug($"[DisplayService] Show called. mode={GetCurrentDisplayMode()}, hasVM={_subtitleViewModel!=null}");
            // 根據設定決定要更新哪個 ViewModel
            var currentMode = GetCurrentDisplayMode();
            
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
        /// 開始新的顯示回合：清空字幕面板一次
        /// </summary>
        public void StartNewRound()
        {
            Logger.Info("[DisplayService] StartNewRound() invoked");
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
        /// 在覆蓋模式中顯示（未實現）
        /// </summary>
        private void ShowInOverlayMode(string originalText, string translatedText)
        {
            // TODO: 實現覆蓋模式的 UI 更新邏輯
            // 可能需要更新 OverlayViewModel 或直接操作覆蓋視窗
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
                // 從配置服務獲取顯示模式設定
                // 暫時默認為字幕模式
                return DisplayMode.Subtitle;
            }
            catch
            {
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
