using System;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 熱鍵整合服務
    /// 基於 PRD §2.3.1 NativeBridge 完整熱鍵系統
    /// </summary>
    public class HotKeyIntegrationService : IDisposable
    {
        private readonly ITranslationPipelineManager _pipelineManager;
        private readonly INotificationService _notificationService;
        private NativeBridge.NativeCallbackDelegate _callbackDelegate;
        private bool _isInitialized = false;
        
        public HotKeyIntegrationService(
            ITranslationPipelineManager pipelineManager,
            INotificationService notificationService)
        {
            _pipelineManager = pipelineManager ?? throw new ArgumentNullException(nameof(pipelineManager));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }
        
        /// <summary>
        /// 初始化熱鍵系統
        /// </summary>
        public bool InitializeHotKeys()
        {
            if (_isInitialized) return true;
            
            try
            {
                // 註冊 Native 層回呼
                _callbackDelegate = OnNativeCallback;
                NativeBridge.SetCallback(_callbackDelegate);
                
                // 註冊 F4 熱鍵（與 Gaminik 相同的觸發鍵）
                var success = NativeBridge.RegisterGlobalHotKey(
                    (int)HotKeyModifiers.None, 
                    115 // F4 鍵值
                );
                
                if (success)
                {
                    _isInitialized = true;
                    _notificationService.ShowSuccess("熱鍵 F4 已註冊，按下 F4 開始翻譯");
                }
                else
                {
                    _notificationService.ShowError("熱鍵註冊失敗");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"熱鍵初始化失敗: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Native 回呼處理函式
        /// </summary>
        private void OnNativeCallback(int messageType, int value)
        {
            try
            {
                if (messageType == (int)NativeMessageType.HotKeyPressed)
                {
                    _notificationService.ShowInfo("熱鍵觸發，開始截圖翻譯");
                    
                    // 獲取游標下的視窗
                    var targetWindow = NativeBridge.GetWindowUnderCursor();
                    
                    if (targetWindow != IntPtr.Zero)
                    {
                        // 觸發截圖翻譯流程
                        _ = _pipelineManager.StartCaptureSessionAsync(targetWindow);
                    }
                    else
                    {
                        _notificationService.ShowWarning("無法獲取目標視窗");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"熱鍵處理失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理資源
        /// </summary>
        public void Cleanup()
        {
            if (_isInitialized)
            {
                try
                {
                    NativeBridge.UnhookAll();
                    _isInitialized = false;
                    _notificationService.ShowInfo("熱鍵系統已清理");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"熱鍵清理失敗: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            Cleanup();
        }
    }
    
    /// <summary>
    /// 熱鍵修飾符
    /// </summary>
    [Flags]
    public enum HotKeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }
    
    /// <summary>
    /// Native 訊息類型
    /// </summary>
    public enum NativeMessageType
    {
        HotKeyPressed = 1,
        WindowCaptured = 2,
        OcrCompleted = 3,
        TranslationCompleted = 4
    }
}
