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
            // 儲存設定並關閉視窗
            ApplySettings();
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 不儲存設定直接關閉
            this.DialogResult = false;
            this.Close();
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
