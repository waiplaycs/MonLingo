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
    private int _roundNumber = 0;
        
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
            set
            {
                if (SetProperty(ref _windowWidth, value))
                {
                    // 在分離模式下，記住最後使用的尺寸
                    if (IsDetached)
                    {
                        _lastDetachedWidth = value;
                    }
                }
            }
        }

        private double _windowHeight = 120;
        public double WindowHeight
        {
            get => _windowHeight;
            set
            {
                if (SetProperty(ref _windowHeight, value))
                {
                    // 在分離模式下，記住最後使用的尺寸
                    if (IsDetached)
                    {
                        _lastDetachedHeight = value;
                    }
                }
            }
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

    private bool _isDetached = false; // 是否分離模式（預設依附）
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
        
        // 鎖定狀態：鎖定時不允許移動/縮放，也不進行自動依附
    private bool _isLocked = false; // 初始狀態為解鎖
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (SetProperty(ref _isLocked, value))
                {
                    // 無論鎖定或非鎖定，保持目前尺寸一致
                    if (IsDetached)
                    {
                        // 更新分離尺寸為當前值，之後切換狀態都使用這一份
                        _lastDetachedWidth = WindowWidth;
                        _lastDetachedHeight = WindowHeight;
                    }
                }
            }
        }

    // 記錄分離模式下的最後尺寸，避免雙擊切換時尺寸跳變
    private double _lastDetachedWidth = 400;
    private double _lastDetachedHeight = 120;

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
            Logger.Info($"[SubtitleVM] AddNewLine: round=#{_roundNumber}, beforeCount={SubtitleLines.Count}, original='{originalText}', translated='{translatedText}'");
            // 創建新的字幕行項目
            var newLine = new SubtitleLineItem
            {
                OriginalText = originalText ?? string.Empty,
                TranslatedText = translatedText ?? string.Empty,
                Timestamp = DateTime.Now
            };

            // 若達到行數上限，移除最舊的行，改為滾動視窗顯示
            while (SubtitleLines.Count >= MaxLines)
            {
                SubtitleLines.RemoveAt(0);
            }

            // 添加到集合
            SubtitleLines.Add(newLine);
            Logger.Info($"[SubtitleVM] AddNewLine: afterCount={SubtitleLines.Count}");

            // 觸發滾動事件
            OnNewLineAdded();
        }

        /// <summary>
        /// 清空所有字幕行
        /// </summary>
        public void ClearLines()
        {
            Logger.Info($"[SubtitleVM] ClearLines: clearing {SubtitleLines.Count} lines");
            SubtitleLines.Clear();
        }

        /// <summary>
        /// 開始新的顯示回合：僅在回合開始時清空一次
        /// </summary>
        public void StartNewRound()
        {
            _roundNumber++;
            Logger.Info($"[SubtitleVM] StartNewRound: round=#{_roundNumber}, clearing previous lines={SubtitleLines.Count}");
            ClearLines();
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
        /// 重新依附到主工具條（自動鎖定）
        /// </summary>
        public void ReattachSubtitleWindow()
        {
            IsDetached = false;
            IsLocked = true; // 依附時自動鎖定
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
                // 吸附狀態時，無論是否鎖定都要跟隨工具條移動
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
                // 吸附狀態時，無論是否鎖定都要跟隨工具條尺寸變化
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
                // 分離模式：不重設尺寸，只記住當前尺寸作為分離尺寸
                _lastDetachedWidth = WindowWidth;
                _lastDetachedHeight = WindowHeight;
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
            
            // 設置頂部位置在主工具條下方，有 10px 重疊以增強視覺連續性
            WindowTop = _mainBarWindow.Top + _mainBarWindow.ActualHeight - 10;
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
