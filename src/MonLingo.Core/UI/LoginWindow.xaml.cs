using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MonLingo.Core.UI
{
    /// <summary>
    /// 登入視窗
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly IUserService _userService;
        private readonly ILicenseService _licenseService;
        private readonly MonLingo.Core.Service.INotificationService _notificationService;
        private bool _isLoginMode = true;

        public LoginWindow()
        {
            InitializeComponent();
            
            // 從Phase5服務容器獲取服務
            _userService = Phase5ServiceContainer.GetService<IUserService>();
            _licenseService = Phase5ServiceContainer.GetService<ILicenseService>();
            _notificationService = Phase5ServiceContainer.GetService<MonLingo.Core.Service.INotificationService>();
        }

        #region 視窗控制事件

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        #endregion

        #region 標籤切換事件

        private void LoginTab_Click(object sender, RoutedEventArgs e)
        {
            SwitchToLoginMode();
        }

        private void RegisterTab_Click(object sender, RoutedEventArgs e)
        {
            SwitchToRegisterMode();
        }

        private void SwitchToLoginMode()
        {
            _isLoginMode = true;
            LoginTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC"));
            LoginTab.Foreground = System.Windows.Media.Brushes.White;
            RegisterTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5"));
            RegisterTab.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666"));

            LoginForm.Visibility = Visibility.Visible;
            RegisterForm.Visibility = Visibility.Collapsed;
            
            HideMessages();
        }

        private void SwitchToRegisterMode()
        {
            _isLoginMode = false;
            RegisterTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#007ACC"));
            RegisterTab.Foreground = System.Windows.Media.Brushes.White;
            LoginTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5"));
            LoginTab.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666"));

            LoginForm.Visibility = Visibility.Collapsed;
            RegisterForm.Visibility = Visibility.Visible;
            
            HideMessages();
        }

        #endregion

        #region 登入相關事件

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformLoginAsync();
        }

        private async Task PerformLoginAsync()
        {
            try
            {
                // 驗證輸入
                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    ShowError("請輸入電子郵件");
                    EmailTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    ShowError("請輸入密碼");
                    PasswordBox.Focus();
                    return;
                }

                // 顯示載入狀態
                ShowLoading("正在登入...");

                // 執行登入
                var success = await _userService.LoginAsync(EmailTextBox.Text.Trim(), PasswordBox.Password);

                HideLoading();

                if (success)
                {
                    ShowSuccess("登入成功！");
                    
                    // 延遲關閉視窗，讓使用者看到成功訊息
                    await Task.Delay(1000);
                    
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowError("登入失敗，請檢查電子郵件和密碼是否正確");
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                ShowError($"登入過程中發生錯誤：{ex.Message}");
            }
        }

        private async void SmsLogin_Click(object sender, RoutedEventArgs e)
        {
            // 這裡應該打開簡訊登入對話框
            var smsDialog = new SmsLoginDialog();
            var result = smsDialog.ShowDialog();
            
            if (result == true)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void GoogleLogin_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 實現 Google OAuth 登入
            ShowError("Google 登入功能即將推出");
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            // 實現忘記密碼功能
            var forgotDialog = new ForgotPasswordDialog();
            forgotDialog.ShowDialog();
        }

        #endregion

        #region 註冊相關事件

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformRegisterAsync();
        }

        private async Task PerformRegisterAsync()
        {
            try
            {
                // 驗證輸入
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    ShowError("請輸入使用者名稱");
                    UsernameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(RegisterEmailTextBox.Text))
                {
                    ShowError("請輸入電子郵件");
                    RegisterEmailTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(RegisterPasswordBox.Password))
                {
                    ShowError("請輸入密碼");
                    RegisterPasswordBox.Focus();
                    return;
                }

                if (RegisterPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    ShowError("密碼確認不一致");
                    ConfirmPasswordBox.Focus();
                    return;
                }

                if (AgreeTermsCheckBox.IsChecked != true)
                {
                    ShowError("請同意服務條款和隱私政策");
                    AgreeTermsCheckBox.Focus();
                    return;
                }

                // 密碼強度驗證
                if (RegisterPasswordBox.Password.Length < 6)
                {
                    ShowError("密碼長度至少需要 6 個字符");
                    RegisterPasswordBox.Focus();
                    return;
                }

                // 顯示載入狀態
                ShowLoading("正在註冊...");

                // 執行註冊
                var success = await _userService.RegisterAsync(
                    RegisterEmailTextBox.Text.Trim(),
                    RegisterPasswordBox.Password,
                    UsernameTextBox.Text.Trim());

                HideLoading();

                if (success)
                {
                    ShowSuccess("註冊成功！歡迎加入 MonLingo");
                    
                    // 延遲關閉視窗
                    await Task.Delay(1500);
                    
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowError("註冊失敗，該電子郵件可能已被使用");
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                ShowError($"註冊過程中發生錯誤：{ex.Message}");
            }
        }

        private void TermsOfService_Click(object sender, RoutedEventArgs e)
        {
            // 打開服務條款頁面
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://monlingo.com/terms",
                UseShellExecute = true
            });
        }

        private void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            // 打開隱私政策頁面
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://monlingo.com/privacy",
                UseShellExecute = true
            });
        }

        #endregion

        #region UI 狀態管理

        private void ShowLoading(string message)
        {
            LoadingText.Text = message;
            LoadingPanel.Visibility = Visibility.Visible;
            
            // 禁用表單控制項
            if (_isLoginMode)
            {
                LoginButton.IsEnabled = false;
                EmailTextBox.IsEnabled = false;
                PasswordBox.IsEnabled = false;
            }
            else
            {
                RegisterButton.IsEnabled = false;
                UsernameTextBox.IsEnabled = false;
                RegisterEmailTextBox.IsEnabled = false;
                RegisterPasswordBox.IsEnabled = false;
                ConfirmPasswordBox.IsEnabled = false;
            }
            
            HideMessages();
        }

        private void HideLoading()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            
            // 啟用表單控制項
            if (_isLoginMode)
            {
                LoginButton.IsEnabled = true;
                EmailTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
            }
            else
            {
                RegisterButton.IsEnabled = true;
                UsernameTextBox.IsEnabled = true;
                RegisterEmailTextBox.IsEnabled = true;
                RegisterPasswordBox.IsEnabled = true;
                ConfirmPasswordBox.IsEnabled = true;
            }
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter)
            {
                if (_isLoginMode && LoginButton.IsEnabled)
                {
                    LoginButton_Click(this, new RoutedEventArgs());
                }
                else if (!_isLoginMode && RegisterButton.IsEnabled)
                {
                    RegisterButton_Click(this, new RoutedEventArgs());
                }
            }
            else if (e.Key == Key.Escape)
            {
                CloseButton_Click(this, new RoutedEventArgs());
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 預填電子郵件（用於自動填入）
        /// </summary>
        public void SetEmail(string email)
        {
            EmailTextBox.Text = email;
            RegisterEmailTextBox.Text = email;
        }

        /// <summary>
        /// 切換到註冊模式（用於從其他頁面跳轉）
        /// </summary>
        public void ShowRegisterForm()
        {
            SwitchToRegisterMode();
        }

        #endregion
    }
}
