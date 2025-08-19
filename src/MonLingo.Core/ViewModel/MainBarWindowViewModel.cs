using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MonLingo.Core.Commands;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Service;
using MonLingo.Core.Models;
using System.Threading.Tasks;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// MainBarWindow 的 ViewModel
    /// 負責處理主工具列的所有業務邏輯和使用者互動
    /// </summary>
    public class MainBarWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool _isDisposed = false;
        
        // 服務引用
        private readonly ISimpleEventAggregator _eventAggregator;
        private readonly ISimpleNotificationService _notificationService;
        private readonly UITranslationBridge _translationBridge;
        
        // Phase 6: 商業化服務
        private readonly IUserService _userService;
        private readonly ILicenseService _licenseService;
        private readonly ICurrencyService _currencyService;
        private readonly IPointsService _pointsService;
        private readonly ICoinsService _coinsService;

        #region 屬性

        private bool _isTranslationActive;
        /// <summary>
        /// 是否正在進行翻譯
        /// </summary>
        public bool IsTranslationActive
        {
            get => _isTranslationActive;
            set => SetProperty(ref _isTranslationActive, value);
        }

        private bool _isAudioCaptureActive;
        /// <summary>
        /// 是否正在進行音訊擷取
        /// </summary>
        public bool IsAudioCaptureActive
        {
            get => _isAudioCaptureActive;
            set => SetProperty(ref _isAudioCaptureActive, value);
        }

        private string _statusText = "就緒";
        /// <summary>
        /// 狀態文字
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _remainingTranslations = "∞";
        /// <summary>
        /// 剩餘翻譯點數
        /// </summary>
        public string RemainingTranslations
        {
            get => _remainingTranslations;
            set => SetProperty(ref _remainingTranslations, value);
        }

        private string _coinBalance = "1,250";
        /// <summary>
        /// 硬幣餘額
        /// </summary>
        public string CoinBalance
        {
            get => _coinBalance;
            set => SetProperty(ref _coinBalance, value);
        }

        private string _selectedEngine = "Google";
        /// <summary>
        /// 選定的翻譯引擎
        /// </summary>
        public string SelectedEngine
        {
            get => _selectedEngine;
            set => SetProperty(ref _selectedEngine, value);
        }

        private string _sourceLanguage = "EN";
        /// <summary>
        /// 來源語言
        /// </summary>
        public string SourceLanguage
        {
            get => _sourceLanguage;
            set => SetProperty(ref _sourceLanguage, value);
        }

        private string _targetLanguage = "中文";
        /// <summary>
        /// 目標語言
        /// </summary>
        public string TargetLanguage
        {
            get => _targetLanguage;
            set => SetProperty(ref _targetLanguage, value);
        }

        private bool _isCoverModeEnabled = true;
        /// <summary>
        /// 是否啟用覆蓋模式
        /// </summary>
        public bool IsCoverModeEnabled
        {
            get => _isCoverModeEnabled;
            set => SetProperty(ref _isCoverModeEnabled, value);
        }

        private bool _isAutoTranslateEnabled = false;
        /// <summary>
        /// 是否啟用自動翻譯
        /// </summary>
        public bool IsAutoTranslateEnabled
        {
            get => _isAutoTranslateEnabled;
            set => SetProperty(ref _isAutoTranslateEnabled, value);
        }

        private bool _isComicModeEnabled = false;
        /// <summary>
        /// 是否啟用漫畫模式
        /// </summary>
        public bool IsComicModeEnabled
        {
            get => _isComicModeEnabled;
            set => SetProperty(ref _isComicModeEnabled, value);
        }

        private bool _isCollapsed = false;
        /// <summary>
        /// 工具列是否收縮
        /// </summary>
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set => SetProperty(ref _isCollapsed, value);
        }

        // Phase 6: 商業化相關屬性
        
        private bool _isLoggedIn = false;
        /// <summary>
        /// 是否已登入
        /// </summary>
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        private bool _isProMember = false;
        /// <summary>
        /// 是否為Pro會員
        /// </summary>
        public bool IsProMember
        {
            get => _isProMember;
            set => SetProperty(ref _isProMember, value);
        }

        private string _username = "遊客";
        /// <summary>
        /// 使用者名稱
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private int _pointsBalance = 0;
        /// <summary>
        /// 積分餘額
        /// </summary>
        public int PointsBalance
        {
            get => _pointsBalance;
            set => SetProperty(ref _pointsBalance, value);
        }

        private int _coinsBalanceInt = 0;
        /// <summary>
        /// 硬幣餘額（整數）
        /// </summary>
        public int CoinsBalanceInt
        {
            get => _coinsBalanceInt;
            set 
            { 
                SetProperty(ref _coinsBalanceInt, value);
                CoinBalance = value.ToString("N0"); // 更新格式化的字串
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 開始翻譯命令
        /// </summary>
        public ICommand StartTranslationCommand { get; private set; }

        /// <summary>
        /// 螢幕翻譯命令
        /// </summary>
        public ICommand CaptureCommand { get; private set; }

        /// <summary>
        /// 音訊轉錄命令
        /// </summary>
        public ICommand AudioCaptureCommand { get; private set; }

        /// <summary>
        /// 歷史記錄命令
        /// </summary>
        public ICommand HistoryCommand { get; private set; }

        /// <summary>
        /// 設定命令
        /// </summary>
        public ICommand SettingsCommand { get; private set; }

        /// <summary>
        /// 最小化命令
        /// </summary>
        public ICommand MinimizeCommand { get; private set; }

        /// <summary>
        /// 退出命令
        /// </summary>
        public ICommand ExitCommand { get; private set; }

        /// <summary>
        /// 引擎對比命令
        /// </summary>
        public ICommand CompareEnginesCommand { get; private set; }

        /// <summary>
        /// 打開詞典命令
        /// </summary>
        public ICommand OpenDictionaryCommand { get; private set; }

        /// <summary>
        /// 快速截圖命令
        /// </summary>
        public ICommand QuickScreenshotCommand { get; private set; }

        /// <summary>
        /// 選擇區域命令
        /// </summary>
        public ICommand SelectRegionCommand { get; private set; }

        /// <summary>
        /// 選擇區域2命令
        /// </summary>
        public ICommand SelectRegion2Command { get; private set; }

        /// <summary>
        /// 漫畫模式命令
        /// </summary>
        public ICommand ComicModeCommand { get; private set; }

        /// <summary>
        /// 打開編輯器命令
        /// </summary>
        public ICommand OpenEditorCommand { get; private set; }

        /// <summary>
        /// 顯示更多工具命令
        /// </summary>
        public ICommand ShowMoreToolsCommand { get; private set; }

        /// <summary>
        /// 打開設定命令
        /// </summary>
        public ICommand OpenSettingsCommand { get; private set; }

        /// <summary>
        /// 收縮命令
        /// </summary>
        public ICommand CollapseCommand { get; private set; }

        // Phase 6: 商業化相關命令

        /// <summary>
        /// 登入命令
        /// </summary>
        public ICommand LoginCommand { get; private set; }

        /// <summary>
        /// 登出命令
        /// </summary>
        public ICommand LogoutCommand { get; private set; }

        /// <summary>
        /// 購買硬幣命令
        /// </summary>
        public ICommand PurchaseCoinsCommand { get; private set; }

        /// <summary>
        /// 每日簽到命令
        /// </summary>
        public ICommand DailyCheckInCommand { get; private set; }

        /// <summary>
        /// 觀看廣告獲取積分命令
        /// </summary>
        public ICommand WatchAdCommand { get; private set; }

        /// <summary>
        /// 分享獲取積分命令
        /// </summary>
        public ICommand ShareAppCommand { get; private set; }

        /// <summary>
        /// 查看會員資訊命令
        /// </summary>
        public ICommand ViewMemberInfoCommand { get; private set; }

        /// <summary>
        /// 升級Pro會員命令
        /// </summary>
        public ICommand UpgradeToProCommand { get; private set; }

        #endregion

        #region 事件

        /// <summary>
        /// 請求開始螢幕擷取事件
        /// </summary>
        public event EventHandler CaptureRequested;

        /// <summary>
        /// 請求開始音訊擷取事件
        /// </summary>
        public event EventHandler AudioCaptureRequested;

        /// <summary>
        /// 請求打開歷史記錄事件
        /// </summary>
        public event EventHandler HistoryRequested;

        /// <summary>
        /// 請求打開設定事件
        /// </summary>
        public event EventHandler SettingsRequested;

        /// <summary>
        /// 請求最小化事件
        /// </summary>
        public event EventHandler MinimizeRequested;

        /// <summary>
        /// 請求退出事件
        /// </summary>
        public event EventHandler ExitRequested;

        /// <summary>
        /// 詞典請求事件
        /// </summary>
        public event EventHandler DictionaryRequested;

        /// <summary>
        /// 更多工具請求事件
        /// </summary>
        public event EventHandler MoreToolsRequested;

        /// <summary>
        /// 收縮請求事件
        /// </summary>
        public event EventHandler<CollapseEventArgs> CollapseRequested;

        #endregion

        #region 建構函式

        public MainBarWindowViewModel()
        {
            try
            {
                // 初始化 Phase 5 服務容器
                Phase5ServiceContainer.Initialize();
                
                // 建立服務容器實例（原有的）
                var serviceContainer = new SimpleServiceContainer();
                serviceContainer.Initialize();
                
                // 獲取服務實例
                _eventAggregator = serviceContainer.GetService<ISimpleEventAggregator>();
                _notificationService = serviceContainer.GetService<ISimpleNotificationService>();
                
                // 獲取 Phase 5 翻譯橋接器
                _translationBridge = Phase5ServiceContainer.GetService<UITranslationBridge>();
                
                // Phase 6: 獲取商業化服務
                _userService = Phase5ServiceContainer.GetService<IUserService>();
                _licenseService = Phase5ServiceContainer.GetService<ILicenseService>();
                _currencyService = Phase5ServiceContainer.GetService<ICurrencyService>();
                _pointsService = Phase5ServiceContainer.GetService<IPointsService>();
                _coinsService = Phase5ServiceContainer.GetService<ICoinsService>();
                
                InitializeCommands();
                
                // 初始化 Phase 5 翻譯功能
                InitializePhase5Async();
                
                // 初始化 Phase 6 商業化功能
                InitializePhase6Async();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"MainBarWindowViewModel 初始化失敗: {ex.Message}\n\n詳細錯誤: {ex}", 
                    "初始化錯誤", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        private async void InitializePhase5Async()
        {
            try
            {
                await _translationBridge.InitializeAsync();
                StatusText = "Phase 5 翻譯功能已就緒";
            }
            catch (Exception ex)
            {
                StatusText = "初始化失敗";
                System.Diagnostics.Debug.WriteLine($"Phase 5 初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化 Phase 6 商業化功能
        /// </summary>
        private async void InitializePhase6Async()
        {
            try
            {
                // 更新貨幣狀態
                await UpdateCurrencyStatusAsync();
                
                // 檢查登入狀態
                if (_userService?.IsLoggedIn() == true)
                {
                    var user = await _userService.GetCurrentUserAsync();
                    StatusText = $"歡迎回來, {user?.Username ?? "使用者"}";
                }
                else
                {
                    StatusText = "請登入以享受完整功能";
                }
                
                System.Diagnostics.Debug.WriteLine("Phase 6 商業化功能初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Phase 6 初始化失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新貨幣狀態
        /// </summary>
        private void InitializeCommands()
        {
            StartTranslationCommand = new RelayCommand(
                () => ExecuteStartTranslationCommand(),
                () => !IsTranslationActive
            );

            CaptureCommand = new RelayCommand(
                () => ExecuteCaptureCommand(),
                () => !IsTranslationActive
            );

            AudioCaptureCommand = new RelayCommand(
                () => ExecuteAudioCaptureCommand(),
                () => !IsAudioCaptureActive
            );

            HistoryCommand = new RelayCommand(
                () => ExecuteHistoryCommand()
            );

            SettingsCommand = new RelayCommand(
                () => ExecuteSettingsCommand()
            );

            MinimizeCommand = new RelayCommand(
                () => ExecuteMinimizeCommand()
            );

            ExitCommand = new RelayCommand(
                () => ExecuteExitCommand()
            );

            CompareEnginesCommand = new RelayCommand(
                () => ExecuteCompareEnginesCommand()
            );

            OpenDictionaryCommand = new RelayCommand(
                () => ExecuteOpenDictionaryCommand()
            );

            QuickScreenshotCommand = new RelayCommand(
                () => ExecuteQuickScreenshotCommand()
            );

            SelectRegionCommand = new RelayCommand(
                () => ExecuteSelectRegionCommand()
            );

            SelectRegion2Command = new RelayCommand(
                () => ExecuteSelectRegion2Command()
            );

            ComicModeCommand = new RelayCommand(
                () => ExecuteComicModeCommand()
            );

            OpenEditorCommand = new RelayCommand(
                () => ExecuteOpenEditorCommand()
            );

            ShowMoreToolsCommand = new RelayCommand(
                () => ExecuteShowMoreToolsCommand()
            );

            OpenSettingsCommand = new RelayCommand(
                () => ExecuteOpenSettingsCommand()
            );

            CollapseCommand = new RelayCommand(
                () => ExecuteCollapseCommand()
            );

            // Phase 6: 初始化商業化命令
            LoginCommand = new RelayCommand(
                () => ExecuteLoginCommand(),
                () => !IsLoggedIn
            );

            LogoutCommand = new RelayCommand(
                () => ExecuteLogoutCommand(),
                () => IsLoggedIn
            );

            PurchaseCoinsCommand = new RelayCommand(
                () => ExecutePurchaseCoinsCommand(),
                () => IsLoggedIn
            );

            DailyCheckInCommand = new RelayCommand(
                () => ExecuteDailyCheckInCommand(),
                () => IsLoggedIn
            );

            WatchAdCommand = new RelayCommand(
                () => ExecuteWatchAdCommand(),
                () => IsLoggedIn
            );

            ShareAppCommand = new RelayCommand(
                () => ExecuteShareAppCommand(),
                () => IsLoggedIn
            );

            ViewMemberInfoCommand = new RelayCommand(
                () => ExecuteViewMemberInfoCommand(),
                () => IsLoggedIn
            );

            UpgradeToProCommand = new RelayCommand(
                () => ExecuteUpgradeToProCommand(),
                () => IsLoggedIn && !IsProMember
            );
        }

        #endregion

        #region 命令實現

        private void ExecuteCaptureCommand()
        {
            try
            {
                StatusText = "準備螢幕翻譯...";
                IsTranslationActive = true;
                
                // 使用事件聚合器發送捕獲事件
                _eventAggregator?.Publish(new Phase5CaptureRequestedEvent());
                
                // 觸發螢幕擷取事件（向後相容）
                CaptureRequested?.Invoke(this, EventArgs.Empty);
                
                // 顯示通知
                _notificationService?.ShowNotification("螢幕翻譯", "正在準備螢幕擷取...");
                
                // 模擬翻譯過程
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                    
                    // 在UI執行緒上更新狀態
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsTranslationActive = false;
                        StatusText = "翻譯完成";
                        
                        _notificationService?.ShowNotification("翻譯完成", "螢幕翻譯已完成");
                        
                        // 3秒後重置狀態
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(3000);
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "就緒";
                            });
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                StatusText = "翻譯失敗";
                IsTranslationActive = false;
                // 記錄錯誤
                System.Diagnostics.Debug.WriteLine($"螢幕翻譯錯誤: {ex.Message}");
            }
        }

        private void ExecuteAudioCaptureCommand()
        {
            try
            {
                if (IsAudioCaptureActive)
                {
                    // 停止音訊擷取
                    StatusText = "停止音訊擷取";
                    IsAudioCaptureActive = false;
                }
                else
                {
                    // 開始音訊擷取
                    StatusText = "開始音訊擷取...";
                    IsAudioCaptureActive = true;
                    AudioCaptureRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                StatusText = "音訊擷取失敗";
                IsAudioCaptureActive = false;
                System.Diagnostics.Debug.WriteLine($"音訊擷取錯誤: {ex.Message}");
            }
        }

        private void ExecuteHistoryCommand()
        {
            try
            {
                HistoryRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"歷史記錄錯誤: {ex.Message}");
            }
        }

        private void ExecuteSettingsCommand()
        {
            try
            {
                SettingsRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定錯誤: {ex.Message}");
            }
        }

        private void ExecuteMinimizeCommand()
        {
            try
            {
                MinimizeRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"最小化錯誤: {ex.Message}");
            }
        }

        private void ExecuteStartTranslationCommand()
        {
            try
            {
                IsTranslationActive = !IsTranslationActive;
                
                if (IsTranslationActive)
                {
                    StatusText = "啟動翻譯會話...";
                    
                    // 使用 Phase 5 翻譯橋接器啟動翻譯會話
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await _translationBridge.StartTranslationSessionAsync();
                            
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "翻譯會話已啟動 - 按 F4 開始翻譯";
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "啟動失敗";
                                IsTranslationActive = false;
                                _notificationService?.ShowError($"翻譯啟動失敗: {ex.Message}");
                            });
                        }
                    });
                }
                else
                {
                    StatusText = "停止翻譯會話...";
                    
                    // 停止翻譯會話
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await _translationBridge.StopTranslationSessionAsync();
                            
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "翻譯會話已停止";
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "停止失敗";
                                _notificationService?.ShowError($"翻譯停止失敗: {ex.Message}");
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText = "翻譯操作失敗";
                IsTranslationActive = false;
                System.Diagnostics.Debug.WriteLine($"翻譯操作錯誤: {ex.Message}");
            }
        }

        private void ExecuteCompareEnginesCommand()
        {
            try
            {
                StatusText = "打開引擎對比...";
                // TODO: 實現引擎對比功能
                System.Diagnostics.Debug.WriteLine("引擎對比功能待實現");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"引擎對比錯誤: {ex.Message}");
            }
        }

        private void ExecuteOpenDictionaryCommand()
        {
            try
            {
                StatusText = "打開自定義詞典...";
                
                // 實現詞典窗口打開
                DictionaryRequested?.Invoke(this, EventArgs.Empty);
                
                // 提供反饋
                _notificationService?.ShowNotification("詞典", "自定義詞典已打開");
            }
            catch (Exception ex)
            {
                StatusText = "打開詞典失敗";
                System.Diagnostics.Debug.WriteLine($"打開詞典錯誤: {ex.Message}");
            }
        }

        private void ExecuteQuickScreenshotCommand()
        {
            try
            {
                StatusText = "快速截圖翻譯...";
                
                // 使用新的快速翻譯服務，支持字幕模式
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        // 透過 Application.Current.MainWindow 獲取主視窗參考
                        Window mainWindow = null;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 尋找 MainBarWindow 類型的視窗
                            foreach (Window window in System.Windows.Application.Current.Windows)
                            {
                                if (window.GetType().Name == "MainBarWindow")
                                {
                                    mainWindow = window;
                                    break;
                                }
                            }
                        });

                        var quickTranslationService = new MonLingo.Core.Service.QuickTranslationService(mainWindow);
                        await quickTranslationService.StartQuickTranslationAsync();
                        
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusText = "快速翻譯完成";
                            
                            // 3秒後重置狀態
                            System.Threading.Tasks.Task.Run(async () =>
                            {
                                await System.Threading.Tasks.Task.Delay(3000);
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    StatusText = "就緒";
                                });
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusText = "快速翻譯失敗";
                            _notificationService?.ShowError($"快速翻譯失敗: {ex.Message}");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                StatusText = "快速翻譯操作失敗";
                System.Diagnostics.Debug.WriteLine($"快速截圖錯誤: {ex.Message}");
            }
        }

        private void ExecuteSelectRegionCommand()
        {
            try
            {
                StatusText = "選擇翻譯區域...";
                
                // 使用事件聚合器發送區域選擇事件
                _eventAggregator?.Publish(new Phase5RegionSelectionEvent { RegionType = 1 });
                
                // 提供反饋
                _notificationService?.ShowNotification("區域選擇", "請在螢幕上拖拽選擇翻譯區域");
                
                System.Diagnostics.Debug.WriteLine("區域選擇功能已啟動");
            }
            catch (Exception ex)
            {
                StatusText = "區域選擇失敗";
                System.Diagnostics.Debug.WriteLine($"區域選擇錯誤: {ex.Message}");
            }
        }

        private void ExecuteSelectRegion2Command()
        {
            try
            {
                StatusText = "選擇翻譯區域2...";
                
                // 使用事件聚合器發送區域選擇事件
                _eventAggregator?.Publish(new Phase5RegionSelectionEvent { RegionType = 2 });
                
                // 提供反饋
                _notificationService?.ShowNotification("區域選擇", "請在螢幕上拖拽選擇第二個翻譯區域");
                
                System.Diagnostics.Debug.WriteLine("區域2選擇功能已啟動");
            }
            catch (Exception ex)
            {
                StatusText = "區域2選擇失敗";
                System.Diagnostics.Debug.WriteLine($"區域2選擇錯誤: {ex.Message}");
            }
        }

        private void ExecuteComicModeCommand()
        {
            try
            {
                StatusText = "漫畫翻譯模式...";
                
                // 切換漫畫模式狀態
                IsComicModeEnabled = !IsComicModeEnabled;
                
                // 提供反饋
                string modeStatus = IsComicModeEnabled ? "啟用" : "停用";
                _notificationService?.ShowNotification("漫畫模式", $"漫畫翻譯優化已{modeStatus}");
                
                StatusText = $"漫畫模式已{modeStatus}";
                
                System.Diagnostics.Debug.WriteLine($"漫畫模式: {modeStatus}");
            }
            catch (Exception ex)
            {
                StatusText = "漫畫模式切換失敗";
                System.Diagnostics.Debug.WriteLine($"漫畫模式錯誤: {ex.Message}");
            }
        }

        private void ExecuteOpenEditorCommand()
        {
            try
            {
                StatusText = "打開編輯器...";
                // TODO: 實現編輯器功能
                System.Diagnostics.Debug.WriteLine("編輯器功能待實現");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打開編輯器錯誤: {ex.Message}");
            }
        }

        private void ExecuteShowMoreToolsCommand()
        {
            try
            {
                StatusText = "顯示更多工具...";
                
                // 觸發更多工具菜單事件
                MoreToolsRequested?.Invoke(this, EventArgs.Empty);
                
                // 提供反饋
                _notificationService?.ShowNotification("工具", "更多工具菜單已打開");
            }
            catch (Exception ex)
            {
                StatusText = "顯示工具失敗";
                System.Diagnostics.Debug.WriteLine($"更多工具錯誤: {ex.Message}");
            }
        }

        private void ExecuteOpenSettingsCommand()
        {
            try
            {
                StatusText = "打開設定...";
                SettingsRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打開設定錯誤: {ex.Message}");
            }
        }

        private void ExecuteCollapseCommand()
        {
            try
            {
                StatusText = "收縮工具列...";
                
                // 切換收縮狀態
                IsCollapsed = !IsCollapsed;
                
                // 觸發收縮事件，讓 View 處理實際的收縮邏輯
                CollapseRequested?.Invoke(this, new CollapseEventArgs { IsCollapsed = IsCollapsed });
                
                string collapseStatus = IsCollapsed ? "已收縮" : "已展開";
                StatusText = $"工具列{collapseStatus}";
                
                System.Diagnostics.Debug.WriteLine($"工具列收縮狀態: {collapseStatus}");
            }
            catch (Exception ex)
            {
                StatusText = "收縮操作失敗";
                System.Diagnostics.Debug.WriteLine($"收縮錯誤: {ex.Message}");
            }
        }

        private void ExecuteExitCommand()
        {
            try
            {
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"退出錯誤: {ex.Message}");
            }
        }

        // Phase 6: 商業化命令實現

        private async void ExecuteLoginCommand()
        {
            try
            {
                var loginWindow = new MonLingo.Core.UI.SimpleLoginWindow();
                var result = loginWindow.ShowDialog();
                
                if (result == true)
                {
                    // 登入成功，更新UI狀態
                    await UpdateUserStatusAsync();
                    await UpdateCurrencyStatusAsync();
                    
                    StatusText = "登入成功";
                    _notificationService?.ShowNotification("登入成功", "歡迎回來！");
                }
            }
            catch (Exception ex)
            {
                StatusText = "登入失敗";
                System.Diagnostics.Debug.WriteLine($"登入錯誤: {ex.Message}");
            }
        }

        private async void ExecuteLogoutCommand()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "確定要登出嗎？", 
                    "確認登出", 
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await _userService.LogoutAsync();
                    
                    // 重置UI狀態
                    IsLoggedIn = false;
                    IsProMember = false;
                    Username = "遊客";
                    PointsBalance = 0;
                    CoinsBalanceInt = 0;
                    RemainingTranslations = "50"; // 免費使用者預設
                    
                    StatusText = "已登出";
                    _notificationService?.ShowNotification("登出成功", "下次見！");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"登出錯誤: {ex.Message}");
            }
        }

        private void ExecutePurchaseCoinsCommand()
        {
            try
            {
                // TODO: 打開購買硬幣對話框
                StatusText = "購買功能即將推出";
                _notificationService?.ShowNotification("提示", "購買功能即將推出");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"購買硬幣錯誤: {ex.Message}");
            }
        }

        private async void ExecuteDailyCheckInCommand()
        {
            try
            {
                if (_pointsService != null)
                {
                    var pointsEarned = await _pointsService.AddPointsFromDailyCheckInAsync();
                    
                    if (pointsEarned > 0)
                    {
                        await UpdateCurrencyStatusAsync();
                        StatusText = $"簽到成功，獲得 {pointsEarned} 積分";
                    }
                    else
                    {
                        StatusText = "今日已簽到";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"每日簽到錯誤: {ex.Message}");
            }
        }

        private async void ExecuteWatchAdCommand()
        {
            try
            {
                if (_pointsService != null)
                {
                    // 模擬觀看廣告
                    StatusText = "正在載入廣告...";
                    await Task.Delay(2000); // 模擬廣告時間
                    
                    var pointsEarned = await _pointsService.AddPointsFromRewardedAdAsync();
                    
                    if (pointsEarned > 0)
                    {
                        await UpdateCurrencyStatusAsync();
                        StatusText = $"觀看廣告完成，獲得 {pointsEarned} 積分";
                    }
                    else
                    {
                        StatusText = "今日觀看廣告次數已達上限";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"觀看廣告錯誤: {ex.Message}");
            }
        }

        private async void ExecuteShareAppCommand()
        {
            try
            {
                if (_pointsService != null)
                {
                    // 執行分享操作
                    var pointsEarned = await _pointsService.AddPointsFromSharingAsync();
                    
                    if (pointsEarned > 0)
                    {
                        await UpdateCurrencyStatusAsync();
                        StatusText = $"分享成功，獲得 {pointsEarned} 積分";
                    }
                    else
                    {
                        StatusText = "今日分享次數已達上限";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"分享應用錯誤: {ex.Message}");
            }
        }

        private void ExecuteViewMemberInfoCommand()
        {
            try
            {
                // TODO: 打開會員資訊對話框
                StatusText = "會員資訊功能即將推出";
                _notificationService?.ShowNotification("提示", "會員資訊功能即將推出");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查看會員資訊錯誤: {ex.Message}");
            }
        }

        private void ExecuteUpgradeToProCommand()
        {
            try
            {
                // TODO: 打開升級Pro會員對話框
                StatusText = "升級功能即將推出";
                _notificationService?.ShowNotification("提示", "升級功能即將推出");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"升級Pro會員錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新使用者狀態
        /// </summary>
        private async Task UpdateUserStatusAsync()
        {
            try
            {
                if (_userService?.IsLoggedIn() == true)
                {
                    var user = await _userService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        IsLoggedIn = true;
                        Username = user.Username ?? "使用者";
                        IsProMember = _licenseService?.IsPro() ?? false;
                    }
                }
                else
                {
                    IsLoggedIn = false;
                    IsProMember = false;
                    Username = "遊客";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新使用者狀態失敗: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged 實現

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 更新翻譯狀態
        /// </summary>
        /// <param name="isActive">是否活躍</param>
        /// <param name="statusText">狀態文字</param>
        public void UpdateTranslationStatus(bool isActive, string statusText = null)
        {
            IsTranslationActive = isActive;
            if (!string.IsNullOrEmpty(statusText))
            {
                StatusText = statusText;
            }
        }

        /// <summary>
        /// 更新音訊擷取狀態
        /// </summary>
        /// <param name="isActive">是否活躍</param>
        /// <param name="statusText">狀態文字</param>
        public void UpdateAudioCaptureStatus(bool isActive, string statusText = null)
        {
            IsAudioCaptureActive = isActive;
            if (!string.IsNullOrEmpty(statusText))
            {
                StatusText = statusText;
            }
        }

        /// <summary>
        /// 更新貨幣狀態
        /// </summary>
        private async Task UpdateCurrencyStatusAsync()
        {
            try
            {
                if (_pointsService != null)
                {
                    PointsBalance = await _pointsService.GetPointsBalanceAsync();
                }

                if (_coinsService != null)
                {
                    CoinsBalanceInt = await _coinsService.GetCoinsBalanceAsync();
                }

                // 更新界面顯示
                if (_currencyService != null)
                {
                    var status = await _currencyService.GetCurrencyStatusAsync();
                    
                    // 更新積分顯示
                    if (status.IsProMember)
                    {
                        RemainingTranslations = "∞";
                    }
                    else
                    {
                        RemainingTranslations = status.RemainingTranslations.ToString();
                    }
                    
                    // 更新硬幣餘額
                    CoinBalance = status.CoinsBalance.ToString("N0");
                }

                // 計算剩餘翻譯次數
                UpdateRemainingTranslations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新貨幣狀態失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新剩餘翻譯次數
        /// </summary>
        private void UpdateRemainingTranslations()
        {
            try
            {
                if (IsProMember)
                {
                    RemainingTranslations = "無限";
                }
                else
                {
                    var points = PointsBalance;
                    var coins = CoinsBalanceInt;
                    
                    // 根據文字翻譯成本計算 (使用Google基礎翻譯作為基準)
                    var textCost = _coinsService?.CalculateTranslationCost(MonLingo.Core.Models.TranslationEngine.Google, 100) ?? 1;
                    var availableTranslations = Math.Max(50, points + (coins / textCost)); // 最少保證50次免費
                    
                    RemainingTranslations = availableTranslations.ToString();
                }
            }
            catch (Exception ex)
            {
                RemainingTranslations = "50"; // 預設值
                System.Diagnostics.Debug.WriteLine($"計算剩餘翻譯次數失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化商業化功能
        /// </summary>
        private async Task InitializeCommercialFeaturesAsync()
        {
            try
            {
                await UpdateUserStatusAsync();
                await UpdateCurrencyStatusAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化商業化功能失敗: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable 實現

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 清理 Phase 5 翻譯橋接器
                    _translationBridge?.Dispose();
                    
                    // 清理 Phase 5 服務容器
                    Phase5ServiceContainer.Cleanup();
                    
                    // 清理託管資源
                    // 取消所有事件訂閱
                    CaptureRequested = null;
                    AudioCaptureRequested = null;
                    HistoryRequested = null;
                    SettingsRequested = null;
                    MinimizeRequested = null;
                    ExitRequested = null;
                }

                _isDisposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 收縮事件參數
    /// </summary>
    public class CollapseEventArgs : EventArgs
    {
        public bool IsCollapsed { get; set; }
    }
}
