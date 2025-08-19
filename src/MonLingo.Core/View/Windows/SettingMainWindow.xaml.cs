using System;
using System.Windows;

namespace MonLingo.View.Windows
{
    /// <summary>
    /// SettingMainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class SettingMainWindow : Window
    {
        public SettingMainWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 儲存設定並關閉視窗
                ApplySettings();
                
                // 安全地關閉視窗（不設定 DialogResult，因為不是 ShowDialog 方式打開）
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存設定時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 不儲存設定直接關閉
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"關閉視窗時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // 僅套用設定，不關閉視窗
            ApplySettings();
        }

        private void ApplySettings()
        {
            // 這裡實作設定儲存邏輯
            // 可以透過 ViewModel 或服務來處理
        }
    }
}
