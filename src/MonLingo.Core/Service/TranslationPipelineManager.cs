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
        
        public event EventHandler<TranslationResult> TranslationCompleted;
        public event EventHandler CaptureRequested;
        public event EventHandler<Exception> ErrorOccurred;
        
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
                    SourceText = ocrResult.Text,
                    TranslatedText = translationResult,
                    Timestamp = DateTime.Now,
                    BoundingBox = ocrResult.BoundingBox,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Confidence = ocrResult.Confidence
                };
                
                // 5. 觸發翻譯完成事件
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
                
                // TODO: 添加具體的初始化邏輯
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
            // 使用當前前景視窗
            await StartCaptureSessionAsync(IntPtr.Zero);
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
    }
}
