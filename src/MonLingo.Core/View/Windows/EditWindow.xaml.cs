using System;
using System.Windows;
using MonLingo.Core.ViewModel;
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
            this.DialogResult = e.IsConfirmed;
            this.Close();
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
    }
}
