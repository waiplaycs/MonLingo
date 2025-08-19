using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
            InitializeComponent();
            
            // 創建並設置 ViewModel
            _viewModel = new SubtitleViewModel();
            DataContext = _viewModel;
            
            // 訂閱新行添加事件
            _viewModel.NewLineAdded += OnNewLineAdded;
            
            // 載入時隱藏視窗（初始狀態）
            Opacity = 0;
            
            // 設置視窗事件
            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
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
        /// 顯示字幕視窗（淡入效果）
        /// </summary>
        public void ShowSubtitle()
        {
            Show();
            ShowWithFadeIn();
        }

        /// <summary>
        /// 隱藏字幕視窗（淡出效果）
        /// </summary>
        public void HideSubtitle()
        {
            HideWithFadeOut();
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

        /// <summary>
        /// 調整大小手柄滑鼠按下事件
        /// </summary>
        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
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
