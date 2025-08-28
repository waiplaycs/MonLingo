using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MonLingo.Core.Service;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Models;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 語言設定對話框
    /// 提供簡潔的語言配置界面，支持實時儲存用戶偏好
    /// </summary>
    public partial class LanguageSettingsWindow : Window
    {
        private ILanguageConfigService _languageConfigService;
        private ComboBox _sourceLanguageCombo;
        private ComboBox _targetLanguageCombo;
        private TextBlock _statusText;

        public LanguageSettingsWindow()
        {
            InitializeComponent();
            InitializeLanguageConfigService();
        }

        /// <summary>
        /// 初始化語言配置服務
        /// </summary>
        private void InitializeLanguageConfigService()
        {
            try
            {
                _languageConfigService = Phase5ServiceContainer.GetService<ILanguageConfigService>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"語言配置服務初始化失敗: {ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 窗口載入事件
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeUIAsync();
            await LoadCurrentSettingsAsync();
        }

        /// <summary>
        /// 初始化用戶界面
        /// </summary>
        private async Task InitializeUIAsync()
        {
            // 設定窗口基本屬性
            Title = "翻譯語言設定";
            Width = 500;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            // 創建主容器
            var mainStack = new StackPanel
            {
                Margin = new Thickness(20),
                Background = System.Windows.Media.Brushes.White
            };

            // 添加標題
            var titleText = new TextBlock
            {
                Text = "🌏 翻譯語言設定",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            mainStack.Children.Add(titleText);

            // 源語言選擇區域
            var sourceGroup = new GroupBox
            {
                Header = "📥 輸入語言 (OCR 識別)",
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(10)
            };

            _sourceLanguageCombo = new ComboBox
            {
                Height = 30,
                Margin = new Thickness(5)
            };

            // 填充源語言選項
            foreach (var lang in LanguageSettings.SourceLanguages.Values)
            {
                _sourceLanguageCombo.Items.Add(lang);
            }

            sourceGroup.Content = _sourceLanguageCombo;
            mainStack.Children.Add(sourceGroup);

            // 目標語言選擇區域
            var targetGroup = new GroupBox
            {
                Header = "📤 輸出語言 (翻譯結果)",
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(10)
            };

            _targetLanguageCombo = new ComboBox
            {
                Height = 30,
                Margin = new Thickness(5)
            };

            // 填充目標語言選項
            foreach (var lang in LanguageSettings.TargetLanguages.Values)
            {
                _targetLanguageCombo.Items.Add(lang);
            }

            targetGroup.Content = _targetLanguageCombo;
            mainStack.Children.Add(targetGroup);

            // 狀態文字
            _statusText = new TextBlock
            {
                Text = "就緒",
                Foreground = System.Windows.Media.Brushes.Green,
                Margin = new Thickness(0, 10, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            mainStack.Children.Add(_statusText);

            // 按鈕區域
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var saveButton = new Button
            {
                Content = "💾 儲存設定",
                Width = 100,
                Height = 35,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.LightBlue
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Content = "❌ 取消",
                Width = 80,
                Height = 35,
                Margin = new Thickness(5)
            };
            cancelButton.Click += (s, e) => Close();

            var resetButton = new Button
            {
                Content = "🔄 重設",
                Width = 80,
                Height = 35,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.LightYellow
            };
            resetButton.Click += ResetButton_Click;

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(resetButton);
            buttonPanel.Children.Add(cancelButton);
            mainStack.Children.Add(buttonPanel);

            // 綁定變更事件
            _sourceLanguageCombo.SelectionChanged += LanguageCombo_SelectionChanged;
            _targetLanguageCombo.SelectionChanged += LanguageCombo_SelectionChanged;

            Content = mainStack;
        }

        /// <summary>
        /// 載入當前設定
        /// </summary>
        private async Task LoadCurrentSettingsAsync()
        {
            if (_languageConfigService == null) return;

            try
            {
                _statusText.Text = "載入設定中...";
                _statusText.Foreground = System.Windows.Media.Brushes.Orange;

                var config = await _languageConfigService.GetLanguageConfigAsync();

                // 設定源語言
                var sourceDisplayName = LanguageSettings.GetDisplayNameByLanguageCode(config.SourceLanguage, true);
                _sourceLanguageCombo.SelectedItem = sourceDisplayName;

                // 設定目標語言
                var targetDisplayName = LanguageSettings.GetDisplayNameByLanguageCode(config.TargetLanguage, false);
                _targetLanguageCombo.SelectedItem = targetDisplayName;

                _statusText.Text = $"已載入設定: {sourceDisplayName} → {targetDisplayName}";
                _statusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                _statusText.Text = $"載入失敗: {ex.Message}";
                _statusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        /// <summary>
        /// 語言選擇變更事件
        /// </summary>
        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        /// <summary>
        /// 儲存按鈕點擊事件
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_languageConfigService == null)
            {
                MessageBox.Show("語言配置服務不可用", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _statusText.Text = "儲存設定中...";
                _statusText.Foreground = System.Windows.Media.Brushes.Orange;

                var sourceDisplayName = _sourceLanguageCombo.SelectedItem?.ToString();
                var targetDisplayName = _targetLanguageCombo.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(sourceDisplayName) || string.IsNullOrEmpty(targetDisplayName))
                {
                    MessageBox.Show("請選擇輸入語言和輸出語言", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 將顯示名稱轉換為語言代碼
                var sourceCode = LanguageSettings.GetLanguageCodeByDisplayName(sourceDisplayName, true);
                var targetCode = LanguageSettings.GetLanguageCodeByDisplayName(targetDisplayName, false);

                // 儲存到語言配置服務
                await _languageConfigService.SetSourceLanguageAsync(sourceCode);
                await _languageConfigService.SetTargetLanguageAsync(targetCode);

                _statusText.Text = "✅ 設定已儲存";
                _statusText.Foreground = System.Windows.Media.Brushes.Green;

                MessageBox.Show("語言設定已成功儲存！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _statusText.Text = $"儲存失敗: {ex.Message}";
                _statusText.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重設按鈕點擊事件
        /// </summary>
        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("確定要重設為預設語言設定嗎？", "確認", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _statusText.Text = "重設中...";
                    _statusText.Foreground = System.Windows.Media.Brushes.Orange;

                    if (_languageConfigService != null)
                    {
                        await _languageConfigService.ResetToDefaultAsync();
                    }

                    await LoadCurrentSettingsAsync();
                }
                catch (Exception ex)
                {
                    _statusText.Text = $"重設失敗: {ex.Message}";
                    _statusText.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show($"重設失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 更新狀態文字
        /// </summary>
        private void UpdateStatus()
        {
            var sourceDisplayName = _sourceLanguageCombo.SelectedItem?.ToString();
            var targetDisplayName = _targetLanguageCombo.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(sourceDisplayName) && !string.IsNullOrEmpty(targetDisplayName))
            {
                _statusText.Text = $"當前選擇: {sourceDisplayName} → {targetDisplayName}";
                _statusText.Foreground = System.Windows.Media.Brushes.Blue;
            }
        }
    }
}
