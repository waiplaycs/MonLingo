using System;
using System.Windows;
using MonLingo.Core.ViewModel;
using System.Linq;
using MonLingo.View.Windows;
using NLog;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 編輯翻譯窗口
    /// </summary>
    public partial class EditWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private EditWindowViewModel _viewModel;

    // 非模態確認事件：用於外部取得編輯結果
    public event EventHandler<EditConfirmedEventArgs> EditConfirmed;

        public EditWindow()
        {
            InitializeComponent();
            Logger.Info("EditWindow 初始化");
        }

        public EditWindow(string originalText, string translatedText, string sourceLanguage = "中文(簡體)", string targetLanguage = "中文(繁體)")
        {
            InitializeComponent();
            
            _viewModel = new EditWindowViewModel(originalText, translatedText, sourceLanguage, targetLanguage);
            _viewModel.WindowCloseRequested += OnWindowCloseRequested;
            this.DataContext = _viewModel;
            
            Logger.Info($"EditWindow 初始化完成 - 原文長度: {originalText?.Length ?? 0}, 譯文長度: {translatedText?.Length ?? 0}");
        }

        private void OnWindowCloseRequested(object sender, EditWindowCloseEventArgs e)
        {
            try
            {
                if (e.IsConfirmed)
                {
                    // 先對外發送確認事件（非模態場景使用）
                    var original = _viewModel?.OriginalText ?? string.Empty;
                    var translated = _viewModel?.TranslatedText ?? string.Empty;
                    EditConfirmed?.Invoke(this, new EditConfirmedEventArgs
                    {
                        OriginalText = original,
                        TranslatedText = translated
                    });
                }

                // 嘗試設定 DialogResult（若為模態顯示），非模態時會擲出例外，忽略即可
                try { this.DialogResult = e.IsConfirmed; } catch { /* ignore for modeless */ }
            }
            finally
            {
                this.Close();
            }
        }

        /// <summary>
        /// 獲取編輯結果
        /// </summary>
        public (string OriginalText, string TranslatedText) GetEditResult()
        {
            return (_viewModel?.OriginalText ?? string.Empty, _viewModel?.TranslatedText ?? string.Empty);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.WindowCloseRequested -= OnWindowCloseRequested;
                _viewModel = null;
            }
            base.OnClosed(e);
        }

        private void Header_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // 聚焦或開啟設定視窗，定位至翻譯語言
                var existing = Application.Current.Windows.OfType<SettingMainWindow>().FirstOrDefault();
                if (existing != null)
                {
                    if (existing.WindowState == WindowState.Minimized)
                        existing.WindowState = WindowState.Normal;
                    existing.Activate();
                    existing.Topmost = true; existing.Topmost = false;
                    existing.OpenTranslationLanguagePage();
                    return;
                }

                var settingsWindow = new SettingMainWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                settingsWindow.OpenTranslationLanguagePage();
                settingsWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "EditWindow header click failed to open settings");
            }
        }
    }

    public class EditConfirmedEventArgs : EventArgs
    {
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
    }
}
