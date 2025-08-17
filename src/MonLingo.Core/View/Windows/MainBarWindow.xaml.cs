using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MonLingo.ViewModel;

namespace MonLingo.View.Windows
{
    /// <summary>
    /// MainBarWindow - 可拖曳的浮動主工具列
    /// 對應 Gaminik.View.MainBarWindow 的功能和設計
    /// </summary>
    public partial class MainBarWindow : Window
    {
        private MainBarWindowViewModel _viewModel;
        
        public MainBarWindow()
        {
            InitializeComponent();
            InitializeWindow();
            SetupViewModel();
        }

        private void InitializeWindow()
        {
            // 設置視窗屬性以實現浮動工具列效果
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;
            
            // 設置初始位置和大小
            this.Width = 400;
            this.Height = 60;
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = 50;
        }

        private void SetupViewModel()
        {
            _viewModel = new MainBarWindowViewModel();
            this.DataContext = _viewModel;
            
            // 訂閱ViewModel事件
            _viewModel.CaptureRequested += OnCaptureRequested;
            _viewModel.SettingsRequested += OnSettingsRequested;
            _viewModel.ExitRequested += OnExitRequested;
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
            var captureWindow = new CaptureRegionWindow();
            captureWindow.Show();
            
            // 臨時隱藏工具列以避免干擾擷取
            this.Hide();
        }

        private void OnSettingsRequested(object sender, EventArgs e)
        {
            // 打開設定視窗
            var settingsWindow = new SettingMainWindow();
            settingsWindow.Show();
        }

        private void OnExitRequested(object sender, EventArgs e)
        {
            // 安全關閉應用程式
            Application.Current.Shutdown();
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
