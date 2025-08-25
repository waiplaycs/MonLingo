using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 區域選擇覆蓋層 - 完整的 bb.png 樣式實現
    /// </summary>
    public partial class RegionSelectionOverlay : Window
    {
        private bool _isSelecting = false;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private Point _startPoint;
        private Point _lastMousePosition;
        private int _regionNumber;
        private SelectionBox _currentSelectionBox;
        private List<SelectionBox> _selectionBoxes = new List<SelectionBox>();

        public event EventHandler<(Rect region, int number)> RegionSelected;

        public RegionSelectionOverlay(int regionNumber = 1)
        {
            InitializeComponent();
            _regionNumber = regionNumber;
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // 設置窗口屬性
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // 幾乎透明的背景
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Cursor = Cursors.Cross;
            
            // 事件處理
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseRightButtonDown += OnMouseRightButtonDown; // 新增右鍵事件
            this.KeyDown += OnKeyDown;
            this.Focusable = true;
            this.Focus();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting && !_isDragging && !_isResizing)
            {
                // 確保只有一個框 - 清除現有的框
                ClearAllSelections();
                
                _isSelecting = true;
                _startPoint = e.GetPosition(MainCanvas);
                
                // 創建新的選擇框 - 始終使用區域編號作為框編號
                _currentSelectionBox = new SelectionBox(_regionNumber, _regionNumber, this);
                _selectionBoxes.Add(_currentSelectionBox);
                
                // 添加到 Canvas
                _currentSelectionBox.AddToCanvas(MainCanvas);
                _currentSelectionBox.SetPosition(_startPoint.X, _startPoint.Y, 0, 0);
                
                this.CaptureMouse();
            }
        }

        // 新增清除所有選擇的方法
        private void ClearAllSelections()
        {
            foreach (var box in _selectionBoxes)
            {
                box.RemoveFromCanvas();
            }
            _selectionBoxes.Clear();
            _currentSelectionBox = null;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting && _currentSelectionBox != null)
            {
                Point currentPoint = e.GetPosition(MainCanvas);
                
                double left = Math.Min(_startPoint.X, currentPoint.X);
                double top = Math.Min(_startPoint.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);
                
                _currentSelectionBox.SetPosition(left, top, width, height);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                this.ReleaseMouseCapture();
                
                if (_currentSelectionBox != null)
                {
                    var rect = _currentSelectionBox.GetRect();
                    if (rect.Width > 10 && rect.Height > 10) // 最小尺寸檢查
                    {
                        _currentSelectionBox.ShowControls();
                        RegionSelected?.Invoke(this, (rect, _regionNumber));
                    }
                    else
                    {
                        // 移除太小的選擇框
                        _currentSelectionBox.RemoveFromCanvas();
                        _selectionBoxes.Remove(_currentSelectionBox);
                    }
                }
                _currentSelectionBox = null;
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 右鍵退出框選模式
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        public void RemoveSelectionBox(SelectionBox box)
        {
            if (_selectionBoxes.Contains(box))
            {
                box.RemoveFromCanvas();
                _selectionBoxes.Remove(box);
                
                // 重新編號
                for (int i = 0; i < _selectionBoxes.Count; i++)
                {
                    _selectionBoxes[i].UpdateNumber(i + 1);
                }
            }
        }
    }

    /// <summary>
    /// 選擇框類 - 實現完整的 bb.png 樣式功能
    /// </summary>
    public class SelectionBox
    {
        private Rectangle _mainRectangle;
        private TextBlock _numberLabel;
        private Button _settingsButton;
        private StackPanel _actionButtonsPanel;
        private Button _hideButton;
        private Button _closeButton;
        private Ellipse _dragHandle;
        
        // 8個調整控制點
        private Ellipse _topLeftHandle, _topRightHandle, _bottomLeftHandle, _bottomRightHandle;
        private Rectangle _topHandle, _bottomHandle, _leftHandle, _rightHandle;
        
        private Canvas _parentCanvas;
        private RegionSelectionOverlay _parentWindow;
        private int _regionType;
        private int _boxNumber;
        private bool _isHidden = false;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private Point _lastMousePosition;
        private string _resizeDirection = "";

        public SelectionBox(int regionType, int boxNumber, RegionSelectionOverlay parentWindow)
        {
            _regionType = regionType;
            _boxNumber = boxNumber;
            _parentWindow = parentWindow;
            CreateElements();
        }

        private void CreateElements()
        {
            var strokeBrush = _regionType == 1 ? Brushes.Red : Brushes.Blue;
            // 去除框底色 - 設置為透明
            var fillColor = Colors.Transparent;

            // 主選擇框 - 更幼框線
            _mainRectangle = new Rectangle
            {
                Stroke = strokeBrush,
                StrokeThickness = 1.5, // 更幼的框線
                StrokeDashArray = new DoubleCollection { 3, 3 }, // 更細的虛線
                Fill = new SolidColorBrush(fillColor) // 無底色
            };

            // 1. 左上角編號標示 - 現代化設計
            _numberLabel = new TextBlock
            {
                Text = _boxNumber.ToString(),
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(200, 33, 150, 243)), // 現代藍色背景
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(6, 3, 6, 3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 2. 右上角設置按鈕 - 現代化設計
            _settingsButton = new Button
            {
                Content = "⋯",
                FontSize = 14,
                Width = 24,
                Height = 24,
                Background = new SolidColorBrush(Color.FromArgb(220, 245, 245, 245)), // 半透明白色
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 158, 158, 158)),
                BorderThickness = new Thickness(1),
                Visibility = Visibility.Collapsed
            };
            _settingsButton.MouseEnter += OnSettingsButtonMouseEnter;
            _settingsButton.MouseLeave += OnSettingsButtonMouseLeave;

            // 隱藏/關閉按鈕面板 - 現代化設計
            _actionButtonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Visibility = Visibility.Collapsed
            };

            _hideButton = new Button
            {
                Content = "👁",
                FontSize = 12,
                Width = 24,
                Height = 24,
                Background = new SolidColorBrush(Color.FromArgb(220, 227, 242, 253)), // 現代藍色
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 33, 150, 243)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 1, 0)
            };
            _hideButton.Click += OnHideButtonClick;

            _closeButton = new Button
            {
                Content = "✕",
                FontSize = 12,
                Width = 24,
                Height = 24,
                Background = new SolidColorBrush(Color.FromArgb(220, 255, 235, 238)), // 現代紅色
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 244, 67, 54)),
                BorderThickness = new Thickness(1)
            };
            _closeButton.Click += OnCloseButtonClick;

            _actionButtonsPanel.Children.Add(_hideButton);
            _actionButtonsPanel.Children.Add(_closeButton);

            // 3. 中間上方拖拽手柄 - 現代化設計
            _dragHandle = new Ellipse
            {
                Width = 24,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromArgb(200, 96, 125, 139)), // 現代灰藍色
                Cursor = Cursors.SizeAll,
                Visibility = Visibility.Collapsed
            };
            _dragHandle.MouseLeftButtonDown += OnDragHandleMouseDown;
            _dragHandle.MouseMove += OnDragHandleMouseMove;
            _dragHandle.MouseLeftButtonUp += OnDragHandleMouseUp;

            // 4. 創建8個調整控制點
            CreateResizeHandles(strokeBrush);
        }

        private void CreateResizeHandles(Brush brush)
        {
            // 四角控制點 - 改為小方塊，去除白圓點
            _topLeftHandle = CreateCornerHandle(Cursors.SizeNWSE, "nw-resize");
            _topRightHandle = CreateCornerHandle(Cursors.SizeNESW, "ne-resize");
            _bottomLeftHandle = CreateCornerHandle(Cursors.SizeNESW, "sw-resize");
            _bottomRightHandle = CreateCornerHandle(Cursors.SizeNWSE, "se-resize");

            // 四邊控制點
            _topHandle = CreateEdgeHandle(Cursors.SizeNS, "n-resize", 40, 6);
            _bottomHandle = CreateEdgeHandle(Cursors.SizeNS, "s-resize", 40, 6);
            _leftHandle = CreateEdgeHandle(Cursors.SizeWE, "w-resize", 6, 40);
            _rightHandle = CreateEdgeHandle(Cursors.SizeWE, "e-resize", 6, 40);
        }

        private Ellipse CreateCornerHandle(Cursor cursor, string direction)
        {
            // 改為小而現代的設計，去除明顯的白圓點
            var handle = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), // 半透明白色
                Stroke = new SolidColorBrush(Color.FromArgb(200, 33, 150, 243)), // 現代藍色邊框
                StrokeThickness = 1,
                Cursor = cursor,
                Visibility = Visibility.Collapsed
            };
            
            handle.MouseLeftButtonDown += (s, e) => StartResize(direction, e);
            handle.MouseMove += OnResizeHandleMouseMove;
            handle.MouseLeftButtonUp += OnResizeHandleMouseUp;
            
            return handle;
        }

        private Rectangle CreateEdgeHandle(Cursor cursor, string direction, double width, double height)
        {
            var handle = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.Transparent,
                Cursor = cursor,
                Visibility = Visibility.Collapsed
            };
            
            handle.MouseLeftButtonDown += (s, e) => StartResize(direction, e);
            handle.MouseMove += OnResizeHandleMouseMove;
            handle.MouseLeftButtonUp += OnResizeHandleMouseUp;
            
            return handle;
        }

        public void AddToCanvas(Canvas canvas)
        {
            _parentCanvas = canvas;
            
            // 按順序添加所有元素
            canvas.Children.Add(_mainRectangle);
            canvas.Children.Add(_numberLabel);
            canvas.Children.Add(_settingsButton);
            canvas.Children.Add(_actionButtonsPanel);
            canvas.Children.Add(_dragHandle);
            
            // 添加調整控制點
            canvas.Children.Add(_topLeftHandle);
            canvas.Children.Add(_topRightHandle);
            canvas.Children.Add(_bottomLeftHandle);
            canvas.Children.Add(_bottomRightHandle);
            canvas.Children.Add(_topHandle);
            canvas.Children.Add(_bottomHandle);
            canvas.Children.Add(_leftHandle);
            canvas.Children.Add(_rightHandle);
        }

        public void RemoveFromCanvas()
        {
            if (_parentCanvas != null)
            {
                _parentCanvas.Children.Remove(_mainRectangle);
                _parentCanvas.Children.Remove(_numberLabel);
                _parentCanvas.Children.Remove(_settingsButton);
                _parentCanvas.Children.Remove(_actionButtonsPanel);
                _parentCanvas.Children.Remove(_dragHandle);
                
                // 移除調整控制點
                _parentCanvas.Children.Remove(_topLeftHandle);
                _parentCanvas.Children.Remove(_topRightHandle);
                _parentCanvas.Children.Remove(_bottomLeftHandle);
                _parentCanvas.Children.Remove(_bottomRightHandle);
                _parentCanvas.Children.Remove(_topHandle);
                _parentCanvas.Children.Remove(_bottomHandle);
                _parentCanvas.Children.Remove(_leftHandle);
                _parentCanvas.Children.Remove(_rightHandle);
            }
        }

        public void SetPosition(double left, double top, double width, double height)
        {
            // 設置主矩形
            Canvas.SetLeft(_mainRectangle, left);
            Canvas.SetTop(_mainRectangle, top);
            _mainRectangle.Width = width;
            _mainRectangle.Height = height;

            // 設置編號標籤（左上角）
            Canvas.SetLeft(_numberLabel, left - 15);
            Canvas.SetTop(_numberLabel, top - 20);

            // 設置右上角設置按鈕
            Canvas.SetLeft(_settingsButton, left + width - 20);
            Canvas.SetTop(_settingsButton, top - 25);

            // 設置操作按鈕面板
            Canvas.SetLeft(_actionButtonsPanel, left + width - 50);
            Canvas.SetTop(_actionButtonsPanel, top - 25);

            // 設置中間上方拖拽手柄
            Canvas.SetLeft(_dragHandle, left + width / 2 - 10);
            Canvas.SetTop(_dragHandle, top - 15);

            // 設置調整控制點
            UpdateResizeHandles(left, top, width, height);
        }

        private void UpdateResizeHandles(double left, double top, double width, double height)
        {
            // 四角控制點
            Canvas.SetLeft(_topLeftHandle, left - 4);
            Canvas.SetTop(_topLeftHandle, top - 4);
            
            Canvas.SetLeft(_topRightHandle, left + width - 4);
            Canvas.SetTop(_topRightHandle, top - 4);
            
            Canvas.SetLeft(_bottomLeftHandle, left - 4);
            Canvas.SetTop(_bottomLeftHandle, top + height - 4);
            
            Canvas.SetLeft(_bottomRightHandle, left + width - 4);
            Canvas.SetTop(_bottomRightHandle, top + height - 4);

            // 四邊控制點
            Canvas.SetLeft(_topHandle, left + width / 2 - 30);
            Canvas.SetTop(_topHandle, top - 4);
            
            Canvas.SetLeft(_bottomHandle, left + width / 2 - 30);
            Canvas.SetTop(_bottomHandle, top + height - 4);
            
            Canvas.SetLeft(_leftHandle, left - 4);
            Canvas.SetTop(_leftHandle, top + height / 2 - 30);
            
            Canvas.SetLeft(_rightHandle, left + width - 4);
            Canvas.SetTop(_rightHandle, top + height / 2 - 30);
        }

        public void ShowControls()
        {
            _settingsButton.Visibility = Visibility.Visible;
            _dragHandle.Visibility = Visibility.Visible;
            
            // 顯示調整控制點
            _topLeftHandle.Visibility = Visibility.Visible;
            _topRightHandle.Visibility = Visibility.Visible;
            _bottomLeftHandle.Visibility = Visibility.Visible;
            _bottomRightHandle.Visibility = Visibility.Visible;
            _topHandle.Visibility = Visibility.Visible;
            _bottomHandle.Visibility = Visibility.Visible;
            _leftHandle.Visibility = Visibility.Visible;
            _rightHandle.Visibility = Visibility.Visible;
        }

        public void HideControls()
        {
            _settingsButton.Visibility = Visibility.Collapsed;
            _dragHandle.Visibility = Visibility.Collapsed;
            _actionButtonsPanel.Visibility = Visibility.Collapsed;
            
            // 隱藏調整控制點
            _topLeftHandle.Visibility = Visibility.Collapsed;
            _topRightHandle.Visibility = Visibility.Collapsed;
            _bottomLeftHandle.Visibility = Visibility.Collapsed;
            _bottomRightHandle.Visibility = Visibility.Collapsed;
            _topHandle.Visibility = Visibility.Collapsed;
            _bottomHandle.Visibility = Visibility.Collapsed;
            _leftHandle.Visibility = Visibility.Collapsed;
            _rightHandle.Visibility = Visibility.Collapsed;
        }

        public Rect GetRect()
        {
            return new Rect(
                Canvas.GetLeft(_mainRectangle),
                Canvas.GetTop(_mainRectangle),
                _mainRectangle.Width,
                _mainRectangle.Height
            );
        }

        public void UpdateNumber(int newNumber)
        {
            _boxNumber = newNumber;
            _numberLabel.Text = newNumber.ToString();
        }

        // 事件處理器
        private void OnSettingsButtonMouseEnter(object sender, MouseEventArgs e)
        {
            _settingsButton.Visibility = Visibility.Collapsed;
            _actionButtonsPanel.Visibility = Visibility.Visible;
        }

        private void OnSettingsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            // 延遲隱藏，讓用戶有時間點擊按鈕
        }

        private void OnHideButtonClick(object sender, RoutedEventArgs e)
        {
            _isHidden = !_isHidden;
            if (_isHidden)
            {
                _mainRectangle.Visibility = Visibility.Collapsed;
                _hideButton.Content = "👁‍🗨";
                HideControls();
            }
            else
            {
                _mainRectangle.Visibility = Visibility.Visible;
                _hideButton.Content = "👁";
                ShowControls();
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            _parentWindow.RemoveSelectionBox(this);
        }

        private void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(_parentCanvas);
            ((UIElement)sender).CaptureMouse();
        }

        private void OnDragHandleMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(_parentCanvas);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;

                var rect = GetRect();
                SetPosition(rect.Left + deltaX, rect.Top + deltaY, rect.Width, rect.Height);
                
                _lastMousePosition = currentPosition;
            }
        }

        private void OnDragHandleMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        private void StartResize(string direction, MouseButtonEventArgs e)
        {
            _isResizing = true;
            _resizeDirection = direction;
            _lastMousePosition = e.GetPosition(_parentCanvas);
        }

        private void OnResizeHandleMouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                Point currentPosition = e.GetPosition(_parentCanvas);
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;

                var rect = GetRect();
                double newLeft = rect.Left;
                double newTop = rect.Top;
                double newWidth = rect.Width;
                double newHeight = rect.Height;

                // 根據調整方向調整尺寸
                switch (_resizeDirection)
                {
                    case "nw-resize":
                        newLeft += deltaX;
                        newTop += deltaY;
                        newWidth -= deltaX;
                        newHeight -= deltaY;
                        break;
                    case "ne-resize":
                        newTop += deltaY;
                        newWidth += deltaX;
                        newHeight -= deltaY;
                        break;
                    case "sw-resize":
                        newLeft += deltaX;
                        newWidth -= deltaX;
                        newHeight += deltaY;
                        break;
                    case "se-resize":
                        newWidth += deltaX;
                        newHeight += deltaY;
                        break;
                    case "n-resize":
                        newTop += deltaY;
                        newHeight -= deltaY;
                        break;
                    case "s-resize":
                        newHeight += deltaY;
                        break;
                    case "w-resize":
                        newLeft += deltaX;
                        newWidth -= deltaX;
                        break;
                    case "e-resize":
                        newWidth += deltaX;
                        break;
                }

                // 確保最小尺寸
                if (newWidth > 10 && newHeight > 10)
                {
                    SetPosition(newLeft, newTop, newWidth, newHeight);
                }
                
                _lastMousePosition = currentPosition;
            }
        }

        private void OnResizeHandleMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
        }
    }
}