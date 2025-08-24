/* ============================================================ */
/* MonLingo 工具條 C# Code-Behind 完整備份 */
/* 日期: 2025年8月24日 */
/* 文件: MainBarWindow.xaml.cs 完整內容 */
/* ============================================================ */

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using MonLingo.ViewModel;
using MonLingo.View.Windows;
using NLog;

namespace MonLingo.Core.View.Windows
{
    public partial class MainBarWindow : Window, INotifyPropertyChanged
    {
        #region 私有字段
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private WorkingMainBarWindowViewModel _viewModel;
        private bool _isDragging = false;
        private Point _lastMousePosition;
        private bool _isDraggingResize = false;
        private readonly double _dragSensitivity = 1.0;
        private readonly double _originalWidth = 975.0;
        private readonly double _collapsedWidth = 100.0;
        private bool _isCollapsed = false;
        private Geometry _collapseButtonIcon;
        #endregion

        #region 屬性
        public Geometry CollapseButtonIcon
        {
            get => _collapseButtonIcon;
            set
            {
                _collapseButtonIcon = value;
                OnPropertyChanged(nameof(CollapseButtonIcon));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region 建構函數
        public MainBarWindow()
        {
            InitializeComponent();
            InitializeWindow();
            Logger.Info("🚀 MainBarWindow 初始化完成");
        }

        private void InitializeWindow()
        {
            try
            {
                // 初始化 ViewModel
                _viewModel = new WorkingMainBarWindowViewModel(this);
                this.DataContext = _viewModel;

                // 設定窗口初始狀態
                this.Width = _originalWidth;
                
                // 初始化收縮按鈕圖標為向左收縮
                CollapseButtonIcon = (Geometry)this.FindResource("IconChevronsLeft");

                // 訂閱 ViewModel 事件
                if (_viewModel != null)
                {
                    _viewModel.CaptureRequested += OnCaptureRequested;
                    _viewModel.SettingsRequested += OnSettingsRequested;
                    _viewModel.ExitRequested += OnExitRequested;
                    _viewModel.MinimizeRequested += OnMinimizeRequested;
                    _viewModel.CollapseRequested += OnCollapseRequested;
                }

                // 確保觸發區域在初始化時可用
                this.Loaded += (s, e) => EnsureRightTriggerAreaEnabled();

                Logger.Debug("✅ MainBarWindow 初始化設定完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ MainBarWindow 初始化時發生錯誤");
            }
        }
        #endregion

        #region 拖拽功能
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 檢查是否點擊在按鈕上
                var element = e.OriginalSource as FrameworkElement;
                if (IsClickOnButton(element))
                {
                    // 如果點擊在按鈕上，不進行拖拽
                    Logger.Debug("點擊在按鈕上，取消拖拽");
                    return;
                }

                if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed)
                {
                    _isDragging = true;
                    _lastMousePosition = e.GetPosition(this);
                    Logger.Debug("開始拖拽工具條");
                    this.DragMove();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "拖拽工具條時發生錯誤");
            }
        }

        /// <summary>
        /// 檢查點擊是否在按鈕或可交互元素上
        /// </summary>
        private bool IsClickOnButton(FrameworkElement element)
        {
            if (element == null) return false;

            // 向上遍歷視覺樹，檢查是否為按鈕或可交互元素
            while (element != null)
            {
                if (element is Button || 
                    element is CheckBox || 
                    element is ComboBox ||
                    element.Name == "RightTriggerArea" ||
                    element.Cursor == Cursors.Hand)
                {
                    return true;
                }
                element = element.Parent as FrameworkElement;
            }
            return false;
        }
        #endregion

        #region 右端觸發區域事件處理
        private void EnsureRightTriggerAreaEnabled()
        {
            try
            {
                var triggerArea = this.FindName("RightTriggerArea") as FrameworkElement;
                if (triggerArea != null)
                {
                    triggerArea.IsEnabled = true;
                    triggerArea.Visibility = Visibility.Visible;
                    Logger.Debug($"🔄 右端觸發區域已確保啟用，狀態：可見={triggerArea.Visibility}, 啟用={triggerArea.IsEnabled}");
                }
                else
                {
                    Logger.Warn("⚠️ 找不到右端觸發區域控件");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "確保右端觸發區域啟用時發生錯誤");
            }
        }

