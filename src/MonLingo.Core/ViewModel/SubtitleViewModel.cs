using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MonLingo.Core.Commands;
using NLog;

namespace MonLingo.Core.ViewModel
{
    /// <summary>
    /// 字幕模式的 ViewModel
    /// 管理字幕行的顯示和動畫效果，支援與主工具條對齊
    /// </summary>
    public class SubtitleViewModel : INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        #region Properties

        /// <summary>
        /// 字幕行集合，最多顯示指定行數
        /// </summary>
        public ObservableCollection<SubtitleLineItem> SubtitleLines { get; }

        private int _maxLines = 5; // 最多顯示5行字幕
        public int MaxLines
        {
            get => _maxLines;
            set => SetProperty(ref _maxLines, value);
        }

        private double _windowWidth = 400; // 預設寬度
        public double WindowWidth
        {
            get => _windowWidth;
            set => SetProperty(ref _windowWidth, value);
        }

        private double _windowHeight = 120;
        public double WindowHeight
        {
            get => _windowHeight;
            set => SetProperty(ref _windowHeight, value);
        }

        private double _windowLeft = 100; // 預設左邊距
        public double WindowLeft
        {
            get => _windowLeft;
            set => SetProperty(ref _windowLeft, value);
        }

        private double _windowTop = 100; // 預設頂部位置
        public double WindowTop
        {
            get => _windowTop;
            set => SetProperty(ref _windowTop, value);
        }

        private bool _isDetached = false; // 是否分離模式
        public bool IsDetached
        {
            get => _isDetached;
            set 
            { 
                if (SetProperty(ref _isDetached, value))
                {
                    OnDetachModeChanged();
                }
            }
        }

