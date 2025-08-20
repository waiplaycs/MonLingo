using System;
using System.Threading.Tasks;
using System.Windows;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MonLingo.Core.UI
{
    /// <summary>
    /// 忘記密碼對話框
    /// </summary>
    public partial class ForgotPasswordDialog : Window
    {
        private readonly IUserService _userService;
        private readonly MonLingo.Core.Service.INotificationService _notificationService;

        public ForgotPasswordDialog()
        {
            InitializeComponent();
            
            // 從 Phase5ServiceContainer 獲取服務
            _userService = Phase5ServiceContainer.GetService<IUserService>();
            _notificationService = Phase5ServiceContainer.GetService<MonLingo.Core.Service.INotificationService>();

            // 設定焦點
            EmailTextBox.Focus();
        }

        #region 視窗控制事件

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        #endregion

        #region 重設密碼相關事件

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformPasswordResetAsync();
        }

        private async Task PerformPasswordResetAsync()
        {
            try
            {
                // 驗證輸入
                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    ShowError("請輸入電子郵件地址");
                    EmailTextBox.Focus();
                    return;
                }

                var email = EmailTextBox.Text.Trim();

                // 簡單的電子郵件格式驗證
                if (!IsValidEmail(email))
                {
                    ShowError("請輸入有效的電子郵件地址");
                    EmailTextBox.Focus();
                    return;
                }

                // 顯示載入狀態
                ShowLoading("正在發送重設連結...");

                // 執行密碼重設
                var success = await _userService.ResetPasswordAsync(email);

                HideLoading();

                if (success)
                {
                    ShowSuccess($"密碼重設連結已發送到 {email}，請檢查您的郵箱");
                    
                    // 禁用重設按鈕，防止重複發送
                    ResetButton.IsEnabled = false;
                    ResetButton.Content = "已發送";
                    
                    // 延遲關閉視窗
                    await Task.Delay(3000);
                    
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowError("發送重設連結失敗，請確認電子郵件地址是否正確");
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                ShowError($"重設密碼時發生錯誤：{ex.Message}");
            }
        }

        #endregion

        #region 輸入驗證

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region UI 狀態管理

        private void ShowLoading(string message)
        {
            LoadingText.Text = message;
            LoadingPanel.Visibility = Visibility.Visible;
            
            // 禁用控制項
            ResetButton.IsEnabled = false;
            EmailTextBox.IsEnabled = false;
            
            HideMessages();
        }

        private void HideLoading()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            
            // 啟用控制項
            ResetButton.IsEnabled = true;
            EmailTextBox.IsEnabled = true;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
            SuccessPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccess(string message)
        {
            SuccessText.Text = message;
            SuccessPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void HideMessages()
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
            SuccessPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region 鍵盤快速鍵

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == System.Windows.Input.Key.Enter && ResetButton.IsEnabled)
            {
                ResetButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CloseButton_Click(this, new RoutedEventArgs());
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 預填電子郵件
        /// </summary>
        public void SetEmail(string email)
        {
            EmailTextBox.Text = email;
        }

        #endregion
    }
}
