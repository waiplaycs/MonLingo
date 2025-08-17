using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MonLingo.Core.Commands;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// MainBarWindow 的 ViewModel
    /// 負責處理主工具列的所有業務邏輯和使用者互動
    /// </summary>
    public class MainBarWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool _isDisposed = false;

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

        #endregion

        #region 命令

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
            InitializeCommands();
        }

        private void InitializeCommands()
        {
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
        }

        #endregion

        #region 命令實現

        private void ExecuteCaptureCommand()
        {
            try
            {
                StatusText = "準備螢幕翻譯...";
                IsTranslationActive = true;
                
                // 觸發螢幕擷取事件
                CaptureRequested?.Invoke(this, EventArgs.Empty);
                
                // 模擬翻譯過程
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                    
                    // 在UI執行緒上更新狀態
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsTranslationActive = false;
                        StatusText = "翻譯完成";
                        
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
