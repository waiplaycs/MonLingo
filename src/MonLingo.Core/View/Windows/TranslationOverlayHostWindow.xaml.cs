using System;
using System.Windows;
using System.Windows.Media.Animation;
using NLog;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 翻譯覆蓋宿主視窗 - 主要的翻譯框，包含所有翻譯文字塊
    /// 根據 Gaminik 覆蓋模式設計實現
    /// </summary>
    public partial class TranslationOverlayHostWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// 當宿主視窗關閉時觸發，用於通知OverlayDisplayManager清理所有子視窗
        /// </summary>
        public event EventHandler HostWindowClosing;
        
        public TranslationOverlayHostWindow()
        {
            InitializeComponent();
            Logger.Info("[TranslationOverlayHost] 翻譯框已創建");
        }
        
        /// <summary>
        /// 設置翻譯框的位置和大小
        /// </summary>
        /// <param name="left">左邊位置</param>
        /// <param name="top">頂部位置</param>
        /// <param name="width">寬度</param>
        /// <param name="height">高度</param>
        public void SetBounds(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Logger.Info($"[TranslationOverlayHost] 設置翻譯框邊界: {left},{top} {width}x{height}");
        }
        
        /// <summary>
        /// 處理翻譯框點擊事件 - 點擊後關閉整個覆蓋系統
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Logger.Info("[TranslationOverlayHost] 翻譯框被點擊，開始關閉覆蓋系統");
            CloseWithAnimation();
        }
        
        /// <summary>
        /// 淡出並關閉翻譯框
        /// </summary>
        public void CloseWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) => 
            {
                // 通知OverlayDisplayManager清理所有子視窗
                HostWindowClosing?.Invoke(this, EventArgs.Empty);
                Close();
            };
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
