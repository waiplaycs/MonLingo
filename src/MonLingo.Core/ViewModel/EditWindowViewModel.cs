using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading.Tasks;
using NLog;

namespace MonLingo.Core.ViewModel
{
    /// <summary>
    /// 編輯窗口關閉事件參數
    /// </summary>
    public class EditWindowCloseEventArgs : EventArgs
    {
        public bool IsConfirmed { get; set; }
    }

    /// <summary>
    /// 編輯窗口 ViewModel
    /// </summary>
    public class EditWindowViewModel : INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private string _originalText;
        private string _translatedText;
        private string _sourceLanguage;
        private string _targetLanguage;
        private bool _isTranslating;

        public EditWindowViewModel(string originalText, string translatedText, string sourceLanguage, string targetLanguage)
        {
            _originalText = originalText ?? string.Empty;
            _translatedText = translatedText ?? string.Empty;
            _sourceLanguage = sourceLanguage ?? "中文(簡體)";
            _targetLanguage = targetLanguage ?? "中文(繁體)";

            Logger.Info($"EditWindowViewModel 初始化 - {_sourceLanguage} → {_targetLanguage}");
        }

        #region 屬性

        public string OriginalText
        {
            get => _originalText;
            set
            {
                _originalText = value;
                OnPropertyChanged(nameof(OriginalText));
            }
        }

        public string TranslatedText
        {
            get => _translatedText;
            set
            {
                _translatedText = value;
                OnPropertyChanged(nameof(TranslatedText));
            }
        }

        public string SourceLanguage
        {
            get => _sourceLanguage;
            set
            {
                _sourceLanguage = value;
                OnPropertyChanged(nameof(SourceLanguage));
            }
        }

        public string TargetLanguage
        {
            get => _targetLanguage;
            set
            {
                _targetLanguage = value;
                OnPropertyChanged(nameof(TargetLanguage));
            }
        }

        public bool IsTranslating
        {
            get => _isTranslating;
            set
            {
                _isTranslating = value;
                OnPropertyChanged(nameof(IsTranslating));
                OnPropertyChanged(nameof(CanTranslate));
            }
        }

        public bool CanTranslate => !IsTranslating && !string.IsNullOrWhiteSpace(OriginalText);

        #endregion

        #region 命令

        private ICommand _translateCommand;
        public ICommand TranslateCommand =>
            _translateCommand ??= new AsyncRelayCommand(ExecuteTranslateAsync, () => CanTranslate);

        private ICommand _confirmCommand;
        public ICommand ConfirmCommand =>
            _confirmCommand ??= new RelayCommand(() =>
            {
                Logger.Info("編輯窗口 - 確定");
                WindowCloseRequested?.Invoke(this, new EditWindowCloseEventArgs { IsConfirmed = true });
            });

        private ICommand _cancelCommand;
        public ICommand CancelCommand =>
            _cancelCommand ??= new RelayCommand(() =>
            {
                Logger.Info("編輯窗口 - 取消");
                WindowCloseRequested?.Invoke(this, new EditWindowCloseEventArgs { IsConfirmed = false });
            });

        private ICommand _copyOriginalTextCommand;
        public ICommand CopyOriginalTextCommand =>
            _copyOriginalTextCommand ??= new RelayCommand(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(OriginalText))
                    {
                        System.Windows.Clipboard.SetText(OriginalText);
                        Logger.Info("原文已複製到剪貼簿");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "複製原文失敗");
                }
            });

        private ICommand _copyTranslatedTextCommand;
        public ICommand CopyTranslatedTextCommand =>
            _copyTranslatedTextCommand ??= new RelayCommand(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(TranslatedText))
                    {
                        System.Windows.Clipboard.SetText(TranslatedText);
                        Logger.Info("譯文已複製到剪貼簿");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "複製譯文失敗");
                }
            });

        #endregion

        #region 事件

        public event EventHandler<EditWindowCloseEventArgs> WindowCloseRequested;
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region 方法

        private async Task ExecuteTranslateAsync()
        {
            if (string.IsNullOrWhiteSpace(OriginalText))
            {
                Logger.Warn("原文為空，無法翻譯");
                return;
            }

            try
            {
                IsTranslating = true;
                Logger.Info($"開始翻譯: {OriginalText.Substring(0, Math.Min(50, OriginalText.Length))}...");

                // 使用翻譯服務
                try
                {
                    var translateService = new MonLingo.Core.Service.TranslateService();
                    var result = await translateService.TranslateAsync(OriginalText, "zh-CN", "zh-TW");
                    if (!string.IsNullOrEmpty(result))
                    {
                        TranslatedText = result;
                        Logger.Info($"翻譯完成: {result.Substring(0, Math.Min(50, result.Length))}...");
                    }
                    else
                    {
                        Logger.Warn("翻譯服務返回空結果，使用模擬翻譯");
                        await Task.Delay(500); // 模擬翻譯延遲
                        TranslatedText = $"[翻譯]{OriginalText}";
                    }
                }
                catch (Exception serviceEx)
                {
                    Logger.Warn(serviceEx, "翻譯服務失敗，使用模擬翻譯");
                    // fallback 到模擬翻譯
                    await Task.Delay(500);
                    TranslatedText = $"[翻譯]{OriginalText}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "翻譯過程中發生錯誤");
                TranslatedText = "[翻譯失敗]";
            }
            finally
            {
                IsTranslating = false;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 簡單的同步 RelayCommand 實現
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }

    /// <summary>
    /// 支援異步操作的 RelayCommand
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        public async void Execute(object parameter)
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        private void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
