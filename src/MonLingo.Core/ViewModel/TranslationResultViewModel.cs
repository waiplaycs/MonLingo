using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MonLingo.Core.Commands;
using MonLingo.View.Windows;

namespace MonLingo.ViewModel
{
    /// <summary>
    /// TranslationPopupWindow 的 ViewModel
    /// 負責處理翻譯結果的顯示和互動邏輯
    /// </summary>
    public class TranslationResultViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Rect _sourceRegion;
        private readonly DateTime _startTime;
        private bool _isDisposed = false;

        #region 屬性

        private bool _isTranslating = true;
        /// <summary>
        /// 是否正在翻譯
        /// </summary>
        public bool IsTranslating
        {
            get => _isTranslating;
            set => SetProperty(ref _isTranslating, value);
        }

        private bool _hasResult = false;
        /// <summary>
        /// 是否有翻譯結果
        /// </summary>
        public bool HasResult
        {
            get => _hasResult;
            set => SetProperty(ref _hasResult, value);
        }

        private bool _hasError = false;
        /// <summary>
        /// 是否有錯誤
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        private string _statusText = "正在翻譯...";
        /// <summary>
        /// 狀態文字
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _originalText = "";
        /// <summary>
        /// 原文
        /// </summary>
        public string OriginalText
        {
            get => _originalText;
            set => SetProperty(ref _originalText, value);
        }

        private string _translatedText = "";
        /// <summary>
        /// 譯文
        /// </summary>
        public string TranslatedText
        {
            get => _translatedText;
            set => SetProperty(ref _translatedText, value);
        }

        private string _detectedLanguage = "自動偵測";
        /// <summary>
        /// 檢測到的語言
        /// </summary>
        public string DetectedLanguage
        {
            get => _detectedLanguage;
            set => SetProperty(ref _detectedLanguage, value);
        }

        private string _targetLanguage = "繁體中文";
        /// <summary>
        /// 目標語言
        /// </summary>
        public string TargetLanguage
        {
            get => _targetLanguage;
            set => SetProperty(ref _targetLanguage, value);
        }

        private string _errorMessage = "";
        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private double _processingTime = 0.0;
        /// <summary>
        /// 處理時間 (秒)
        /// </summary>
        public double ProcessingTime
        {
            get => _processingTime;
            set => SetProperty(ref _processingTime, value);
        }

