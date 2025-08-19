using System;
using System.Threading.Tasks;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// UI 與翻譯服務的橋接器
    /// 負責協調 UI 層和 Phase 5 核心翻譯功能
    /// </summary>
    public class UITranslationBridge : IDisposable
    {
        private readonly ITranslationPipelineManager _pipelineManager;
        private readonly INotificationService _notificationService;
        private bool _isInitialized = false;
        private bool _isDisposed = false;

        public UITranslationBridge(
            ITranslationPipelineManager pipelineManager,
            INotificationService notificationService)
        {
            _pipelineManager = pipelineManager ?? throw new ArgumentNullException(nameof(pipelineManager));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// 初始化橋接器 - 註冊事件
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized || _isDisposed)
                return;

            try
            {
                // 初始化翻譯管道
                await _pipelineManager.InitializeAsync();

                // 註冊翻譯完成事件
                _pipelineManager.TranslationCompleted += OnTranslationCompleted;
                _pipelineManager.CaptureRequested += OnCaptureRequested;
                _pipelineManager.ErrorOccurred += OnErrorOccurred;

                _isInitialized = true;
                
                _notificationService.ShowSuccess("MonLingo Phase 5 翻譯功能已就緒");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"翻譯功能初始化失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 開始翻譯會話 - 對應主按鈕功能
        /// </summary>
        public async Task StartTranslationSessionAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            try
            {
                _notificationService.ShowInfo("正在啟動螢幕翻譯會話...");
                await _pipelineManager.StartCaptureSessionAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"無法啟動翻譯: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 快速截圖翻譯 - 對應相機按鈕功能
        /// </summary>
        public async Task QuickScreenshotTranslationAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            try
            {
                _notificationService.ShowInfo("正在進行快速截圖翻譯...");
                
                // 觸發單次截圖翻譯
                await _pipelineManager.ProcessSingleFrameAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"快速翻譯失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止翻譯會話
        /// </summary>
        public async Task StopTranslationSessionAsync()
        {
            if (!_isInitialized)
                return;

            try
            {
                await _pipelineManager.StopCaptureSessionAsync();
                _notificationService.ShowInfo("翻譯會話已停止");
            }
            catch (Exception ex)
            {
                _notificationService.ShowWarning($"無法停止翻譯: {ex.Message}");
            }
        }

        #region 事件處理

        private void OnTranslationCompleted(object sender, TranslationResult result)
        {
            try
            {
                _notificationService.ShowSuccess($"翻譯完成: {result.SourceText} → {result.TranslatedText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"顯示翻譯結果失敗: {ex.Message}");
            }
        }

        private void OnCaptureRequested(object sender, EventArgs e)
        {
            try
            {
                _notificationService.ShowInfo("請選擇要翻譯的螢幕區域");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"顯示截圖提示失敗: {ex.Message}");
            }
        }

        private void OnErrorOccurred(object sender, Exception ex)
        {
            try
            {
                _notificationService.ShowError($"翻譯過程中發生錯誤: {ex.Message}");
            }
            catch (Exception notifyEx)
            {
                System.Diagnostics.Debug.WriteLine($"顯示錯誤通知失敗: {notifyEx.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                // 取消訂閱事件
                if (_pipelineManager != null)
                {
                    _pipelineManager.TranslationCompleted -= OnTranslationCompleted;
                    _pipelineManager.CaptureRequested -= OnCaptureRequested;
                    _pipelineManager.ErrorOccurred -= OnErrorOccurred;
                }

                // 清理服務
                _pipelineManager?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UITranslationBridge 清理失敗: {ex.Message}");
            }
            finally
            {
                _isDisposed = true;
            }
        }

        #endregion
    }
}
