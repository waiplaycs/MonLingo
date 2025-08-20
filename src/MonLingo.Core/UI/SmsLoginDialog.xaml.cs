using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MonLingo.Core.UI
{
    /// <summary>
    /// 簡訊登入對話框
    /// </summary>
    public partial class SmsLoginDialog : Window
    {
        private readonly IUserService _userService;
        private readonly MonLingo.Core.Service.INotificationService _notificationService;
        private DispatcherTimer _countdownTimer;
        private int _countdownSeconds = 60;
        private string _currentPhone;

        public SmsLoginDialog()
        {
            InitializeComponent();
            
            // 從 Phase5ServiceContainer 獲取服務
            _userService = Phase5ServiceContainer.GetService<IUserService>();
            _notificationService = Phase5ServiceContainer.GetService<MonLingo.Core.Service.INotificationService>();

            // 設定倒數計時器
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
        }

        #region 視窗控制事件

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        #endregion

        #region 發送驗證碼相關事件

        private async void SendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            await SendVerificationCodeAsync();
        }

        private async Task SendVerificationCodeAsync()
        {
            try
            {
                // 驗證手機號碼
                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    ShowError("請輸入手機號碼");
                    PhoneTextBox.Focus();
                    return;
                }

                var countryCode = ((System.Windows.Controls.ComboBoxItem)CountryCodeComboBox.SelectedItem).Tag.ToString();
                var phone = PhoneTextBox.Text.Trim();
                
                // 簡單的手機號碼格式驗證
                if (phone.Length < 8 || phone.Length > 11)
                {
                    ShowError("請輸入有效的手機號碼");
                    PhoneTextBox.Focus();
                    return;
                }

                _currentPhone = $"+{countryCode}{phone}";

                // 顯示載入狀態
                ShowLoading("正在發送驗證碼...");

                // 發送驗證碼
                var success = await _userService.SendVerificationCodeAsync(_currentPhone);

                HideLoading();

                if (success)
                {
                    ShowSuccess($"驗證碼已發送到 {_currentPhone}");
                    
                    // 切換到驗證碼輸入界面
                    SwitchToCodeInput();
                    
                    // 啟動倒數計時
                    StartCountdown();
                }
                else
                {
                    ShowError("發送驗證碼失敗，請稍後再試");
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                ShowError($"發送驗證碼時發生錯誤：{ex.Message}");
            }
        }

        private async void ResendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendVerificationCodeAsync();
        }

        #endregion

        #region 登入相關事件

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformSmsLoginAsync();
        }

        private async Task PerformSmsLoginAsync()
        {
            try
            {
                // 驗證驗證碼
                if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
                {
                    ShowError("請輸入驗證碼");
                    CodeTextBox.Focus();
                    return;
                }

                var code = CodeTextBox.Text.Trim();
                
                if (code.Length != 6)
                {
                    ShowError("驗證碼應為6位數字");
                    CodeTextBox.Focus();
                    return;
                }

                // 顯示載入狀態
                ShowLoading("正在驗證...");

                // 執行簡訊登入
                var success = await _userService.LoginViaSmsAsync(_currentPhone, code);

                HideLoading();

                if (success)
                {
                    ShowSuccess("登入成功！");
                    
                    // 延遲關閉視窗
                    await Task.Delay(1000);
                    
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowError("驗證碼錯誤或已過期，請重新輸入");
                    CodeTextBox.SelectAll();
                    CodeTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                HideLoading();
                ShowError($"登入過程中發生錯誤：{ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToPhoneInput();
        }

        #endregion

        #region UI 狀態管理

        private void SwitchToCodeInput()
        {
            PhoneInputPanel.Visibility = Visibility.Collapsed;
            CodeInputPanel.Visibility = Visibility.Visible;
            CodeTextBox.Focus();
            HideMessages();
        }

        private void SwitchToPhoneInput()
        {
            CodeInputPanel.Visibility = Visibility.Collapsed;
            PhoneInputPanel.Visibility = Visibility.Visible;
            PhoneTextBox.Focus();
            
            // 停止倒數計時
            _countdownTimer.Stop();
            HideMessages();
        }

        private void ShowLoading(string message)
        {
            LoadingText.Text = message;
            LoadingPanel.Visibility = Visibility.Visible;
            
            // 禁用按鈕
            SendCodeButton.IsEnabled = false;
            LoginButton.IsEnabled = false;
            ResendButton.IsEnabled = false;
            
            HideMessages();
        }

        private void HideLoading()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            
            // 啟用按鈕
            SendCodeButton.IsEnabled = true;
            LoginButton.IsEnabled = true;
            ResendButton.IsEnabled = true;
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

        #region 倒數計時功能

        private void StartCountdown()
        {
            _countdownSeconds = 60;
            ResendButton.Visibility = Visibility.Collapsed;
            ResendHintText.Text = $"如果您沒有收到驗證碼，可在 {_countdownSeconds} 秒後重新發送";
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _countdownSeconds--;
            
            if (_countdownSeconds > 0)
            {
                ResendHintText.Text = $"如果您沒有收到驗證碼，可在 {_countdownSeconds} 秒後重新發送";
            }
            else
            {
                _countdownTimer.Stop();
                ResendHintText.Text = "如果您沒有收到驗證碼，可以重新發送";
                ResendButton.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region 輸入驗證和格式化

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 設定手機號碼輸入只允許數字
            PhoneTextBox.PreviewTextInput += (sender, e) =>
            {
                e.Handled = !IsNumeric(e.Text);
            };
            
            // 設定驗證碼輸入只允許數字
            CodeTextBox.PreviewTextInput += (sender, e) =>
            {
                e.Handled = !IsNumeric(e.Text);
            };
        }

        private bool IsNumeric(string text)
        {
            return int.TryParse(text, out _);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 預設國家代碼
        /// </summary>
        public void SetDefaultCountryCode(string countryCode)
        {
            for (int i = 0; i < CountryCodeComboBox.Items.Count; i++)
            {
                var item = (System.Windows.Controls.ComboBoxItem)CountryCodeComboBox.Items[i];
                if (item.Tag.ToString() == countryCode)
                {
                    CountryCodeComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// 預填手機號碼
        /// </summary>
        public void SetPhoneNumber(string phoneNumber)
        {
            PhoneTextBox.Text = phoneNumber;
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 清理計時器
            _countdownTimer?.Stop();
            _countdownTimer = null;
        }
    }
}