        private bool _showStatusBar = true;
        /// <summary>
        /// 是否顯示狀態列
        /// </summary>
        public bool ShowStatusBar
        {
            get => _showStatusBar;
            set => SetProperty(ref _showStatusBar, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 複製命令
        /// </summary>
        public ICommand CopyCommand { get; private set; }

        /// <summary>
        /// 關閉命令
        /// </summary>
        public ICommand CloseCommand { get; private set; }

        /// <summary>
        /// 重新翻譯命令
        /// </summary>
        public ICommand RetryCommand { get; private set; }

        #endregion

        #region 事件

        /// <summary>
        /// 關閉請求事件
        /// </summary>
        public event EventHandler CloseRequested;

        /// <summary>
        /// 翻譯完成事件
        /// </summary>
        public event EventHandler<TranslationCompletedEventArgs> TranslationCompleted;

        #endregion

        #region 建構函式

        public TranslationResultViewModel(Rect sourceRegion)
        {
            _sourceRegion = sourceRegion;
            _startTime = DateTime.Now;

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            CopyCommand = new RelayCommand(
                () => ExecuteCopyCommand(),
                () => HasResult && !string.IsNullOrEmpty(TranslatedText)
            );

            CloseCommand = new RelayCommand(
                () => ExecuteCloseCommand()
            );

            RetryCommand = new RelayCommand(
                () => ExecuteRetryCommand(),
                () => !IsTranslating
            );
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 開始翻譯過程
        /// </summary>
        public async void StartTranslation()
        {
            try
            {
                IsTranslating = true;
                HasResult = false;
                HasError = false;
                StatusText = "正在擷取螢幕內容...";

                // 模擬螢幕擷取過程
                await Task.Delay(300);

                StatusText = "正在進行OCR識別...";
                
                // 模擬OCR過程
                await PerformOCR();

                StatusText = "正在翻譯...";
                
                // 模擬翻譯過程
                await PerformTranslation();

                // 計算處理時間
                ProcessingTime = (DateTime.Now - _startTime).TotalSeconds;

                IsTranslating = false;
                HasResult = true;
                StatusText = "翻譯完成";

                // 通知翻譯完成
                TranslationCompleted?.Invoke(this, new TranslationCompletedEventArgs(true, TranslatedText));
            }
            catch (Exception ex)
            {
                HandleTranslationError(ex);
            }
        }

        /// <summary>
        /// 顯示訊息
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <param name="messageType">訊息類型</param>
        public void ShowMessage(string message, MessageType messageType)
        {
            StatusText = message;
            
            // 根據訊息類型設置不同的顯示效果
            switch (messageType)
            {
                case MessageType.Success:
                    // 可以添加成功樣式
                    break;
                case MessageType.Error:
                    // 可以添加錯誤樣式
                    break;
                case MessageType.Warning:
                    // 可以添加警告樣式
                    break;
                case MessageType.Info:
                default:
                    // 預設樣式
                    break;
            }

            // 3秒後清除訊息
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                if (HasResult)
                {
                    StatusText = "翻譯完成";
                }
            });
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 執行OCR識別
        /// </summary>
        private async Task PerformOCR()
        {
            // 模擬OCR過程
            await Task.Delay(800);

            // 模擬OCR結果
            OriginalText = "Hello World! This is a sample text for translation testing.";
            DetectedLanguage = "英文";
        }

        /// <summary>
        /// 執行翻譯
        /// </summary>
        private async Task PerformTranslation()
        {
            // 模擬翻譯過程
            await Task.Delay(1000);

            // 模擬翻譯結果
            switch (OriginalText.ToLower())
            {
                case var text when text.Contains("hello world"):
                    TranslatedText = "你好世界！這是一個翻譯測試的範例文字。";
                    break;
                default:
                    TranslatedText = "這是模擬的翻譯結果。實際翻譯將通過Native.dll的OCR引擎和翻譯服務來完成。";
                    break;
            }
        }

        /// <summary>
        /// 處理翻譯錯誤
        /// </summary>
        /// <param name="ex">異常</param>
        private void HandleTranslationError(Exception ex)
        {
            IsTranslating = false;
            HasResult = false;
            HasError = true;
            ErrorMessage = $"翻譯失敗: {ex.Message}";
            StatusText = "翻譯失敗";

            // 計算處理時間
            ProcessingTime = (DateTime.Now - _startTime).TotalSeconds;

            // 通知翻譯完成（失敗）
            TranslationCompleted?.Invoke(this, new TranslationCompletedEventArgs(false, null, ex.Message));
        }

        #endregion

        #region 命令實現

        private void ExecuteCopyCommand()
        {
            try
            {
                if (!string.IsNullOrEmpty(TranslatedText))
                {
                    Clipboard.SetText(TranslatedText);
                    ShowMessage("已複製到剪貼簿", MessageType.Success);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"複製錯誤: {ex.Message}");
                ShowMessage("複製失敗", MessageType.Error);
            }
        }

        private void ExecuteCloseCommand()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteRetryCommand()
        {
            // 重新開始翻譯過程
            StartTranslation();
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

            // 當關鍵屬性變更時，更新命令的可執行狀態
            if (propertyName == nameof(HasResult) || propertyName == nameof(TranslatedText) || propertyName == nameof(IsTranslating))
            {
                (CopyCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RetryCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }

            return true;
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
                    CloseRequested = null;
                    TranslationCompleted = null;
                }

                _isDisposed = true;
            }
        }

        #endregion
    }
}
