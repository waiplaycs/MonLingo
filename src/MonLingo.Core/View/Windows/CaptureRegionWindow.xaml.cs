using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 螢幕擷取區域選擇覆蓋層
    /// 支援全螢幕覆蓋和拖拽選擇區域
    /// </summary>
    public partial class CaptureRegionWindow : Window
    {
        private bool _isSelecting = false;
        private Point _startPoint;
        private Rectangle _selectionRectangle;
        private Rect _selectedRegion;

        public event EventHandler<Rect> RegionSelected;

        public CaptureRegionWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // 設定視窗為虛擬桌面全覆蓋（支援多螢幕）
            this.WindowState = WindowState.Normal;
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // 幾乎透明
            this.Topmost = true;
            this.ShowInTaskbar = false;

            // 設定游標為十字
            this.Cursor = Cursors.Cross;

            // 綁定事件
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseRightButtonDown += OnMouseRightButtonDown; // 新增右鍵事件
            this.KeyDown += OnKeyDown;

            // 確保能接收鍵盤事件
            this.Focusable = true;
            this.Focus();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(this);

            // 創建選擇矩形
            _selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 },
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
            };

            // 添加到畫布
            MainCanvas.Children.Add(_selectionRectangle);
            
            Canvas.SetLeft(_selectionRectangle, _startPoint.X);
            Canvas.SetTop(_selectionRectangle, _startPoint.Y);

            this.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting && _selectionRectangle != null)
            {
                Point currentPoint = e.GetPosition(this);

                double left = Math.Min(_startPoint.X, currentPoint.X);
                double top = Math.Min(_startPoint.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(_selectionRectangle, left);
                Canvas.SetTop(_selectionRectangle, top);
                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;

                _selectedRegion = new Rect(left, top, width, height);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                this.ReleaseMouseCapture();

                // 檢查是否選擇了有效區域
                if (_selectedRegion.Width > 10 && _selectedRegion.Height > 10)
                {
                    // 觸發區域選擇事件
                    RegionSelected?.Invoke(this, _selectedRegion);
                }

                // 關閉視窗
                this.Close();
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 右鍵退出框選模式
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // ESC 鍵取消選擇
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
