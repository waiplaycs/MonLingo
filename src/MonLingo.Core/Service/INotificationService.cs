using System;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 通知服務介面
    /// 用於顯示各種類型的使用者通知
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// 顯示資訊訊息
        /// </summary>
        void ShowInfo(string message);
        
        /// <summary>
        /// 顯示成功訊息
        /// </summary>
        void ShowSuccess(string message);
        
        /// <summary>
        /// 顯示警告訊息
        /// </summary>
        void ShowWarning(string message);
        
        /// <summary>
        /// 顯示錯誤訊息
        /// </summary>
        void ShowError(string message);
        
        /// <summary>
        /// 顯示帶有自訂持續時間的訊息
        /// </summary>
        void ShowMessage(string message, NotificationType type, TimeSpan duration);
    }
    
    /// <summary>
    /// 通知類型
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
