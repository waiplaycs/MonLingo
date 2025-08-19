using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Threading.Tasks;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// 工作版 MainBarWindowViewModel - 啟用基本按鈕功能
    /// </summary>
    public class WorkingMainBarWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region 顯示屬性

        private string _selectedEngine = "Google";
        public string SelectedEngine
        {
            get => _selectedEngine;
            set
            {
                _selectedEngine = value;
                OnPropertyChanged(nameof(SelectedEngine));
            }
        }

        private int _remainingTranslations = 999;
        public int RemainingTranslations
        {
            get => _remainingTranslations;
            set
            {
                _remainingTranslations = value;
                OnPropertyChanged(nameof(RemainingTranslations));
            }
        }

        private int _coinBalance = 1250;
        public int CoinBalance
        {
            get => _coinBalance;
            set
            {
                _coinBalance = value;
                OnPropertyChanged(nameof(CoinBalance));
            }
        }

        private string _sourceLanguage = "EN";
        public string SourceLanguage
        {
            get => _sourceLanguage;
            set
            {
                _sourceLanguage = value;
                OnPropertyChanged(nameof(SourceLanguage));
            }
        }

        private string _targetLanguage = "中文";
        public string TargetLanguage
        {
            get => _targetLanguage;
            set
            {
                _targetLanguage = value;
                OnPropertyChanged(nameof(TargetLanguage));
            }
        }

        private bool _isCoverModeEnabled = true;
        public bool IsCoverModeEnabled
        {
            get => _isCoverModeEnabled;
            set
            {
                _isCoverModeEnabled = value;
                OnPropertyChanged(nameof(IsCoverModeEnabled));
            }
        }

        private bool _isAutoTranslateEnabled = false;
        public bool IsAutoTranslateEnabled
        {
            get => _isAutoTranslateEnabled;
            set
            {
                _isAutoTranslateEnabled = value;
                OnPropertyChanged(nameof(IsAutoTranslateEnabled));
            }
        }

        private bool _isComicModeEnabled = false;
        public bool IsComicModeEnabled
        {
            get => _isComicModeEnabled;
            set
            {
                _isComicModeEnabled = value;
                OnPropertyChanged(nameof(IsComicModeEnabled));
            }
        }

        private bool _isCollapsed = false;
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                _isCollapsed = value;
                OnPropertyChanged(nameof(IsCollapsed));
            }
        }

        #endregion

        #region 命令

        // 1. 開始翻譯
        private ICommand _startTranslationCommand;
        public ICommand StartTranslationCommand =>
            _startTranslationCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("開始翻譯功能被點擊！\n狀態: 翻譯會話已啟動", "翻譯功能", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 6. 翻譯引擎對比
        private ICommand _compareEnginesCommand;
        public ICommand CompareEnginesCommand =>
            _compareEnginesCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("翻譯引擎對比功能被點擊！\n正在比較不同翻譯引擎的結果...", "引擎對比", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 8. 自定義詞典
        private ICommand _openDictionaryCommand;
        public ICommand OpenDictionaryCommand =>
            _openDictionaryCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("自定義詞典功能被點擊！\n正在打開詞典設定...", "詞典功能", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 9. 快速截圖翻譯
        private ICommand _quickScreenshotCommand;
        public ICommand QuickScreenshotCommand =>
            _quickScreenshotCommand ??= new AsyncRelayCommand(async () =>
            {
                try
                {
                    var quickTranslationService = new MonLingo.Core.Service.QuickTranslationService(null);
                    await quickTranslationService.StartQuickTranslationAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"快速翻譯出錯: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

        // 10. 翻譯區域選擇
        private ICommand _selectRegionCommand;
        public ICommand SelectRegionCommand =>
            _selectRegionCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("翻譯區域選擇功能被點擊！\n請在螢幕上選擇翻譯區域...", "區域選擇", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 11. 翻譯區域2選擇
        private ICommand _selectRegion2Command;
        public ICommand SelectRegion2Command =>
            _selectRegion2Command ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("翻譯區域2選擇功能被點擊！\n正在設定第二個翻譯區域...", "區域選擇2", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 12. 漫畫翻譯優化
        private ICommand _comicModeCommand;
        public ICommand ComicModeCommand =>
            _comicModeCommand ??= new SimpleRelayCommand(() =>
            {
                IsComicModeEnabled = !IsComicModeEnabled;
                MessageBox.Show($"漫畫翻譯模式: {(IsComicModeEnabled ? "已開啟" : "已關閉")}\n優化漫畫翻譯效果", "漫畫模式", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 15. 編輯窗口
        private ICommand _openEditorCommand;
        public ICommand OpenEditorCommand =>
            _openEditorCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("編輯窗口功能被點擊！\n正在打開文字編輯器...", "編輯器", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 16. 更多工具
        private ICommand _showMoreToolsCommand;
        public ICommand ShowMoreToolsCommand =>
            _showMoreToolsCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("更多工具功能被點擊！\n顯示額外工具選項...", "更多工具", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 17. 設置
        private ICommand _openSettingsCommand;
        public ICommand OpenSettingsCommand =>
            _openSettingsCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("設置功能被點擊！\n正在打開設定頁面...", "設置", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 18. 最小化
        private ICommand _minimizeCommand;
        public ICommand MinimizeCommand =>
            _minimizeCommand ??= new SimpleRelayCommand(() =>
            {
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
                MessageBox.Show("工具條已最小化", "最小化", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 19. 關閉
        private ICommand _exitCommand;
        public ICommand ExitCommand =>
            _exitCommand ??= new SimpleRelayCommand(() =>
            {
                if (MessageBox.Show("確定要退出 MonLingo 嗎？", "退出確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            });

        // 20. 收縮工具條
        private ICommand _collapseCommand;
        public ICommand CollapseCommand =>
            _collapseCommand ??= new SimpleRelayCommand(() =>
            {
                IsCollapsed = !IsCollapsed;
                MessageBox.Show($"工具條收縮狀態: {(IsCollapsed ? "已收縮" : "已展開")}", "收縮切換", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 簡單的 RelayCommand 實現，避免衝突
    /// </summary>
    public class SimpleRelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public SimpleRelayCommand(Action execute, Func<bool> canExecute = null)
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
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// 支援異步操作的RelayCommand
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
