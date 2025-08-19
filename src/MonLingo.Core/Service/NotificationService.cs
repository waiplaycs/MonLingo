using System;
using System.Windows;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 通知服務實現
    /// 使用 WPF MessageBox 和控制台輸出來顯示通知
    /// </summary>
    public class NotificationService : INotificationService
    {
        /// <summary>
        /// 顯示資訊訊息
        /// </summary>
        public void ShowInfo(string message)
        {
            ShowMessage(message, NotificationType.Info, TimeSpan.FromSeconds(3));
        }
        
        /// <summary>
        /// 顯示成功訊息
        /// </summary>
        public void ShowSuccess(string message)
        {
            ShowMessage(message, NotificationType.Success, TimeSpan.FromSeconds(3));
        }
        
        /// <summary>
        /// 顯示警告訊息
        /// </summary>
        public void ShowWarning(string message)
        {
            ShowMessage(message, NotificationType.Warning, TimeSpan.FromSeconds(5));
        }
        
        /// <summary>
        /// 顯示錯誤訊息
        /// </summary>
        public void ShowError(string message)
        {
            ShowMessage(message, NotificationType.Error, TimeSpan.FromSeconds(10));
        }
        
        /// <summary>
        /// 顯示帶有自訂持續時間的訊息
        /// </summary>
        public void ShowMessage(string message, NotificationType type, TimeSpan duration)
        {
            try
            {
                // 控制台輸出（用於調試）
                var prefix = GetTypePrefix(type);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} {prefix} {message}");
                
                // 如果在 UI 執行緒中，顯示 MessageBox（僅對重要訊息）
                if (Application.Current != null && 
                    Application.Current.Dispatcher.CheckAccess() &&
                    (type == NotificationType.Error || type == NotificationType.Warning))
                {
                    var icon = GetMessageBoxIcon(type);
                    var caption = GetTypeCaption(type);
                    
                    // 在背景執行緒中顯示，避免阻塞 UI
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show(message, caption, MessageBoxButton.OK, icon);
                    }));
                }
            }
            catch (Exception ex)
            {
                // 確保通知服務本身不會導致應用程式崩潰
                Console.WriteLine($"Notification service error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 獲取類型前綴
        /// </summary>
        private string GetTypePrefix(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => "[INFO]",
                NotificationType.Success => "[SUCCESS]",
                NotificationType.Warning => "[WARNING]",
                NotificationType.Error => "[ERROR]",
                _ => "[UNKNOWN]"
            };
        }
        
        /// <summary>
        /// 獲取 MessageBox 圖示
        /// </summary>
        private MessageBoxImage GetMessageBoxIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => MessageBoxImage.Information,
                NotificationType.Success => MessageBoxImage.Information,
                NotificationType.Warning => MessageBoxImage.Warning,
                NotificationType.Error => MessageBoxImage.Error,
                _ => MessageBoxImage.None
            };
        }
        
        /// <summary>
        /// 獲取類型標題
        /// </summary>
        private string GetTypeCaption(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => "資訊",
                NotificationType.Success => "成功",
                NotificationType.Warning => "警告",
                NotificationType.Error => "錯誤",
                _ => "通知"
            };
        }
    }
}
