using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 翻譯結果顯示視窗 - 黑色透明背景
    /// </summary>
    public partial class TranslationResultWindow : Window
    {
        public TranslationResultWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // 設定視窗屬性
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;

            // 預設不可見
            this.Opacity = 0;
        }

        public void ShowTranslationResult(string originalText, string translatedText, Point position)
        {
            // 設定文本內容
            OriginalTextBlock.Text = originalText;
            TranslatedTextBlock.Text = translatedText;

            // 設定位置
            this.Left = position.X;
            this.Top = position.Y;

            // 確保視窗在螢幕範圍內
            EnsureWindowInScreen();

            // 顯示視窗並播放淡入動畫
            this.Show();
            PlayFadeInAnimation();
        }

        private void EnsureWindowInScreen()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (this.Left + this.Width > screenWidth)
                this.Left = screenWidth - this.Width - 20;
            
            if (this.Top + this.Height > screenHeight)
                this.Top = screenHeight - this.Height - 20;

            if (this.Left < 0) this.Left = 20;
            if (this.Top < 0) this.Top = 20;
        }

        private void PlayFadeInAnimation()
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(Window.OpacityProperty, fadeIn);
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnCopyOriginalClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(OriginalTextBlock.Text);
                ShowCopyFeedback("原文已複製");
            }
            catch
            {
                ShowCopyFeedback("複製失敗");
            }
        }

        private void OnCopyTranslationClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TranslatedTextBlock.Text);
                ShowCopyFeedback("翻譯已複製");
            }
            catch
            {
                ShowCopyFeedback("複製失敗");
            }
        }

        private void ShowCopyFeedback(string message)
        {
            FeedbackTextBlock.Text = message;
            FeedbackTextBlock.Visibility = Visibility.Visible;

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(2000),
                BeginTime = TimeSpan.FromMilliseconds(500)
            };

            fadeOut.Completed += (s, e) =>
            {
                FeedbackTextBlock.Visibility = Visibility.Collapsed;
            };

            FeedbackTextBlock.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void OnWindowMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
