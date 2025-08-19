using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MonLingo.Core.Commands;
using MonLingo.Core.Infrastructure;

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

        #endregion

        #region 建構函式

        public MainBarWindowViewModel()
        {
            // 建立服務容器實例
            var serviceContainer = new SimpleServiceContainer();
            serviceContainer.Initialize();
            
            // 獲取服務實例
            _eventAggregator = serviceContainer.GetService<ISimpleEventAggregator>();
            _notificationService = serviceContainer.GetService<ISimpleNotificationService>();
            
            InitializeCommands();
        }

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
                _eventAggregator?.Publish(new CaptureRequestedEvent());
                
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
                // 如果已經在翻譯，停止翻譯
                if (IsTranslationActive)
                {
                    StatusText = "停止翻譯";
                    IsTranslationActive = false;
                    // TODO: 停止翻譯邏輯
                }
                else
                {
                    // 開始翻譯
                    ExecuteCaptureCommand();
                }
            }
            catch (Exception ex)
            {
                StatusText = "操作失敗";
                System.Diagnostics.Debug.WriteLine($"開始翻譯錯誤: {ex.Message}");
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
                // TODO: 實現詞典功能
                System.Diagnostics.Debug.WriteLine("詞典功能待實現");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打開詞典錯誤: {ex.Message}");
            }
        }

        private void ExecuteQuickScreenshotCommand()
        {
            try
            {
                StatusText = "快速截圖翻譯...";
                // TODO: 實現快速截圖翻譯
                System.Diagnostics.Debug.WriteLine("快速截圖功能待實現");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"快速截圖錯誤: {ex.Message}");
            }
        }

        private void ExecuteSelectRegionCommand()
        {
            try
            {
                StatusText = "選擇翻譯區域...";
                // TODO: 實現區域選擇
                System.Diagnostics.Debug.WriteLine("區域選擇功能待實現");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"區域選擇錯誤: {ex.Message}");
            }
        }

        private void ExecuteSelectRegion2Command()
        {
            try
            {
                StatusText = "選擇翻譯區域2...";
                // TODO: 實現區域2選擇
                System.Diagnostics.Debug.WriteLine("區域2選擇功能待實現");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"區域2選擇錯誤: {ex.Message}");
            }
        }

        private void ExecuteComicModeCommand()
        {
            try
            {
                StatusText = "漫畫翻譯模式...";
                // TODO: 實現漫畫翻譯優化
                System.Diagnostics.Debug.WriteLine("漫畫模式功能待實現");
            }
            catch (Exception ex)
            {
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
                // TODO: 實現更多工具菜單
                System.Diagnostics.Debug.WriteLine("更多工具功能待實現");
            }
            catch (Exception ex)
            {
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
                // TODO: 實現工具列收縮功能
                System.Diagnostics.Debug.WriteLine("收縮功能待實現");
            }
            catch (Exception ex)
            {
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
}
