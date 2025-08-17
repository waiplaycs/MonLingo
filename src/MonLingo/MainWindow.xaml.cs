using System.Windows;

namespace MonLingo
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        /// <summary>
        /// 初始化視窗.
        /// </summary>
        private void InitializeWindow()
        {
            // TODO: 設定視窗圖示
            // TODO: 載入使用者設定
            // TODO: 初始化翻譯引擎狀態
            EngineStatusTextBlock.Text = "翻譯引擎: 正在初始化...";
            StatusTextBlock.Text = "應用程式啟動完成";
        }

        /// <summary>
        /// 視窗載入完成事件.
        /// </summary>
        /// <param name="sender">事件發送者.</param>
        /// <param name="e">事件參數.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO: 註冊全域熱鍵
            // TODO: 檢查翻譯引擎可用性
            EngineStatusTextBlock.Text = "翻譯引擎: 就緒";
        }

        /// <summary>
        /// 視窗關閉事件.
        /// </summary>
        /// <param name="sender">事件發送者.</param>
        /// <param name="e">事件參數.</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // TODO: 保存使用者設定
            // TODO: 清理資源
            // TODO: 取消註冊熱鍵
        }
    }
}
