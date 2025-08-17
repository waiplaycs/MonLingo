using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MonLingo.ViewModel;

namespace MonLingo.View.Windows
{
    /// <summary>
    /// CaptureRegionWindow - 螢幕擷取區域選擇覆蓋層
    /// 對應 Gaminik.View.CaptureWindow 的功能
    /// </summary>
    public partial class CaptureRegionWindow : Window
    {
        private CaptureRegionViewModel _viewModel;
        private bool _isSelecting = false;
        private Point _startPoint;
        private Point _endPoint;
        private Rectangle _selectionRectangle;

        /// <summary>
        /// 當前選擇的區域
        /// </summary>
        public Rect SelectedRegion { get; private set; }

        public CaptureRegionWindow()
        {
            InitializeComponent();
            InitializeWindow();
            SetupViewModel();
            SetupSelectionRectangle();
        }

        private void InitializeWindow()
        {
            // 設置視窗屬性以實現全螢幕覆蓋
            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
            this.AllowsTransparency = true;
            this.Background = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)); // 半透明黑色
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Cursor = Cursors.Cross;

            // 覆蓋所有螢幕
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
        }

        private void SetupViewModel()
        {
            _viewModel = new CaptureRegionViewModel();
            this.DataContext = _viewModel;

            // 訂閱ViewModel事件
            _viewModel.CancelRequested += OnCancelRequested;
            _viewModel.CaptureCompleted += OnCaptureCompleted;
        }

        private void SetupSelectionRectangle()
        {
            _selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection(new double[] { 5, 3 }),
                Fill = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                Visibility = Visibility.Collapsed
            };

            // 添加到主Canvas
            MainCanvas.Children.Add(_selectionRectangle);
        }

        #region 滑鼠事件處理

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _isSelecting = true;
            _selectionRectangle.Visibility = Visibility.Visible;
            
            this.CaptureMouse();
            e.Handled = true;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                _endPoint = e.GetPosition(this);
                UpdateSelectionRectangle();
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _endPoint = e.GetPosition(this);
                _isSelecting = false;
                this.ReleaseMouseCapture();

                // 計算選擇的區域
                var selectedRect = GetSelectedRectangle();
                
                // 檢查選擇區域是否有效
                if (selectedRect.Width > 10 && selectedRect.Height > 10)
                {
                    CompleteCapture(selectedRect);
                }
                else
                {
                    // 取消選擇
                    CancelCapture();
                }
                
                e.Handled = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelCapture();
                e.Handled = true;
            }
        }

        #endregion

        #region 選擇區域處理

        private void UpdateSelectionRectangle()
        {
            var rect = GetSelectedRectangle();
            
            Canvas.SetLeft(_selectionRectangle, rect.X);
            Canvas.SetTop(_selectionRectangle, rect.Y);
            _selectionRectangle.Width = rect.Width;
            _selectionRectangle.Height = rect.Height;

            // 更新ViewModel中的選擇區域
            _viewModel.UpdateSelection(rect);
        }

        private Rect GetSelectedRectangle()
        {
            var x = Math.Min(_startPoint.X, _endPoint.X);
            var y = Math.Min(_startPoint.Y, _endPoint.Y);
            var width = Math.Abs(_endPoint.X - _startPoint.X);
            var height = Math.Abs(_endPoint.Y - _startPoint.Y);

            return new Rect(x, y, width, height);
        }

        private void CompleteCapture(Rect selectedRect)
        {
            try
            {
                // 轉換為螢幕座標
                var screenRect = new Rect(
                    selectedRect.X + this.Left,
                    selectedRect.Y + this.Top,
                    selectedRect.Width,
                    selectedRect.Height
                );

                // 通知ViewModel完成擷取
                _viewModel.CompleteCapture(screenRect);
                
                // 關閉視窗
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"擷取完成錯誤: {ex.Message}");
                CancelCapture();
            }
        }

        private void CancelCapture()
        {
            try
            {
                _viewModel.CancelCapture();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消擷取錯誤: {ex.Message}");
                this.Close();
            }
        }

        #endregion

        #region ViewModel事件處理

        private void OnCancelRequested(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnCaptureCompleted(object sender, CaptureCompletedEventArgs e)
        {
            // 顯示翻譯結果視窗
            var translationWindow = new TranslationPopupWindow(e.CaptureResult);
            translationWindow.Show();
            
            this.Close();
        }

        #endregion

        #region 視窗生命週期

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 確保視窗獲得焦點
            this.Focus();
            this.Activate();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理資源
            if (_viewModel != null)
            {
                _viewModel.CancelRequested -= OnCancelRequested;
                _viewModel.CaptureCompleted -= OnCaptureCompleted;
                _viewModel.Dispose();
            }

            base.OnClosed(e);
        }

        #endregion

        #region 視覺效果

        /// <summary>
        /// 顯示提示文字
        /// </summary>
        /// <param name="text">提示文字</param>
        private void ShowHintText(string text)
        {
            // 在畫面上顯示操作提示
            var hintTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 2,
                    Opacity = 0.8
                }
            };

            MainCanvas.Children.Add(hintTextBlock);

            // 3秒後自動移除提示
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, e) =>
            {
                MainCanvas.Children.Remove(hintTextBlock);
                timer.Stop();
            };
            timer.Start();
        }

        #endregion
    }

    /// <summary>
    /// 擷取完成事件參數
    /// </summary>
    public class CaptureCompletedEventArgs : EventArgs
    {
        public Rect CaptureResult { get; }

        public CaptureCompletedEventArgs(Rect captureResult)
        {
            CaptureResult = captureResult;
        }
    }
}
