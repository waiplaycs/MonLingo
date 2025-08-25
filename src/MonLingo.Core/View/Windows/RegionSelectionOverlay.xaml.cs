using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using MonLingo.Core.View.Controls;
using MonLingo.Core.View.Windows;
using NLog;
using System.Windows.Interop;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 區域選擇覆蓋層 - 完整的 bb.png 樣式實現
    /// </summary>
    public partial class RegionSelectionOverlay : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private bool _isSelecting = false;
        private bool _isDragging = false;
        private bool _isResizing = false;
    // 是否仍允許開始新的框選（框選模式）
    private bool _selectionModeEnabled = true;
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
            Logger.Info($"[Overlay] 建立 RegionSelectionOverlay，RegionNumber={_regionNumber}");
            this.SourceInitialized += (s, e) => AttachHwndHook();
        }

        private void InitializeWindow()
        {
            // 設置窗口屬性
            this.WindowState = WindowState.Normal; // 跨多螢幕使用虛擬桌面尺寸
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // 幾乎透明的背景
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Cursor = Cursors.Cross;
            Logger.Info("[Overlay] 初始化視窗屬性完成（Topmost/VirtualScreen/AlmostTransparent=1）");
            
            // 徹底禁用右鍵菜單
            this.ContextMenu = null;
            
            // 事件處理
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseRightButtonDown += OnMouseRightButtonDown; // 新增右鍵事件
            // 右鍵預覽事件，避免系統層級點擊／選單觸發
            this.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
            this.PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp;

            // 全域攔截（即便子元素已將事件標記為 Handled 也能攔住）
            this.AddHandler(UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(OnPreviewMouseRightButtonDown), true);
            this.AddHandler(UIElement.PreviewMouseRightButtonUpEvent, new MouseButtonEventHandler(OnPreviewMouseRightButtonUp), true);
            this.KeyDown += OnKeyDown;
            this.Focusable = true;
            this.Focus();
            Logger.Info("[Overlay] 事件綁定完成，開始攔截右鍵（含 handledEventsToo）");
        }

        // === 原生訊息攔截，徹底阻止右鍵與系統選單 ===
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_NCRBUTTONDOWN = 0x00A4;
    private const int WM_NCRBUTTONUP = 0x00A5;
    private const int WM_CONTEXTMENU = 0x007B;
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

        private void AttachHwndHook()
        {
            try
            {
                var source = (HwndSource)PresentationSource.FromVisual(this);
                if (source != null)
                {
                    source.AddHook(WndProc);
                    Logger.Info("[Overlay] HwndSource Hook 已附加（右鍵原生訊息將被攔截）");
                }
                else
                {
                    Logger.Warn("[Overlay] 無法取得 HwndSource，原生訊息攔截未啟用");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[Overlay] 附加 HwndSource Hook 失敗");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    // 退出選擇模式後，讓覆蓋層可點穿（僅框本身可互動）
                    if (!_selectionModeEnabled && !_isSelecting)
                    {
                        try
                        {
                            long lp = lParam.ToInt64();
                            int x = unchecked((short)(lp & 0xFFFF));
                            int y = unchecked((short)((lp >> 16) & 0xFFFF));
                            var screenPt = new Point(x, y);
                            var pt = this.PointFromScreen(screenPt);

                            // 命中測試，若命中增強框或其子元素，則讓 WPF 正常處理；否則點穿
                            var hit = VisualTreeHelper.HitTest(this, pt)?.VisualHit as DependencyObject;
                            bool overBox = IsOverEnhancedSelectionBox(hit);
                            if (!overBox)
                            {
                                handled = true;
                                //Logger.Info($"[Overlay] WM_NCHITTEST 點穿 at {screenPt}");
                                return new IntPtr(HTTRANSPARENT);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "[Overlay] WM_NCHITTEST 點穿判斷失敗，回退預設處理");
                        }
                    }
                    break;
                case WM_RBUTTONDOWN:
                case WM_RBUTTONUP:
                case WM_NCRBUTTONDOWN:
                case WM_NCRBUTTONUP:
                case WM_CONTEXTMENU:
                    // 僅在選擇模式時攔截右鍵，避免影響正常桌面互動
                    if (_selectionModeEnabled || _isSelecting)
                    {
                        handled = true;
                        Logger.Info($"[Overlay] NativeHook 攔截 msg=0x{msg:X}");
                        CancelSelectionMode();
                        return IntPtr.Zero;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private bool IsOverEnhancedSelectionBox(DependencyObject d)
        {
            while (d != null)
            {
                if (d is MonLingo.Core.View.Controls.EnhancedSelectionBox)
                    return true;
                var parent = VisualTreeHelper.GetParent(d);
                if (parent == null && d is FrameworkElement fe)
                    d = fe.Parent;
                else
                    d = parent;
            }
            return false;
        }

        // 統一的『退出框選模式』（關閉視窗，讓系統背景可點擊；不移除已存在於 SelectionBoxOverlay 的框）
        private void CancelSelectionMode()
        {
        Logger.Info("[Overlay] CancelSelectionMode 觸發：釋放捕獲、清除臨時框、退出框選模式並關閉視窗");
            try { this.ReleaseMouseCapture(); } catch { }
            if (_isSelecting)
            {
                _isSelecting = false;
                if (_currentSelectionBox != null)
                {
            Logger.Info("[Overlay] 移除臨時選擇框");
                    _currentSelectionBox.RemoveFromCanvas();
                    _currentSelectionBox = null;
                }
            }
            // 停用框選模式但保留視窗與既有框供互動
            _selectionModeEnabled = false;
            this.Cursor = Cursors.Arrow;
            this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

            // 完全退出：關閉覆蓋層視窗
            try
            {
                Logger.Info("[Overlay] 關閉 RegionSelectionOverlay 以完全退出");
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[Overlay] 關閉視窗失敗");
            }
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        var pos = e.GetPosition(this);
        Logger.Info($"[Overlay] PreviewMouseRightButtonDown at {pos}; Source={e.Source?.GetType().Name}, OriginalSource={e.OriginalSource?.GetType().Name} — 將取消並攔截");
            if (_selectionModeEnabled || _isSelecting)
            {
                e.Handled = true; // 只在選擇模式時攔截右鍵
                CancelSelectionMode();
            }
        }

        private void OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        var pos = e.GetPosition(this);
        Logger.Info($"[Overlay] PreviewMouseRightButtonUp at {pos}; 已攔截");
            if (_selectionModeEnabled || _isSelecting)
            {
                e.Handled = true; // 只在選擇模式時攔截
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mainCanvas = this.FindName("MainCanvas") as Canvas;
            if (mainCanvas == null) return;

            // 使用 HitTest 判斷是否點擊在子控制項（例如拖拽/縮放控制點或邊框）上
            Point clickPoint = e.GetPosition(mainCanvas);
            var hit = mainCanvas.InputHitTest(clickPoint) as DependencyObject;
            Logger.Info($"[Overlay] MouseLeftButtonDown at {clickPoint}; Hit={(hit?.GetType().Name ?? "null")} ");
            if (hit != null && hit != mainCanvas)
            {
                // 命中的是子元素，讓事件交由子元素處理（不在此開始新的框選）
                Logger.Info("[Overlay] 命中子元素，將事件傳遞給子控制項處理");
                return;
            }
            
            if (!_isSelecting && !_isDragging && !_isResizing)
            {
                if (!_selectionModeEnabled)
                {
                    Logger.Info("[Overlay] 框選模式已停用，忽略左鍵開始");
                    return;
                }
                // 確保只有一個框 - 清除現有的框
                // 不再清空既有增強框，僅管理臨時框
                
                _isSelecting = true;
                _startPoint = e.GetPosition(mainCanvas);
                
                // 創建新的選擇框 - 始終使用區域編號作為框編號
                _currentSelectionBox = new SelectionBox(_regionNumber, _regionNumber, this);
                _selectionBoxes.Add(_currentSelectionBox);
                
                // 添加到 Canvas
                _currentSelectionBox.AddToCanvas(mainCanvas);
                _currentSelectionBox.SetPosition(_startPoint.X, _startPoint.Y, 0, 0);
                
                this.CaptureMouse();
                Logger.Info("[Overlay] 開始框選並捕獲滑鼠");
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
                var mainCanvas = this.FindName("MainCanvas") as Canvas;
                if (mainCanvas == null) return;
                
                Point currentPoint = e.GetPosition(mainCanvas);
                // 輕量級日誌，避免刷爆
                // Logger.Info($"[Overlay] MouseMove {currentPoint}");
                
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
                    Logger.Info($"[Overlay] MouseLeftButtonUp 完成框選: X={rect.X}, Y={rect.Y}, W={rect.Width}, H={rect.Height}");
                    if (rect.Width > 10 && rect.Height > 10) // 最小尺寸檢查
                    {
                        // 創建增強型選擇框來替換當前的選擇框
                        CreateEnhancedSelectionBox(rect);
                        
                        // 移除舊的選擇框
                        _currentSelectionBox.RemoveFromCanvas();
                        _selectionBoxes.Remove(_currentSelectionBox);
                        
                        RegionSelected?.Invoke(this, (rect, _regionNumber));
                        
                        // 框選完成後：退出框選模式（禁止再次框選），保留框供互動
                        _selectionModeEnabled = false;
                        this.Cursor = Cursors.Arrow;
                        this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                        Logger.Info("[Overlay] 框選完成：退出框選模式並保留框");
                        
                        // 框永久保留，不設置自動關閉計時器
                    }
                    else
                    {
                        // 移除太小的選擇框
                        Logger.Info("[Overlay] 框太小，丟棄本次框選");
                        _currentSelectionBox.RemoveFromCanvas();
                        _selectionBoxes.Remove(_currentSelectionBox);
                    }
                }
                _currentSelectionBox = null;
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 後備處理（理論上 Preview 事件已處理）
            var pos = e.GetPosition(this);
            Logger.Info($"[Overlay] MouseRightButtonDown (後備) at {pos} — 將取消");
            if (_selectionModeEnabled || _isSelecting)
            {
                e.Handled = true;
                CancelSelectionMode();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Logger.Info("[Overlay] ESC 觸發取消");
                CancelSelectionMode();
            }
        }

        private void CreateEnhancedSelectionBox(Rect rect)
        {
            Logger.Info($"🔧 創建增強選擇框: X={rect.X}, Y={rect.Y}, W={rect.Width}, H={rect.Height}");
            
            // 創建增強型選擇框
            var enhancedBox = new EnhancedSelectionBox { Number = _regionNumber };
            
            Logger.Info("✅ EnhancedSelectionBox 實例已創建");
            
            // 設置位置和大小
            enhancedBox.SetPosition(rect.Left, rect.Top, rect.Width, rect.Height);
            
            // 重要：設置 Canvas 定位屬性
            Canvas.SetLeft(enhancedBox, rect.Left);
            Canvas.SetTop(enhancedBox, rect.Top);
            enhancedBox.Width = rect.Width;
            enhancedBox.Height = rect.Height;
            
            Logger.Info("✅ EnhancedSelectionBox 位置已設置");
            
            // 設置事件處理器
            enhancedBox.CloseRequested += (sender, e) =>
            {
                Logger.Info("🔴 EnhancedSelectionBox 關閉請求");
                SelectionBoxOverlay.Instance.RemoveElement(enhancedBox);
            };
            
            enhancedBox.HideRequested += (sender, e) =>
            {
                Logger.Info("🙈 EnhancedSelectionBox 隱藏請求");
                enhancedBox.Visibility = Visibility.Hidden;
            };
            
            // 添加到全域 SelectionBoxOverlay（只在框範圍攔截，其他區域點穿）
            SelectionBoxOverlay.Instance.AddElement(enhancedBox, rect);
            Logger.Info("✅ EnhancedSelectionBox 已移交至 SelectionBoxOverlay");

            // 完成後完全退出覆蓋層
            try
            {
                Logger.Info("[Overlay] 框選完成，關閉 RegionSelectionOverlay 以釋放螢幕");
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[Overlay] 關閉視窗失敗（完成框選後）");
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
            _parentWindow?.RemoveSelectionBox(this);
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