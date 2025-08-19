using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using MonLingo.ViewModel;

namespace MonLingo.View.Windows
{
    /// <summary>
    /// MainBarWindow - 可拖曳的浮動主工具列
    /// 對應 Gaminik.View.MainBarWindow 的功能和設計
    /// 基於 TestWindow.xaml 的現代化 UI 設計
    /// </summary>
    public partial class MainBarWindow : Window
    {
        private MainBarWindowViewModel _viewModel;
        
        public MainBarWindow()
        {
            try
            {
                InitializeComponent();
                InitializeWindow();
                SetupViewModel();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"MainBarWindow 初始化失敗:\n{ex.Message}\n\n詳細:\n{ex.ToString()}", "錯誤", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        private void InitializeWindow()
        {
            // 設置視窗屬性以實現浮動工具列效果
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;
            
            // 使用新的現代化尺寸
            this.Height = 52;
            this.Width = 1050;
            
            // 設置初始位置 (螢幕上方中央)
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = 50;
        }

        private void SetupViewModel()
        {
            // 使用工作版 ViewModel 啟用按鈕功能
            var workingViewModel = new WorkingMainBarWindowViewModel();
            this.DataContext = workingViewModel;
        }

        /// <summary>
        /// 處理移動按鈕的滑鼠左鍵按下事件，允許拖動視窗
        /// </summary>
        private void MoveButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        #region 視窗拖曳功能
        
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        #endregion

        #region ViewModel事件處理

        private void OnCaptureRequested(object sender, EventArgs e)
        {
            // 觸發螢幕擷取功能
            // 這裡將會打開 CaptureRegionWindow
            // var captureWindow = new CaptureRegionWindow(); // 暫時註解 - Phase6 測試
            // captureWindow.Show();
            
            // 臨時隱藏工具列以避免干擾擷取
            this.Hide();
        }

        private void OnSettingsRequested(object sender, EventArgs e)
        {
            try
            {
                // 打開設定視窗
                var settingsWindow = new SettingMainWindow();
                settingsWindow.Owner = this; // 設定父視窗
                settingsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"無法打開設定視窗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExitRequested(object sender, EventArgs e)
        {
            // 安全關閉應用程式
            Application.Current.Shutdown();
        }

        private void OnMinimizeRequested(object sender, EventArgs e)
        {
            // 最小化工具條到系統托盤
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        #endregion

        #region 視窗生命週期

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 註冊全域熱鍵 (透過 HotKeyService)
            RegisterGlobalHotKeys();
        }

        private void RegisterGlobalHotKeys()
        {
            try
            {
                // 註冊 Ctrl+Q 熱鍵觸發螢幕翻譯
                var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                if (hwndSource != null)
                {
                    // 這裡將整合 HotKeyService
                    // HotKeyService.RegisterHotKey(hwndSource.Handle, 1, ModifierKeys.Control, Key.Q);
                }
            }
            catch (Exception ex)
            {
                // 記錄熱鍵註冊失敗
                System.Diagnostics.Debug.WriteLine($"熱鍵註冊失敗: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理資源
            if (_viewModel != null)
            {
                _viewModel.CaptureRequested -= OnCaptureRequested;
                _viewModel.SettingsRequested -= OnSettingsRequested;
                _viewModel.ExitRequested -= OnExitRequested;
                _viewModel.MinimizeRequested -= OnMinimizeRequested;
                _viewModel.Dispose();
            }
            
            base.OnClosed(e);
        }

        #endregion

        #region 視窗動畫和視覺效果

        /// <summary>
        /// 顯示工具列時的淡入動畫
        /// </summary>
        public void ShowWithAnimation()
        {
            this.Opacity = 0;
            this.Show();
            
            // 使用 Storyboard 實現淡入效果
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, 
                TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        /// <summary>
        /// 隱藏工具列時的淡出動畫
        /// </summary>
        public void HideWithAnimation()
        {
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, 
                TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => this.Hide();
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        #endregion
    }
}
