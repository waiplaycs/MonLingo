using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Threading.Tasks;
using NLog;
using MonLingo.View.Windows;
using MonLingo.Core.View.Windows;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// 工作版 MainBarWindowViewModel - 啟用基本按鈕功能
    /// </summary>
    public class WorkingMainBarWindowViewModel : INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Window _mainBarWindow;
        
        public WorkingMainBarWindowViewModel(Window mainBarWindow = null)
        {
            Logger.Info("🔧 WorkingMainBarWindowViewModel 建構函數開始");
            Logger.Debug($"📋 傳入的主視窗: {(mainBarWindow != null ? mainBarWindow.GetType().Name : "null")}");
            _mainBarWindow = mainBarWindow;
            Logger.Info("✅ WorkingMainBarWindowViewModel 建構完成");
        }

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

        // 2. 升級到PRO
        private ICommand _upgradeToProCommand;
        public ICommand UpgradeToProCommand =>
            _upgradeToProCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("升級到PRO功能被點擊！\n正在打開升級頁面...", "升級PRO", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 3. 每日簽到
        private ICommand _dailyCheckInCommand;
        public ICommand DailyCheckInCommand =>
            _dailyCheckInCommand ??= new SimpleRelayCommand(() =>
            {
                RemainingTranslations += 10; // 模擬獲得翻譯次數
                MessageBox.Show($"每日簽到完成！\n獲得 10 次翻譯機會\n當前剩餘: {RemainingTranslations} 次", "每日簽到", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 4. 購買硬幣
        private ICommand _purchaseCoinsCommand;
        public ICommand PurchaseCoinsCommand =>
            _purchaseCoinsCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("購買硬幣功能被點擊！\n正在打開硬幣商店...", "硬幣商店", MessageBoxButton.OK, MessageBoxImage.Information);
            });

        // 5. 語言設置
        private ICommand _languageSettingsCommand;
        public ICommand LanguageSettingsCommand =>
            _languageSettingsCommand ??= new SimpleRelayCommand(() =>
            {
                MessageBox.Show("語言設置功能被點擊！\n正在打開語言設定...", "語言設置", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private static MonLingo.Core.Service.QuickTranslationService _globalQuickTranslationService;
        
        public ICommand QuickScreenshotCommand =>
            _quickScreenshotCommand ??= new AsyncRelayCommand(async () =>
            {
                Logger.Info("🖱️ QuickScreenshotCommand 被觸發");
                Logger.Debug($"📋 命令執行時狀態: _mainBarWindow={((_mainBarWindow == null) ? "null" : _mainBarWindow.GetType().Name)}, _globalQuickTranslationService={((_globalQuickTranslationService == null) ? "null" : "已存在")}");
                
                try
                {
                    // 使用靜態服務實例，確保字幕視窗可以重複使用
                    if (_globalQuickTranslationService == null)
                    {
                        Logger.Info("🔨 創建新的 QuickTranslationService 實例");
                        Logger.Debug($"📋 傳入主視窗: {((_mainBarWindow == null) ? "null" : _mainBarWindow.GetType().Name)}");
                        _globalQuickTranslationService = new MonLingo.Core.Service.QuickTranslationService(_mainBarWindow);
                        Logger.Info("✅ QuickTranslationService 實例創建完成");
                    }
                    else
                    {
                        Logger.Info("♻️ 重複使用現有的 QuickTranslationService 實例");
                    }
                    
                    Logger.Info("🚀 開始執行快速翻譯");
                    await _globalQuickTranslationService.StartQuickTranslationAsync();
                    Logger.Info("✅ 快速翻譯執行完成");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "❌ 快速翻譯過程中發生錯誤");
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
                try
                {
                    Logger.Info("⚙️ 設定按鈕被點擊，正在打開設定頁面...");
                    
                    // 獲取主窗口參考以管理 Topmost 狀態
                    var mainWindow = Application.Current.MainWindow as MainBarWindow;
                    if (mainWindow != null)
                    {
                        // 暫時禁用工具條的 Topmost 設定，避免與設置窗口衝突
                        mainWindow.Topmost = false;
                        Logger.Info("🔽 暫時禁用工具條 Topmost 設定");
                    }
                    
                    var settingsWindow = new SettingMainWindow();
                    Logger.Debug("✅ SettingMainWindow 實例已創建");
                    
                    // 當設置窗口關閉時，恢復工具條的 Topmost 設定
                    settingsWindow.Closed += (s, args) =>
                    {
                        if (mainWindow != null)
                        {
                            mainWindow.Topmost = true;
                            Logger.Info("🔼 恢復工具條 Topmost 設定");
                        }
                    };
                    
                    // 使用 Show() 而不是 ShowDialog() 以避免阻塞工具條
                    settingsWindow.Show();
                    Logger.Info("✅ 設定頁面已顯示，工具條保持可用");
                }
                catch (Exception ex)
                {
                    // 發生錯誤時也要恢復 Topmost 設定
                    var mainWindow = Application.Current.MainWindow as MainBarWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Topmost = true;
                    }
                    Logger.Error(ex, "❌ 打開設定頁面時發生錯誤");
                    MessageBox.Show($"設置功能無法載入！\n錯誤詳情：{ex.Message}", "設置", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                
                // 觸發收縮切換事件，讓 View 處理實際的收縮邏輯
                CollapseRequested?.Invoke(this, new CollapseEventArgs { IsCollapsed = IsCollapsed });
            });

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Window Events
        
        /// <summary>
        /// 截圖請求事件
        /// </summary>
        public event EventHandler CaptureRequested;
        
        /// <summary>
        /// 設定請求事件  
        /// </summary>
        public event EventHandler SettingsRequested;
        
        /// <summary>
        /// 退出請求事件
        /// </summary>
        public event EventHandler ExitRequested;
        
        /// <summary>
        /// 最小化請求事件
        /// </summary>
        public event EventHandler MinimizeRequested;
        
        /// <summary>
        /// 收縮切換請求事件
        /// </summary>
        public event EventHandler<CollapseEventArgs> CollapseRequested;

        protected virtual void OnCaptureRequested()
        {
            CaptureRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSettingsRequested()
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnExitRequested()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMinimizeRequested()
        {
            MinimizeRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion
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
