using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using MonLingo.Core.ViewModel;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// SubtitleWindow.xaml 的互動邏輯
    /// 實現可分離、可拖拽、可縮放的字幕視窗
    /// </summary>
    public partial class SubtitleWindow : Window
    {
        #region Fields

        private SubtitleViewModel _viewModel;
        private bool _isAnimating = false;

        #endregion

        #region Constructor

        public SubtitleWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔨 SubtitleWindow() 建構函數開始");
                
                System.Diagnostics.Debug.WriteLine("📦 調用 InitializeComponent()");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✅ InitializeComponent() 完成");
                
                // 創建並設置 ViewModel
                System.Diagnostics.Debug.WriteLine("🧠 創建 SubtitleViewModel");
                _viewModel = new SubtitleViewModel();
                DataContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("✅ ViewModel 設置完成");
                
                // 訂閱新行添加事件
                System.Diagnostics.Debug.WriteLine("🔗 訂閱事件");
                _viewModel.NewLineAdded += OnNewLineAdded;
                
                // 載入時隱藏視窗（初始狀態）
                System.Diagnostics.Debug.WriteLine("👁️ 設置初始透明度");
                Opacity = 0;
                
                // 初始化動畫變換
                System.Diagnostics.Debug.WriteLine("🎭 初始化動畫變換");
                InitializeAnimationTransforms();
                System.Diagnostics.Debug.WriteLine("✅ 動畫變換初始化完成");
                
                // 設置視窗事件
                System.Diagnostics.Debug.WriteLine("📋 設置視窗事件");
                Loaded += OnWindowLoaded;
                Closed += OnWindowClosed;
                
                System.Diagnostics.Debug.WriteLine("🎊 SubtitleWindow() 建構函數完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SubtitleWindow() 建構函數錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"📍 錯誤詳情: {ex}");
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
                RootContainer.RenderTransform = transformGroup;
                RootContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                
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
            if (e.ChangedButton == MouseButton.Left && _viewModel?.IsDetached == true)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // 忽略拖拽異常
                }
            }
        }

        /// <summary>
        /// 視窗雙擊事件 - 切換分離/依附模式
        /// </summary>
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel != null)
            {
                if (_viewModel.IsDetached)
                {
                    _viewModel.ReattachSubtitleWindow();
                }
                else
                {
                    _viewModel.DetachSubtitleWindow();
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
                    // 記錄初始狀態
                    var startPoint = PointToScreen(e.GetPosition(this));
                    var startWidth = Width;
                    var startHeight = Height;
                    
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
                            
                            // 直接設置視窗大小，避免綁定延遲
                            Width = newWidth;
                            Height = newHeight;
                            
                            // 同步更新 ViewModel 中的大小
                            if (_viewModel != null)
                            {
                                _viewModel.WindowWidth = newWidth;
                                _viewModel.WindowHeight = newHeight;
                            }
                        }
                    };
                    
                    mouseUpHandler = (s, args) =>
                    {
                        // 清理事件處理器
                        MouseMove -= mouseMoveHandler;
                        MouseUp -= mouseUpHandler;
                        ReleaseMouseCapture();
                    };
                    
                    // 註冊事件處理器
                    MouseMove += mouseMoveHandler;
                    MouseUp += mouseUpHandler;
                }
                catch
                {
                    // 忽略拖拽異常，確保滑鼠釋放
                    ReleaseMouseCapture();
                }
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
                    DragMove();
                }
                catch
                {
                    // 忽略拖拽異常
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
            var scrollViewer = SubtitleScrollViewer;
            
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
