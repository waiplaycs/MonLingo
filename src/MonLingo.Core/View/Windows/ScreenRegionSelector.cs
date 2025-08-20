using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 區域選擇視窗 - 用於螢幕截圖區域選擇
    /// </summary>
    public partial class ScreenRegionSelector : Window
    {
        private bool _isSelecting = false;
        private Point _startPoint;
        private Rectangle _selectionRectangle;
        private Canvas _selectionCanvas;

        public event EventHandler<Rect> RegionSelected;

        public ScreenRegionSelector()
        {
            InitializeWindow();
            SetupSelectionCanvas();
        }

        private void InitializeWindow()
        {
            // 設定視窗為全螢幕覆蓋
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)); // 半透明黑色
            Topmost = true;
            ShowInTaskbar = false;
            
            // 設定游標為十字
            Cursor = Cursors.Cross;
        }

        private void SetupSelectionCanvas()
        {
            _selectionCanvas = new Canvas
            {
                Background = Brushes.Transparent
            };
            
            Content = _selectionCanvas;

            // 註冊事件
            _selectionCanvas.MouseLeftButtonDown += OnMouseLeftButtonDown;
            _selectionCanvas.MouseMove += OnMouseMove;
            _selectionCanvas.MouseLeftButtonUp += OnMouseLeftButtonUp;
            _selectionCanvas.KeyDown += OnKeyDown;
            
            // 確保可以接收鍵盤輸入
            Focusable = true;
            Focus();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(_selectionCanvas);
            
            // 創建選擇矩形
            _selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)) // 半透明紅色
            };
            
            Canvas.SetLeft(_selectionRectangle, _startPoint.X);
            Canvas.SetTop(_selectionRectangle, _startPoint.Y);
            _selectionCanvas.Children.Add(_selectionRectangle);
            
            _selectionCanvas.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || _selectionRectangle == null) return;

            var currentPoint = e.GetPosition(_selectionCanvas);
            
            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _startPoint.X);
            var height = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(_selectionRectangle, x);
            Canvas.SetTop(_selectionRectangle, y);
            _selectionRectangle.Width = width;
            _selectionRectangle.Height = height;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting || _selectionRectangle == null) return;

            _isSelecting = false;
            _selectionCanvas.ReleaseMouseCapture();

            // 計算選取的區域
            var x = Canvas.GetLeft(_selectionRectangle);
            var y = Canvas.GetTop(_selectionRectangle);
            var width = _selectionRectangle.Width;
            var height = _selectionRectangle.Height;

            // 檢查區域大小是否有效
            if (width > 10 && height > 10)
            {
                var selectedRegion = new Rect(x, y, width, height);
                RegionSelected?.Invoke(this, selectedRegion);
            }

            Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            RegionSelected = null;
            base.OnClosed(e);
        }
    }
}
