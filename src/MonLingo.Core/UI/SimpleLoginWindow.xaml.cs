using System;
using System.Threading.Tasks;
using System.Windows;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;

namespace MonLingo.Core.UI
{
    /// <summary>
    /// 簡化登入視窗的互動邏輯
    /// </summary>
    public partial class SimpleLoginWindow : Window
    {
        private readonly IUserService _userService;
        private readonly ILicenseService _licenseService;
        private readonly MonLingo.Core.Service.INotificationService _notificationService;

        public SimpleLoginWindow()
        {
            InitializeComponent();
            
            // 從Phase5服務容器獲取服務
            try
            {
                _userService = Phase5ServiceContainer.GetService<IUserService>();
                _licenseService = Phase5ServiceContainer.GetService<ILicenseService>();
                _notificationService = Phase5ServiceContainer.GetService<MonLingo.Core.Service.INotificationService>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SimpleLoginWindow 服務初始化失敗: {ex.Message}");
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoginButton.IsEnabled = false;
                LoginButton.Content = "登入中...";

                // 使用demo帳號或輸入的帳號
                var email = string.IsNullOrEmpty(EmailTextBox.Text) ? "demo@example.com" : EmailTextBox.Text;
                var password = string.IsNullOrEmpty(PasswordTextBox.Password) ? "demo123" : PasswordTextBox.Password;

                if (_userService != null)
                {
                    var result = await _userService.LoginAsync(email, password);
                    if (result)
                    {
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("登入失敗", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("服務未初始化", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"登入錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "登入";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
