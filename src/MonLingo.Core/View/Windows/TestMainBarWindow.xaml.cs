using System;
using System.Windows;

namespace MonLingo.View.Windows
{
    public partial class TestMainBarWindow : Window
    {
        public TestMainBarWindow()
        {
            try
            {
                MessageBox.Show("開始初始化測試視窗", "調試");
                InitializeComponent();
                MessageBox.Show("測試視窗初始化完成", "調試");
                
                // 設置位置
                this.Left = 100;
                this.Top = 100;
                
                MessageBox.Show("測試視窗設置完成", "調試");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"測試視窗初始化失敗:\n{ex.Message}\n\n詳細:\n{ex.ToString()}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("測試按鈕被點擊了！", "成功");
        }
    }
}
