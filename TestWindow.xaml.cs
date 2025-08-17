using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ReactToWpfToolbar
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 處理移動按鈕的滑鼠左鍵按下事件，以允許拖動視窗。
        /// </summary>
        private void MoveButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// 處理最小化按鈕的點擊事件。
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 處理開始翻譯按鈕的點擊事件，並觸發旋轉動畫。
        /// </summary>
        private void StartTranslateButton_Click(object sender, RoutedEventArgs e)
        {
            // 創建一個 DoubleAnimation 來旋轉按鈕
            var rotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1),
                // 可以添加 EasingFunction 來改變動畫的節奏
                // EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // 獲取按鈕的旋轉變換
            var rotateTransform = (System.Windows.Media.RotateTransform)StartTranslateButton.RenderTransform;

            // 開始動畫
            rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, rotationAnimation);

            // 在此處添加實際的翻譯邏輯
            System.Diagnostics.Debug.WriteLine("開始翻譯按鈕被點擊！");
        }
    }
}
