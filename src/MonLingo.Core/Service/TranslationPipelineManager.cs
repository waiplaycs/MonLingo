using System;
using System.Threading.Tasks;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 翻譯管線管理器實現
    /// 基於 Gaminik.Core.TranslationPipelineManager 設計（PRD §11.3 完整實現）
    /// 實現完整的 OCR → 文字合併 → 翻譯 → 顯示流水線
    /// </summary>
    public class TranslationPipelineManager : ITranslationPipelineManager
    {
        private readonly IScreenCaptureService _screenCaptureService;
        private readonly IOcrService _ocrService;
        private readonly ITranslateService _translateService;
        private readonly INotificationService _notificationService;
        private readonly IConfigService _configService;
        private readonly IDisplayService _displayService;
        private readonly ILanguageConfigService _languageConfigService;
        
        // 工作流程狀態管理
        private bool _isCapturing = false;
        private IntPtr _currentTargetWindow = IntPtr.Zero;
        
        public bool IsCapturing => _isCapturing;
        
        public event EventHandler<TranslationResult> TranslationCompleted;
        public event EventHandler CaptureRequested;
        public event EventHandler<Exception> ErrorOccurred;
        
        public TranslationPipelineManager(
            IScreenCaptureService screenCaptureService,
            IOcrService ocrService,
            ITranslateService translateService,
            INotificationService notificationService,
            IConfigService configService,
            IDisplayService displayService,
            ILanguageConfigService languageConfigService)
        {
            _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _translateService = translateService ?? throw new ArgumentNullException(nameof(translateService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            _languageConfigService = languageConfigService ?? throw new ArgumentNullException(nameof(languageConfigService));
        }
        
        /// <summary>
        /// 啟動完整的擷取翻譯會話（PRD §11.3 完整實現）
        /// </summary>
        public async Task StartCaptureSessionAsync(IntPtr targetWindow)
        {
            if (_isCapturing) return;
            
            try
            {
                _isCapturing = true;
                _currentTargetWindow = targetWindow;
                
                // 1. 啟動螢幕擷取服務
                var captureStarted = _screenCaptureService.StartCapture(targetWindow);
                if (!captureStarted)
                {
                    throw new InvalidOperationException("Failed to start screen capture");
                }
                
                _notificationService.ShowInfo("開始截圖翻譯會話");
                
                // 2. 開始消費迴圈
                await StartConsumerLoopAsync();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Translation session failed: {ex.Message}";
                ErrorOccurred?.Invoke(this, new Exception(errorMessage));
                _notificationService.ShowError(errorMessage);
                StopCaptureSession();
            }
        }
        
        /// <summary>
        /// 消費者迴圈：持續讀取擷取結果並處理（PRD §11.3）
        /// </summary>
        private async Task StartConsumerLoopAsync()
        {
            while (_isCapturing)
            {
                try
                {
                    // 從螢幕擷取服務讀取最新幀
                    var frame = _screenCaptureService.ReadFrame();
                    
                    if (frame != null && frame.ImageData != null && frame.Size > 0)
                    {
                        // 執行 OCR + 翻譯管線
                        await ProcessFrameAsync(frame);
                    }
                    
                    // 控制消費頻率（避免 CPU 過載）
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    // 繼續處理，不中斷整個會話
                }
            }
        }
        
        /// <summary>
        /// 處理單一幀：OCR → 文字合併 → 翻譯 → 顯示（根據文檔完整實現）
        /// </summary>
        private async Task ProcessFrameAsync(CaptureFrame frame)
        {
            try
            {
                // 初始化 OCR 服務（如果需要）
                if (!_ocrService.IsInitialized)
                {
                    await _ocrService.InitializeAsync();
                }

                // ============ PHASE 1: OCR 處理 ============
                // 🎯 優先使用OCR服務的語言配置功能，自動記住用戶語言設定
                OcrResult ocrResult;
                
                if (_ocrService is RealOcrService configAwareOcrService)
                {
                    // 使用配置感知的OCR方法，自動記住語言設定
                    ocrResult = await configAwareOcrService.RecognizeTextWithConfigAsync(
                        frame.ImageData, frame.Width, frame.Height);
                }
                else
                {
                    // 後備方案：使用原有的OCR方法
                    ocrResult = await _ocrService.RecognizeTextAsync(
                        frame.ImageData, frame.Width, frame.Height);
                }
                
                if (ocrResult == null || ocrResult.Lines == null || !ocrResult.Lines.Any())
                {
                    return; // 沒有識別到文字，跳過
                }
                
                // ============ PHASE 2: 【字幕模式特殊邏輯】文字合併 ============
                // 將零散的文字行合併成連貫的句子
                string mergedText = TextMerger.MergeForSubtitle(ocrResult);
                
                if (string.IsNullOrWhiteSpace(mergedText))
                {
                    return; // 合併後沒有有效文字，跳過
                }
                
                // ============ PHASE 3: 翻譯處理 ============
                // 🎯 使用翻譯服務的語言配置功能，自動記住用戶語言設定
                string translationResult;
                
                // 優先使用TranslateWithConfigAsync，自動使用用戶配置的語言
                if (_translateService is TranslateService configAwareService)
                {
                    translationResult = await configAwareService.TranslateWithConfigAsync(mergedText);
                }
                else
                {
                    // 後備方案：手動獲取語言設定
                    var sourceLanguage = await _languageConfigService.GetSourceLanguageAsync();
                    var targetLanguage = await _languageConfigService.GetTargetLanguageAsync();
                    translationResult = await _translateService.TranslateAsync(mergedText, sourceLanguage, targetLanguage);
                }
                
                if (string.IsNullOrWhiteSpace(translationResult))
                {
                    return; // 翻譯失敗，跳過
                }
                
                // ============ PHASE 4: 顯示結果分發 ============
                // 設置覆蓋模式所需的 OCR 結果和區域資訊
                var wpfRect = new System.Windows.Rect(ocrResult.BoundingBox.X, ocrResult.BoundingBox.Y, 
                                                      ocrResult.BoundingBox.Width, ocrResult.BoundingBox.Height);
                _displayService.SetOcrContext(ocrResult, wpfRect);
                
                // 將結果交給 DisplayService 根據模式顯示
                _displayService.Show(mergedText, translationResult);
                
                // ============ PHASE 5: 事件通知 ============
                // 獲取當前語言設定用於結果記錄
                var currentSourceLanguage = await _languageConfigService.GetSourceLanguageAsync();
                var currentTargetLanguage = await _languageConfigService.GetTargetLanguageAsync();
                
                var result = new TranslationResult
                {
                    SourceText = mergedText,
                    TranslatedText = translationResult,
                    Timestamp = DateTime.Now,
                    BoundingBox = ocrResult.BoundingBox,
                    SourceLanguage = currentSourceLanguage,
                    TargetLanguage = currentTargetLanguage,
                    Confidence = ocrResult.Confidence
                };
                
                // 觸發翻譯完成事件
                TranslationCompleted?.Invoke(this, result);
                
                // 6. 顯示通知（可選）
                if (await _configService.GetAsync<bool>("ShowNotifications"))
                {
                    _notificationService.ShowSuccess($"翻譯完成: {translationResult}");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
        
        /// <summary>
        /// 初始化翻譯管道
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // 初始化各個服務
                _notificationService.ShowInfo("正在初始化翻譯服務...");
                
                // 初始化 OCR 服務（如果需要）
                if (!_ocrService.IsInitialized)
                {
                    await _ocrService.InitializeAsync();
                }
                
                // TODO: 添加其他服務的初始化邏輯
                await Task.Delay(100); // 模擬初始化時間
                
                _notificationService.ShowSuccess("翻譯服務已就緒");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }
        
        /// <summary>
        /// 啟動完整的擷取翻譯會話 - 自動偵測視窗
        /// </summary>
        public async Task StartCaptureSessionAsync()
        {
            // 獲取游標下的視窗或使用桌面視窗
            IntPtr targetWindow = IntPtr.Zero;
            try
            {
                // 先嘗試使用 P/Invoke 獲取前景視窗
                targetWindow = GetForegroundWindow();
                Console.WriteLine($"[DEBUG] GetForegroundWindow 返回: {targetWindow}");
                
                if (targetWindow == IntPtr.Zero)
                {
                    // 使用當前程序的主視窗
                    targetWindow = Process.GetCurrentProcess().MainWindowHandle;
                    Console.WriteLine($"[DEBUG] 使用當前程序主視窗: {targetWindow}");
                }
                
                if (targetWindow == IntPtr.Zero)
                {
                    // 作為最後後備方案，使用桌面視窗
                    targetWindow = GetDesktopWindow();
                    Console.WriteLine($"[DEBUG] 使用桌面視窗: {targetWindow}");
                }
                
                Console.WriteLine($"[DEBUG] 最終目標視窗: {targetWindow}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] 獲取目標視窗失敗: {ex.Message}");
                // 作為後備方案，使用桌面視窗
                targetWindow = GetDesktopWindow();
                Console.WriteLine($"[DEBUG] 使用後備桌面視窗: {targetWindow}");
            }
            
            await StartCaptureSessionAsync(targetWindow);
        }
        
        /// <summary>
        /// 停止擷取會話（異步版本）
        /// </summary>
        public async Task StopCaptureSessionAsync()
        {
            await Task.Run(() => StopCaptureSession());
        }
        
        /// <summary>
        /// 處理單一影格 - 用於快速翻譯
        /// </summary>
        public async Task ProcessSingleFrameAsync()
        {
            try
            {
                CaptureRequested?.Invoke(this, EventArgs.Empty);
                
                // 進行單次螢幕擷取
                var frameData = _screenCaptureService.ReadFrame();
                if (frameData != null && frameData.ImageData != null && frameData.ImageData.Length > 0)
                {
                    await ProcessFrameAsync(frameData);
                }
                else
                {
                    ErrorOccurred?.Invoke(this, new Exception("無法擷取螢幕畫面"));
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
        
        /// <summary>
        /// 停止擷取會話
        /// </summary>
        public void StopCaptureSession()
        {
            if (!_isCapturing) return;
            
            try
            {
                _isCapturing = false;
                _screenCaptureService.StopCapture();
                _currentTargetWindow = IntPtr.Zero;
                
                _notificationService.ShowInfo("截圖翻譯會話已停止");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
        
        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            StopCaptureSession();
        }
        
        #region Windows API P/Invoke
        
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();
        
        #endregion
    }
}