        private Window _mainBarWindow;
        public Window MainBarWindow
        {
            get => _mainBarWindow;
            set 
            { 
                if (_mainBarWindow != value)
                {
                    UnsubscribeFromMainBarWindow();
                    _mainBarWindow = value;
                    SubscribeToMainBarWindow();
                    if (!IsDetached)
                    {
                        UpdateWindowPositionFromMainBar();
                    }
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 新字幕行添加事件，用於觸發滾動動畫
        /// </summary>
        public event Action NewLineAdded;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructor

        public SubtitleViewModel()
        {
            SubtitleLines = new ObservableCollection<SubtitleLineItem>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 添加新的字幕行
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="translatedText">譯文</param>
        public void AddNewLine(string originalText, string translatedText)
        {
            // 先清洗舊顯示內容
            SubtitleLines.Clear();

            // 創建新的字幕行項目
            var newLine = new SubtitleLineItem
            {
                OriginalText = originalText ?? string.Empty,
                TranslatedText = translatedText ?? string.Empty,
                Timestamp = DateTime.Now
            };

            // 添加到集合
            SubtitleLines.Add(newLine);

            // 觸發滾動事件
            OnNewLineAdded();
        }

        /// <summary>
        /// 清空所有字幕行
        /// </summary>
        public void ClearLines()
        {
            SubtitleLines.Clear();
        }

        /// <summary>
        /// 設置主工具條視窗引用
        /// </summary>
        /// <param name="mainBarWindow">主工具條視窗</param>
        public void SetMainBarWindow(Window mainBarWindow)
        {
            MainBarWindow = mainBarWindow;
        }

        /// <summary>
        /// 分離字幕視窗（雙擊標題列或右鍵菜單）
        /// </summary>
        public void DetachSubtitleWindow()
        {
            IsDetached = true;
        }

        /// <summary>
        /// 重新依附到主工具條
        /// </summary>
        public void ReattachSubtitleWindow()
        {
            IsDetached = false;
        }

        #endregion

        #region Commands

        private RelayCommand _detachCommand;
        public RelayCommand DetachCommand => _detachCommand ??= new RelayCommand(DetachSubtitleWindow);

        private RelayCommand _reattachCommand;
        public RelayCommand ReattachCommand => _reattachCommand ??= new RelayCommand(ReattachSubtitleWindow);

        private RelayCommand _closeCommand;
        public RelayCommand CloseCommand => _closeCommand ??= new RelayCommand(() =>
        {
            Logger.Info("🔒 SubtitleViewModel CloseCommand 被觸發");
            Logger.Debug($"📊 目前應用程式視窗數量: {Application.Current.Windows.Count}");
            
            // 隱藏字幕視窗但不關閉，允許下次重新顯示
            bool windowFound = false;
            foreach (Window window in Application.Current.Windows)
            {
                Logger.Debug($"🔍 檢查視窗: {window.GetType().Name}");
                if (window.GetType().Name == "SubtitleWindow")
                {
                    Logger.Info($"📺 找到字幕視窗，當前狀態: IsVisible={window.IsVisible}, Visibility={window.Visibility}");
                    window.Visibility = Visibility.Hidden;
                    Logger.Info($"✅ 字幕視窗已隱藏，新狀態: IsVisible={window.IsVisible}, Visibility={window.Visibility}");
                    windowFound = true;
                    break;
                }
            }
            
            if (!windowFound)
            {
                Logger.Warn("⚠️ 未找到字幕視窗進行關閉操作");
            }
        });

        #endregion

        #region Private Methods

        /// <summary>
        /// 訂閱主工具條視窗事件
        /// </summary>
        private void SubscribeToMainBarWindow()
        {
            if (_mainBarWindow != null)
            {
                _mainBarWindow.LocationChanged += OnMainBarLocationChanged;
                _mainBarWindow.SizeChanged += OnMainBarSizeChanged;
            }
        }

        /// <summary>
        /// 取消訂閱主工具條視窗事件
        /// </summary>
        private void UnsubscribeFromMainBarWindow()
        {
            if (_mainBarWindow != null)
            {
                _mainBarWindow.LocationChanged -= OnMainBarLocationChanged;
                _mainBarWindow.SizeChanged -= OnMainBarSizeChanged;
            }
        }

        /// <summary>
        /// 主工具條位置變化事件處理
        /// </summary>
        private void OnMainBarLocationChanged(object sender, EventArgs e)
        {
            if (!IsDetached)
            {
                UpdateWindowPositionFromMainBar();
            }
        }

        /// <summary>
        /// 主工具條尺寸變化事件處理
        /// </summary>
        private void OnMainBarSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsDetached)
            {
                UpdateWindowPositionFromMainBar();
            }
        }

        /// <summary>
        /// 分離模式變化處理
        /// </summary>
        private void OnDetachModeChanged()
        {
            if (IsDetached)
            {
                // 分離模式：保持與依附模式完全相同的尺寸
                // 不改變位置，只確保尺寸一致
                if (_mainBarWindow != null)
                {
                    // 使用與依附模式完全相同的尺寸
                    WindowWidth = _mainBarWindow.ActualWidth;
                    WindowHeight = 120; // 與依附模式相同的高度
                }
                else
                {
                    // 默認尺寸
                    WindowWidth = 400;
                    WindowHeight = 120;
                }
                
                // 完全不移動視窗位置，保持用戶當前的視窗位置
                // 這樣雙擊分離時視窗會停留在原地
            }
            else
            {
                // 依附模式：重新跟隨主工具條
                UpdateWindowPositionFromMainBar();
            }
        }

        /// <summary>
        /// 根據主工具條位置更新視窗位置
        /// </summary>
        private void UpdateWindowPositionFromMainBar()
        {
            if (_mainBarWindow == null) return;

            // 設置寬度與主工具條一致
            WindowWidth = _mainBarWindow.ActualWidth;
            
            // 設置固定高度，確保比例協調
            WindowHeight = 120; // 依附模式標準高度
            
            // 設置左邊距與主工具條一致
            WindowLeft = _mainBarWindow.Left;
            
            // 設置頂部位置在主工具條下方
            WindowTop = _mainBarWindow.Top + _mainBarWindow.ActualHeight;
        }

        /// <summary>
        /// 觸發新行添加事件
        /// </summary>
        protected virtual void OnNewLineAdded()
        {
            NewLineAdded?.Invoke();
        }

        /// <summary>
        /// 設置屬性值並觸發通知
        /// </summary>
        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 觸發屬性變更事件
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 字幕行項目類別
    /// </summary>
    public class SubtitleLineItem
    {
        /// <summary>
        /// 原文
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 譯文
        /// </summary>
        public string TranslatedText { get; set; } = string.Empty;

        /// <summary>
        /// 時間戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 顯示的文字（結合原文和譯文）
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OriginalText))
                    return TranslatedText;
                
                if (string.IsNullOrWhiteSpace(TranslatedText))
                    return OriginalText;

                return $"{OriginalText} → {TranslatedText}";
            }
        }
    }
}
