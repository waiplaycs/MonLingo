using System;

namespace MonLingo.Core.Events
{
    /// <summary>
    /// OCR調試相關事件
    /// </summary>
    public static class OcrDebugEvents
    {
        /// <summary>
        /// 隱藏OCR調試覆蓋層事件
        /// </summary>
        public static event Action HideDebugOverlay;

        /// <summary>
        /// 觸發隱藏調試覆蓋層事件
        /// </summary>
        public static void TriggerHideDebugOverlay()
        {
            HideDebugOverlay?.Invoke();
        }
    }
}
