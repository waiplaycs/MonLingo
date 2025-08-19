using System;
using System.Threading.Tasks;
using System.Text;
using System.Drawing;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 翻譯管線管理器實現
    /// 基於 Gaminik.Core.TranslationPipelineManager 設計（PRD §11.3 完整實現）
    /// </summary>
    public class TranslationPipelineManager : ITranslationPipelineManager
    {
        private readonly IScreenCaptureService _screenCaptureService;
        private readonly IOcrService _ocrService;
        private readonly ITranslateService _translateService;
        private readonly INotificationService _notificationService;
        private readonly IConfigService _configService;
        
        // 工作流程狀態管理
        private bool _isCapturing = false;
        private IntPtr _currentTargetWindow = IntPtr.Zero;
        
        public bool IsCapturing => _isCapturing;
        
        public event Action<TranslationResult> TranslationCompleted;
        public event Action<string> ErrorOccurred;
        
        public TranslationPipelineManager(
            IScreenCaptureService screenCaptureService,
            IOcrService ocrService,
            ITranslateService translateService,
            INotificationService notificationService,
            IConfigService configService)
        {
            _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _translateService = translateService ?? throw new ArgumentNullException(nameof(translateService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
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
                ErrorOccurred?.Invoke(errorMessage);
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
                    ErrorOccurred?.Invoke($"Frame processing error: {ex.Message}");
                    // 繼續處理，不中斷整個會話
                }
            }
        }
        
        /// <summary>
        /// 處理單一幀：OCR → 翻譯 → 顯示（PRD §11.3）
        /// </summary>
        private async Task ProcessFrameAsync(CaptureFrame frame)
        {
            try
            {
                // 1. OCR 處理
                var ocrResult = await _ocrService.RecognizeTextAsync(
                    frame.ImageData, frame.Width, frame.Height);
                
                if (string.IsNullOrWhiteSpace(ocrResult?.Text))
                {
                    return; // 沒有識別到文字，跳過
                }
                
                // 2. 取得翻譯設定
                var sourceLanguage = await _configService.GetAsync<string>("SourceLanguage") ?? "auto";
                var targetLanguage = await _configService.GetAsync<string>("TargetLanguage") ?? "zh-TW";
                
                // 3. 翻譯處理
                var translationResult = await _translateService.TranslateAsync(
                    ocrResult.Text, sourceLanguage, targetLanguage);
                
                if (string.IsNullOrWhiteSpace(translationResult))
                {
                    return; // 翻譯失敗，跳過
                }
                
                // 4. 建立翻譯結果
                var result = new TranslationResult
                {
                    OriginalText = ocrResult.Text,
                    TranslatedText = translationResult,
                    Timestamp = DateTime.Now,
                    BoundingBox = ocrResult.BoundingBox,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Confidence = ocrResult.Confidence
                };
                
                // 5. 觸發翻譯完成事件
                TranslationCompleted?.Invoke(result);
                
                // 6. 顯示通知（可選）
                if (await _configService.GetAsync<bool>("ShowNotifications"))
                {
                    _notificationService.ShowSuccess($"翻譯完成: {translationResult}");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Frame processing failed: {ex.Message}");
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
                ErrorOccurred?.Invoke($"Stop capture session failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            StopCaptureSession();
        }
    }
}