        private void RightTriggerArea_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.SizeWE;
            Logger.Debug("🖱️ 滑鼠進入右端觸發區域，游標已切換為調整大小");
        }

        private void RightTriggerArea_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDraggingResize)
            {
                this.Cursor = Cursors.Arrow;
                Logger.Debug("🖱️ 滑鼠離開右端觸發區域，游標已恢復");
            }
        }

        private void RightTriggerArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var triggerArea = sender as FrameworkElement;
                if (triggerArea != null && triggerArea.IsEnabled)
                {
                    Logger.Debug("🔄 開始拖拽調整工具條大小");
                    _isDraggingResize = true;
                    _lastMousePosition = e.GetPosition(this);
                    triggerArea.CaptureMouse();
                    e.Handled = true;
                }
                else
                {
                    Logger.Warn("⚠️ 觸發區域未啟用或為空");
                    EnsureRightTriggerAreaEnabled();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "開始拖拽調整大小時發生錯誤");
            }
        }

        private void RightTriggerArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingResize) return;

            try
            {
                var currentPosition = e.GetPosition(this);
                var deltaX = (currentPosition.X - _lastMousePosition.X) * _dragSensitivity;
                
                // 右端拖拽：只調整寬度
                var newWidth = this.Width + deltaX;
                
                // 限制最小和最大寬度
                newWidth = Math.Max(_collapsedWidth, Math.Min(_originalWidth, newWidth));
                
                if (Math.Abs(this.Width - newWidth) > 0.5)
                {
                    this.Width = newWidth;
                    Logger.Debug($"🔄 拖拽調整寬度: {this.Width:F1}px (收縮狀態: {_isCollapsed})");
                }
                
                // 動態顯示/隱藏內容 - 但不影響觸發區域
                UpdateContentVisibility(this.Width);
                
                _lastMousePosition = currentPosition;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "拖拽調整大小時發生錯誤");
            }
        }

        private void UpdateContentVisibility(double width)
        {
            try
            {
                var collapsibleContent = this.FindName("CollapsibleContent") as FrameworkElement;
                var rightTriggerArea = this.FindName("RightTriggerArea") as FrameworkElement;

                if (width <= _collapsedWidth + 50) // 收縮狀態：顯示第1、20、21號按鈕
                {
                    if (collapsibleContent != null)
                        collapsibleContent.Visibility = Visibility.Collapsed;
                }
                else // 完全展開狀態：顯示所有按鈕
                {
                    if (collapsibleContent != null)
                        collapsibleContent.Visibility = Visibility.Visible;
                }

                // 🔑 關鍵：確保右端觸發區域始終可用，無論收縮狀態如何
                if (rightTriggerArea != null)
                {
                    rightTriggerArea.IsEnabled = true;
                    rightTriggerArea.Visibility = Visibility.Visible;
                    Logger.Debug($"🛡️ 觸發區域狀態確保：可見={rightTriggerArea.Visibility}, 啟用={rightTriggerArea.IsEnabled}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新內容可見性時發生錯誤");
            }
        }

        private void RightTriggerArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDraggingResize)
                {
                    Logger.Debug("結束拖拽調整工具條大小");
                    _isDraggingResize = false;
                    ((FrameworkElement)sender).ReleaseMouseCapture();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "結束拖拽調整大小時發生錯誤");
            }
        }
        #endregion

        #region 收縮/展開功能
        public void ToggleCollapse()
        {
            try
            {
                Logger.Info($"🔄 收縮切換開始 - 當前狀態: {(_isCollapsed ? "收縮" : "展開")}");
                
                if (_isCollapsed)
                {
                    ExpandToolbar();
                }
                else
                {
                    CollapseToolbar();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "收縮切換時發生錯誤");
            }
        }

        private void CollapseToolbar()
        {
            try
            {
                Logger.Info("🔽 開始收縮工具條");
                _isCollapsed = true;

                // 更新收縮按鈕圖標為向右展開
                CollapseButtonIcon = (Geometry)this.FindResource("IconChevronsRight");

                // 立即隱藏可收縮內容
                var collapsibleContent = this.FindName("CollapsibleContent") as FrameworkElement;
                if (collapsibleContent != null)
                {
                    collapsibleContent.Visibility = Visibility.Collapsed;
                }

                // 創建寬度動畫
                var widthAnimation = new DoubleAnimation
                {
                    From = this.Width,
                    To = _collapsedWidth,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                widthAnimation.Completed += (s, e) =>
                {
                    EnsureRightTriggerAreaEnabled();
                    Logger.Debug($"✅ 工具條收縮完成，寬度: {this.Width}");
                };

                this.BeginAnimation(WidthProperty, widthAnimation);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "收縮工具條時發生錯誤");
            }
        }

        private void ExpandToolbar()
        {
            try
            {
                Logger.Info("🔼 開始展開工具條");
                _isCollapsed = false;

                // 更新收縮按鈕圖標為向左收縮
                CollapseButtonIcon = (Geometry)this.FindResource("IconChevronsLeft");

                // 創建寬度動畫
                var widthAnimation = new DoubleAnimation
                {
                    From = this.Width,
                    To = _originalWidth,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                widthAnimation.Completed += (s, e) =>
                {
                    // 顯示所有內容
                    UpdateContentVisibility(_originalWidth);
                    
                    // 🔑 關鍵：確保右端觸發區域在展開後立即可用
                    EnsureRightTriggerAreaEnabled();
                    
                    // 🛡️ 額外保護：強制重新啟用觸發區域
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        EnsureRightTriggerAreaEnabled();
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    
                    Logger.Debug($"✅ 工具條展開完成，寬度: {this.Width}，右端觸發區域已多重確保可用");
                };

                this.BeginAnimation(WidthProperty, widthAnimation);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "展開工具條時發生錯誤");
            }
        }
        #endregion

        #region ViewModel事件處理
        private void OnCaptureRequested(object sender, EventArgs e)
        {
            // 觸發螢幕擷取功能
            // 這裡將會打開 CaptureRegionWindow
            // var captureWindow = new CaptureRegionWindow(); // 暫時註解 - Phase6 測試
            // captureWindow.Show();
            
            // 臨時隱藏工具列以避免干擾擷取
            this.Hide();
        }

        private void OnSettingsRequested(object sender, EventArgs e)
        {
            try
            {
                Logger.Info("🔧 正在打開設定視窗...");
                
                // 打開設定視窗
                var settingsWindow = new SettingMainWindow();
                Logger.Info("✅ 設定視窗物件已創建");
                
                // 移除 Owner 設定，讓設置窗口獨立運行，不受工具條窗口影響
                // settingsWindow.Owner = this; // 註釋掉這行以避免工具條被禁用
                Logger.Info("✅ 設定為獨立視窗");
                
                settingsWindow.Show();
                Logger.Info("✅ 設定視窗已顯示");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ 無法打開設定視窗");
                MessageBox.Show($"設置窗口無法載入！\n錯誤詳情: {ex.Message}\n\n堆疊追蹤:\n{ex.StackTrace}", 
                    "設置", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExitRequested(object sender, EventArgs e)
        {
            // 安全關閉應用程式
            Application.Current.Shutdown();
        }

        private void OnMinimizeRequested(object sender, EventArgs e)
        {
            // 最小化工具條到系統托盤
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
        }
        
        private void OnCollapseRequested(object sender, CollapseEventArgs e)
        {
            // 執行工具條收縮/展開
            ToggleCollapse();
        }
        #endregion

        #region 視窗生命週期
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 註冊全域熱鍵 (透過 HotKeyService)
            RegisterGlobalHotKeys();
        }

        private void RegisterGlobalHotKeys()
        {
            try
            {
                // 註冊 Ctrl+Q 熱鍵觸發螢幕翻譯
                var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                if (hwndSource != null)
                {
                    // 這裡將整合 HotKeyService
                    // HotKeyService.RegisterHotKey(hwndSource.Handle, 1, ModifierKeys.Control, Key.Q);
                }
            }
            catch (Exception ex)
            {
                // 記錄熱鍵註冊失敗
                System.Diagnostics.Debug.WriteLine($"熱鍵註冊失敗: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理資源
            if (_viewModel != null)
            {
                // WorkingMainBarWindowViewModel 可能沒有事件，只需清理引用
                _viewModel = null;
            }
            
            base.OnClosed(e);
        }
        #endregion

        #region 視窗動畫和視覺效果
        /// <summary>
        /// 顯示工具列時的淡入動畫
        /// </summary>
        public void ShowWithAnimation()
        {
            this.Opacity = 0;
            this.Show();
            
            // 使用 Storyboard 實現淡入效果
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, 
                TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        /// <summary>
        /// 隱藏工具列時的淡出動畫
        /// </summary>
        public void HideWithAnimation()
        {
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, 
                TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => this.Hide();
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        #endregion
    }
}

/* ============================================================ */
/* 21按鈕功能完整列表 */
/* ============================================================ */

/*
1. StartTranslateButton - 開始翻譯按鈕 (播放圖標)
   - Command: StartTranslationCommand
   - 旋轉動畫效果
   - 觸發主要翻譯功能

2. ProStatusButton - 會員狀態按鈕 (皇冠圖標 + PRO文字)
   - Command: UpgradeToProCommand
   - 金色漸層背景
   - 升級到專業版功能

3. RemainingTranslationsButton - 今日剩餘翻譯點數
   - Command: DailyCheckInCommand
   - 顯示: {Binding RemainingTranslations}
   - 藍色文字，每日簽到功能

4. CoinBalanceButton - 硬幣餘額 (硬幣圖標 + 數字)
   - Command: PurchaseCoinsCommand
   - 顯示: {Binding CoinBalance}
   - 購買硬幣功能

5. TranslationEngineSelector - 翻譯引擎選擇
   - 顯示: {Binding SelectedEngine}
   - 綠色圓點狀態指示器
   - 下拉箭頭圖標

6. CompareEnginesButton - 翻譯引擎對比
   - Command: CompareEnginesCommand
   - GitCompare圖標
   - 引擎對比功能

7. LanguageSettingsButton - 翻譯語言設置 (大方框設計)
   - Command: LanguageSettingsCommand
   - 顯示: {SourceLanguage} → {TargetLanguage}
   - 可點選的語言配對顯示

8. DictionaryButton - 自定義詞典
   - Command: OpenDictionaryCommand
   - BookOpen圖標
   - 打開詞典功能

9. QuickScreenshotButton - 快速截圖翻譯
   - Command: QuickScreenshotCommand
   - Camera圖標
   - 快速截圖功能

10. SelectRegionButton - 翻譯區域選擇
    - Command: SelectRegionCommand
    - Square圖標
    - 選擇翻譯區域功能

11. SelectRegion2Button - 翻譯區域2選擇
    - Command: SelectRegion2Command
    - SquareDot圖標
    - 選擇第二個翻譯區域

12. ComicModeButton - 漫畫翻譯優化
    - Command: ComicModeCommand
    - Palette圖標
    - 漫畫模式功能

13. CoverModeToggle - 覆蓋模式切換 (Toggle Switch)
    - IsChecked: {Binding IsCoverModeEnabled}
    - 藍色主題 (#3B82F6)
    - "COVER" 文字標籤

14. AutoTranslateToggle - 自動翻譯開關 (Toggle Switch)
    - IsChecked: {Binding IsAutoTranslateEnabled}
    - 綠色主題 (#22C55E)
    - "AUTO" 文字標籤

15. EditorButton - 編輯窗口
    - Command: OpenEditorCommand
    - Edit3圖標
    - 打開編輯器功能

16. MoreToolsButton - 更多工具
    - Command: ShowMoreToolsCommand
    - MoreHorizontal圖標
    - 顯示更多工具功能

17. SettingsButton - 設置
    - Command: OpenSettingsCommand
    - Settings圖標
    - 打開設置功能

18. MinimizeButton - 最小化
    - Command: MinimizeCommand
    - Minus圖標
    - 最小化窗口功能

19. CloseButton - 關閉
    - Command: ExitCommand
    - X圖標
    - 關閉應用程式功能

20. CollapseToggleButton - 收縮工具條
    - Command: CollapseToggleCommand
    - Tag: {Binding CollapseButtonIcon} (動態圖標)
    - 工具條收縮/展開功能

21. MoveArea - 移動工具條 (拖拽區域)
    - GripVertical圖標
    - MouseLeftButtonDown: Window_MouseLeftButtonDown
    - 拖拽移動工具條功能
*/
