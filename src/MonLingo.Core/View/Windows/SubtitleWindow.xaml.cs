using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using MonLingo.Core.ViewModel;
using NLog;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// SubtitleWindow.xaml 的互動邏輯
    /// 實現可分離、可拖拽、可縮放的字幕視窗
    /// </summary>
    public partial class SubtitleWindow : Window
    {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        #region Fields

        private SubtitleViewModel _viewModel;
        private bool _isAnimating = false;
        private Point _dragStartPosition; // 記錄拖拽開始位置
        private readonly double _minMovementThreshold = 5.0; // 最小移動閾值（像素）

        #endregion

        #region Constructor

        public SubtitleWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔨 SubtitleWindow() 建構函數開始");
                Logger.Info("SubtitleWindow ctor start");
                
                System.Diagnostics.Debug.WriteLine("📦 調用 InitializeComponent()");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✅ InitializeComponent() 完成");
                Logger.Debug("InitializeComponent completed");
                
                // 創建並設置 ViewModel
                System.Diagnostics.Debug.WriteLine("🧠 創建 SubtitleViewModel");
                _viewModel = new SubtitleViewModel();
                DataContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("✅ ViewModel 設置完成");
                Logger.Debug("ViewModel created and set as DataContext");
                
                // 訂閱新行添加事件
                System.Diagnostics.Debug.WriteLine("🔗 訂閱事件");
                _viewModel.NewLineAdded += OnNewLineAdded;
                
                // 訂閱視窗狀態變化事件
                this.StateChanged += OnWindowStateChanged;
                
                // 載入時隱藏視窗（初始狀態）
                System.Diagnostics.Debug.WriteLine("👁️ 設置初始透明度");
                Opacity = 0;
                
                // 初始化動畫變換
                System.Diagnostics.Debug.WriteLine("🎭 初始化動畫變換");
                InitializeAnimationTransforms();
                System.Diagnostics.Debug.WriteLine("✅ 動畫變換初始化完成");
                Logger.Debug("Animation transforms initialized");
                
                // 設置視窗事件
                System.Diagnostics.Debug.WriteLine("📋 設置視窗事件");
                Loaded += OnWindowLoaded;
                Closed += OnWindowClosed;
                
                System.Diagnostics.Debug.WriteLine("🎊 SubtitleWindow() 建構函數完成");
                Logger.Info("SubtitleWindow ctor end");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SubtitleWindow() 建構函數錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"📍 錯誤詳情: {ex}");
                Logger.Error(ex, "SubtitleWindow ctor error");
                throw;
            }
        }

        /// <summary>
        /// 帶主工具條引用的建構函式
        /// </summary>
        /// <param name="mainBarWindow">主工具條視窗</param>
        public SubtitleWindow(Window mainBarWindow) : this()
        {
            SetMainBarWindow(mainBarWindow);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化動畫變換
        /// </summary>
        private void InitializeAnimationTransforms()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎭 開始初始化動畫變換");
                
                // 設置初始變換組合 - 應用到根容器而不是視窗
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(0.95, 0.95));
                transformGroup.Children.Add(new TranslateTransform(0, 10));
                
                // 將變換應用到根容器而不是視窗本身
                var root = this.FindName("RootContainer") as UIElement;
                if (root != null)
                {
                    root.RenderTransform = transformGroup;
                    root.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                
                System.Diagnostics.Debug.WriteLine("✅ 動畫變換初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 初始化動畫變換時發生錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"📍 錯誤詳情: {ex}");
                
                // 如果變換失敗，我們可以繼續，只是沒有動畫效果
                // 不要重新拋出異常，避免影響視窗創建
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 設置主工具條視窗引用
        /// </summary>
        /// <param name="mainBarWindow">主工具條視窗</param>
        public void SetMainBarWindow(Window mainBarWindow)
        {
            _viewModel?.SetMainBarWindow(mainBarWindow);
        }

        /// <summary>
        /// 顯示字幕視窗（增強的順滑動畫效果）
        /// </summary>
        public void ShowSubtitle()
        {
            Show();
            ShowWithEnhancedAnimation();
        }

        /// <summary>
        /// 隱藏字幕視窗（增強的順滑淡出效果）
        /// </summary>
        public void HideSubtitle()
        {
            HideWithEnhancedAnimation();
        }

        /// <summary>
        /// 添加新的字幕行
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="translatedText">譯文</param>
        public void AddSubtitleLine(string originalText, string translatedText)
        {
            _viewModel?.AddNewLine(originalText, translatedText);
        }

        /// <summary>
        /// 清空所有字幕行
        /// </summary>
        public void ClearSubtitles()
        {
            _viewModel?.ClearLines();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 視窗載入事件處理
        /// </summary>
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 初始化完成
            try
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                Logger.Info($"SubtitleWindow loaded: DPIScale=({dpi.DpiScaleX:F2},{dpi.DpiScaleY:F2}), IsDetached={_viewModel?.IsDetached}, IsLocked={_viewModel?.IsLocked}, L={Left}, T={Top}, W={ActualWidth}, H={ActualHeight}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "OnWindowLoaded logging failed");
            }
        }

        /// <summary>
        /// 視窗關閉事件處理
        /// </summary>
        private void OnWindowClosed(object sender, EventArgs e)
        {
            // 清理資源
            if (_viewModel != null)
            {
                _viewModel.NewLineAdded -= OnNewLineAdded;
            }
            this.StateChanged -= OnWindowStateChanged;
            Logger.Info("SubtitleWindow closed");
        }

        /// <summary>
        /// 視窗狀態變化事件處理
        /// </summary>
        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            try
            {
                Logger.Debug($"SubtitleWindow state changed to: {this.WindowState}");
                
                // 如果字幕視窗在吸附狀態下被最小化，則一起最小化主工具條
                if (this.WindowState == WindowState.Minimized && 
                    _viewModel != null && 
                    !_viewModel.IsDetached)
                {
                    Logger.Info("SubtitleWindow minimized in attached state, minimizing MainBarWindow too");
                    
                    // 尋找主工具條視窗並最小化
                    var mainBarWindow = Application.Current.Windows.OfType<MainBarWindow>().FirstOrDefault();
                    if (mainBarWindow?.DataContext is MonLingo.ViewModel.WorkingMainBarWindowViewModel mainViewModel)
                    {
                        // 觸發主工具條的最小化命令
                        if (mainViewModel.MinimizeCommand?.CanExecute(null) == true)
                        {
                            mainViewModel.MinimizeCommand.Execute(null);
                            Logger.Debug("MainBarWindow minimize command executed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling window state change");
            }
        }

        /// <summary>
    /// 一鍵複製所有譯文至剪貼簿
        /// </summary>
        private void CopyAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
        if (_viewModel == null || _viewModel.SubtitleLines == null || _viewModel.SubtitleLines.Count == 0)
            return;

                var all = string.Join(Environment.NewLine,
                    _viewModel.SubtitleLines
                        .Where(line => !string.IsNullOrWhiteSpace(line.TranslatedText))
                        .Select(line => line.TranslatedText));

                if (string.IsNullOrWhiteSpace(all))
                    return;

                Clipboard.SetText(all);

                // 顯示置中 1 秒的提示
                ShowCenterToast("✓複製成功", TimeSpan.FromSeconds(1));
            }
            catch
            {
                // ignore minimal errors
            }
        }

        private bool _toastShowing = false;
        private async void ShowCenterToast(string message, TimeSpan duration)
        {
            try
            {
                if (_toastShowing) return;
                _toastShowing = true;

                var toastText = this.FindName("CenterToastText") as TextBlock;
                var toast = this.FindName("CenterToast") as UIElement;
                if (toastText != null) toastText.Text = message;
                if (toast is FrameworkElement toastFe) toastFe.Visibility = Visibility.Visible;

                var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(150)))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                toast?.BeginAnimation(OpacityProperty, fadeIn);

                await System.Threading.Tasks.Task.Delay(duration);

                var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(200)))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                fadeOut.Completed += (s, e) =>
                {
                    if (toast is FrameworkElement fe) fe.Visibility = Visibility.Collapsed;
                    _toastShowing = false;
                };
                toast?.BeginAnimation(OpacityProperty, fadeOut);
            }
            catch
            {
                _toastShowing = false;
            }
        }

        /// <summary>
        /// 新行添加事件處理 - 觸發自動滾動
        /// </summary>
        private void OnNewLineAdded()
        {
            if (!_isAnimating)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ScrollToBottom();
                }));
            }
        }

        /// <summary>
        /// 視窗滑鼠按下事件 - 支援拖拽
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (_viewModel?.IsLocked == true) return; // 鎖定時不可拖移
                // 若點擊在互動元件上，則不啟動拖移
                if (IsInteractiveElement(e.OriginalSource as DependencyObject)) return;
                try
                {
                    Logger.Debug($"DragStart Window: L={Left}, T={Top}, W={ActualWidth}, H={ActualHeight}, IsDetached={_viewModel?.IsDetached}, IsLocked={_viewModel?.IsLocked}");
                    var oldLeft = Left;
                    var oldTop = Top;
                    DragMove();

                    // 拖移結束後嘗試吸附到工具條底部（需接近）
                    TrySnapAttachToToolbarBottom(oldLeft, oldTop);
                    Logger.Debug($"DragEnd Window: L={Left}, T={Top}, IsDetached={_viewModel?.IsDetached}");
                }
                catch (Exception ex)
                {
                    // 忽略拖拽異常
                    Logger.Warn(ex, "DragMove exception (Window_MouseDown)");
                }
            }
        }

        // 判斷點擊目標是否為互動元件（按鈕、輸入、滾動條、拖拽手把等）
        private bool IsInteractiveElement(DependencyObject d)
        {
            while (d != null)
            {
                // 只檢查真正的互動控件，不包括一般的容器
                if (d is Button || d is TextBox || d is PasswordBox || d is ComboBox || d is Slider)
                    return true;
                
                // 特別檢查縮放手把區域
                if (d is FrameworkElement fe && fe.Name == "ResizeGrip")
                    return true;
                    
                d = VisualTreeHelper.GetParent(d);
            }
            return false;
        }

        /// <summary>
        /// 視窗雙擊事件 - 智慧切換模式
        /// 吸附時：分離+解鎖 (一步到位)
        /// 分離且解鎖時：鎖定
        /// 分離且鎖定時：解鎖
        /// </summary>
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel != null)
            {
                if (!_viewModel.IsDetached)
                {
                    // 目前為吸附狀態 -> 分離+解鎖 (一步到位)
                    _viewModel.DetachSubtitleWindow();
                    _viewModel.IsLocked = false;
                    Logger.Info("Double-click while attached -> Detach + Unlock");
                }
                else
                {
                    // 目前為分離狀態 -> 切換鎖定狀態
                    _viewModel.IsLocked = !_viewModel.IsLocked;
                    Logger.Info($"Double-click while detached -> Toggle lock to {_viewModel.IsLocked}");
                }
            }
        }

        /// <summary>
        /// 拖拽縮放控制項的滑鼠按下事件
        /// </summary>
        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // 防止事件冒泡到父視窗
                e.Handled = true;
                
                // 開始視窗縮放拖拽
                try
                {
                    // 只有分離且鎖定時才不可縮放，吸附狀態允許縮放
                    if (_viewModel?.IsLocked == true && _viewModel?.IsDetached == true) return; 
                    // 記錄初始狀態
                    var startPoint = PointToScreen(e.GetPosition(this));
                    var startWidth = Width;
                    var startHeight = Height;
                    var startLeft = Left;  // 記錄初始左邊位置
                    var startTop = Top;    // 記錄初始頂部位置
            Logger.Debug($"ResizeStart: W={startWidth}, H={startHeight}, L={startLeft}, T={startTop}");
                    
                    // 捕獲滑鼠
                    CaptureMouse();
                    
                    // 定義滑鼠移動處理
                    MouseEventHandler mouseMoveHandler = null;
                    MouseButtonEventHandler mouseUpHandler = null;
                    
                    mouseMoveHandler = (s, args) =>
                    {
                        if (args.LeftButton == MouseButtonState.Pressed)
                        {
                            var currentPoint = PointToScreen(args.GetPosition(this));
                            var deltaX = currentPoint.X - startPoint.X;
                            var deltaY = currentPoint.Y - startPoint.Y;
                            
                            // 計算新的視窗大小，確保不小於最小值
                            var newWidth = Math.Max(MinWidth, startWidth + deltaX);
                            var newHeight = Math.Max(MinHeight, startHeight + deltaY);
                            
                            // 固定左上角位置，只調整右下角
                            Left = startLeft;
                            Top = startTop;
                            Width = newWidth;
                            Height = newHeight;
                            
                            // 同步更新 ViewModel 中的大小和位置
                            if (_viewModel != null)
                            {
                                _viewModel.WindowLeft = startLeft;
                                _viewModel.WindowTop = startTop;
                                _viewModel.WindowWidth = newWidth;
                                _viewModel.WindowHeight = newHeight;
                Logger.Trace($"Resizing: W={newWidth}, H={newHeight}, L={startLeft}, T={startTop}");
                            }
                        }
                    };
                    
                    mouseUpHandler = (s, args) =>
                    {
                        // 清理事件處理器
                        MouseMove -= mouseMoveHandler;
                        MouseUp -= mouseUpHandler;
                        ReleaseMouseCapture();
            Logger.Debug($"ResizeEnd: W={Width}, H={Height}, L={Left}, T={Top}");
                    };
                    
                    // 註冊事件處理器
                    MouseMove += mouseMoveHandler;
                    MouseUp += mouseUpHandler;
                }
        catch (Exception ex)
                {
                    // 忽略拖拽異常，確保滑鼠釋放
                    ReleaseMouseCapture();
            Logger.Warn(ex, "Resize exception");
                }
            }
        }

        // 嘗試吸附至工具條底部：僅當視窗底邊接近工具條底邊一定閾值且水平重疊足夠時
        private void TrySnapAttachToToolbarBottom(double oldLeft, double oldTop)
        {
            try
            {
                if (_viewModel == null || _viewModel.MainBarWindow == null) return;
                if (_viewModel.IsLocked) return;

                var toolbar = _viewModel.MainBarWindow;
                // 視窗和工具條的矩形
                Rect winRect = new Rect(Left, Top, ActualWidth, ActualHeight);
                Rect barRect = new Rect(toolbar.Left, toolbar.Top, toolbar.ActualWidth, toolbar.ActualHeight);

                // 擴大吸附條件：
                // 1) 與工具條有任何垂直方向重疊，且水平至少有 1px 重疊 -> 直接吸附
                // 2) 否則沿用「距離底邊 16px 內且水平重疊 ≥ 40px」規則
                const double verticalThreshold = 16;
                const double horizontalOverlapMin = 40;

                // 垂直重疊（任意交集）
                double verticalOverlap = Math.Min(winRect.Bottom, barRect.Bottom) - Math.Max(winRect.Top, barRect.Top);
                double horizontalOverlap = Math.Min(winRect.Right, barRect.Right) - Math.Max(winRect.Left, barRect.Left);

                bool hasAnyOverlap = verticalOverlap > 0 && horizontalOverlap > 0;

                // 距離條件
                double distanceToBarBottom = Math.Abs(winRect.Top - (barRect.Bottom));
                Logger.Debug($"SnapCheck: vOverlap={verticalOverlap}, hOverlap={horizontalOverlap}, distBottom={distanceToBarBottom}");

                if (hasAnyOverlap || (distanceToBarBottom <= verticalThreshold && horizontalOverlap >= horizontalOverlapMin))
                {
                    // 進行依附：對齊寬度與工具條一致，位置緊貼工具條底部（完全無縫隙）
                    _viewModel.IsDetached = false;
                    _viewModel.IsLocked = true; // 吸附時自動鎖定
                    _viewModel.WindowLeft = toolbar.Left;
                    _viewModel.WindowTop = toolbar.Top + toolbar.ActualHeight - 10; // 向上調整10px增加重疊
                    _viewModel.WindowWidth = toolbar.ActualWidth;
                    Logger.Info($"Snapped to toolbar bottom and auto-locked: L={_viewModel.WindowLeft}, T={_viewModel.WindowTop}, W={_viewModel.WindowWidth}");
                    // 高度使用依附標準高度（由 VM 控制）
                }
                else
                {
                    // 沒有吸附：切換為分離狀態，並同步目前位置與大小
                    _viewModel.IsDetached = true;
                    _viewModel.WindowLeft = Left;
                    _viewModel.WindowTop = Top;
                    _viewModel.WindowWidth = ActualWidth;
                    _viewModel.WindowHeight = ActualHeight;
                    Logger.Debug("Not snapped: conditions not met -> set IsDetached=true and keep current position/size");
                }
            }
            catch (Exception ex)
            {
                // 安全失敗，不做任何事
                Logger.Warn(ex, "Snap attach check failed");
            }
        }

        /// <summary>
        /// 標題列滑鼠按下事件 - 拖拽視窗
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    // 若點擊在互動元件上，則不啟動拖移（避免按鈕被誤當拖移）
                    if (IsInteractiveElement(e.OriginalSource as DependencyObject)) return;
                    if (_viewModel?.IsLocked == true) return; // 鎖定時不可拖移
                    
                    // 記錄拖拽開始位置
                    _dragStartPosition = new Point(Left, Top);
                    Logger.Debug($"TitleBar drag start from: L={_dragStartPosition.X}, T={_dragStartPosition.Y}");
                    
                    var oldLeft = Left;
                    var oldTop = Top;
                    DragMove();
                    
                    // 計算移動距離
                    var moveDistance = Math.Sqrt(Math.Pow(Left - _dragStartPosition.X, 2) + Math.Pow(Top - _dragStartPosition.Y, 2));
                    Logger.Debug($"TitleBar drag end: L={Left}, T={Top}, MoveDistance={moveDistance:F2}px");
                    
                    // 只有在真正移動了足夠距離時才嘗試吸附
                    if (moveDistance >= _minMovementThreshold)
                    {
                        TrySnapAttachToToolbarBottom(oldLeft, oldTop);
                        Logger.Debug($"Movement threshold met, snap attempted. IsDetached={_viewModel?.IsDetached}");
                    }
                    else
                    {
                        Logger.Debug("Movement too small, snap skipped to prevent unwanted re-attachment");
                    }
                }
                catch (Exception ex)
                {
                    // 忽略拖拽異常
                    Logger.Warn(ex, "DragMove exception (TitleBar)");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 平滑滾動到底部
        /// </summary>
        private void ScrollToBottom()
        {
            // 使用動畫滾動到底部
            var scrollViewer = this.FindName("SubtitleScrollViewer") as ScrollViewer;
            
            if (scrollViewer != null)
            {
                // 更新佈局以確保新項目被正確測量
                scrollViewer.UpdateLayout();
                
                // 創建滾動動畫
                var animation = new DoubleAnimation
                {
                    From = scrollViewer.VerticalOffset,
                    To = scrollViewer.ScrollableHeight,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                // 應用動畫
                scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
            }
        }

        /// <summary>
        /// 淡入顯示視窗
        /// </summary>
        private void ShowWithFadeIn()
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// 淡出隱藏視窗
        /// </summary>
        private void HideWithFadeOut()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) =>
            {
                Hide();
            };

            BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// 增強的順滑顯示動畫 - 結合淡入、縮放和滑入效果
        /// </summary>
        private void ShowWithEnhancedAnimation()
        {
            try
            {
                // 設置初始狀態
                Opacity = 0;
                
                // 確保視窗有 Transform
                var transformGroup = new TransformGroup();
                var scaleTransform = new ScaleTransform(0.95, 0.95);
                var translateTransform = new TranslateTransform(0, 10);
                transformGroup.Children.Add(scaleTransform);
                transformGroup.Children.Add(translateTransform);
                RenderTransform = transformGroup;
                RenderTransformOrigin = new Point(0.5, 0.5);

                // 創建動畫組
                var storyboard = new Storyboard();

                // 1. 淡入動畫
                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeInAnimation, this);
                Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));

                // 2. 縮放動畫（放大效果）
                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(350),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                Storyboard.SetTarget(scaleXAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));

                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(350),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                Storyboard.SetTarget(scaleYAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath(ScaleTransform.ScaleYProperty));

                // 3. 滑入動畫（向上滑入）
                var slideInAnimation = new DoubleAnimation
                {
                    From = 10,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(slideInAnimation, translateTransform);
                Storyboard.SetTargetProperty(slideInAnimation, new PropertyPath(TranslateTransform.YProperty));

                // 添加所有動畫到故事板
                storyboard.Children.Add(fadeInAnimation);
                storyboard.Children.Add(scaleXAnimation);
                storyboard.Children.Add(scaleYAnimation);
                storyboard.Children.Add(slideInAnimation);

                // 開始動畫
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // 如果動畫失敗，直接顯示視窗
                Opacity = 1;
                RenderTransform = Transform.Identity;
                System.Diagnostics.Debug.WriteLine($"字幕視窗動畫錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 增強的順滑隱藏動畫 - 結合淡出、縮放和滑出效果
        /// </summary>
        private void HideWithEnhancedAnimation()
        {
            try
            {
                // 獲取當前的變換組件
                var transformGroup = RenderTransform as TransformGroup;
                ScaleTransform scaleTransform = null;
                TranslateTransform translateTransform = null;

                if (transformGroup != null && transformGroup.Children.Count >= 2)
                {
                    scaleTransform = transformGroup.Children[0] as ScaleTransform;
                    translateTransform = transformGroup.Children[1] as TranslateTransform;
                }

                // 如果沒有找到變換，創建新的
                if (scaleTransform == null || translateTransform == null)
                {
                    transformGroup = new TransformGroup();
                    scaleTransform = new ScaleTransform(1.0, 1.0);
                    translateTransform = new TranslateTransform(0, 0);
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(translateTransform);
                    RenderTransform = transformGroup;
                    RenderTransformOrigin = new Point(0.5, 0.5);
                }

                // 創建動畫組
                var storyboard = new Storyboard();

                // 1. 淡出動畫
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeOutAnimation, this);
                Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(OpacityProperty));

                // 2. 縮小動畫
                var scaleXAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.95,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(scaleXAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));

                var scaleYAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.95,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(scaleYAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath(ScaleTransform.ScaleYProperty));

                // 3. 滑出動畫（向下滑出）
                var slideOutAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(slideOutAnimation, translateTransform);
                Storyboard.SetTargetProperty(slideOutAnimation, new PropertyPath(TranslateTransform.YProperty));

                // 動畫完成後隱藏視窗
                storyboard.Completed += (s, e) =>
                {
                    Hide();
                    // 重置變換為下次顯示做準備
                    var newTransformGroup = new TransformGroup();
                    newTransformGroup.Children.Add(new ScaleTransform(0.95, 0.95));
                    newTransformGroup.Children.Add(new TranslateTransform(0, 10));
                    RenderTransform = newTransformGroup;
                };

                // 添加所有動畫到故事板
                storyboard.Children.Add(fadeOutAnimation);
                storyboard.Children.Add(scaleXAnimation);
                storyboard.Children.Add(scaleYAnimation);
                storyboard.Children.Add(slideOutAnimation);

                // 開始動畫
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // 如果動畫失敗，直接隱藏視窗
                Hide();
                System.Diagnostics.Debug.WriteLine($"字幕視窗隱藏動畫錯誤: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// ScrollViewer 行為輔助類別，用於動畫滾動
    /// </summary>
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
                new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static double GetVerticalOffset(ScrollViewer scrollViewer)
        {
            return (double)scrollViewer.GetValue(VerticalOffsetProperty);
        }

        public static void SetVerticalOffset(ScrollViewer scrollViewer, double value)
        {
            scrollViewer.SetValue(VerticalOffsetProperty, value);
        }

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
