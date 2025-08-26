using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NLog;
using System.Threading.Tasks;

namespace MonLingo.Core.View.Controls
{
    public partial class EnhancedSelectionBox : UserControl
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private bool _isDragging = false;
        private Point _lastPosition;
        private ResizeDirection _currentResize = ResizeDirection.None;
    private bool _isFlashing = false;
        
        public event EventHandler<EventArgs> CloseRequested;
        public event EventHandler<EventArgs> HideRequested;
    public int Number { get; set; }

        public EnhancedSelectionBox()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("🔧 EnhancedSelectionBox 已加載，開始設置事件處理器");
            // 設置左上角編號
            try
            {
                var label = this.FindName("NumberLabel") as TextBlock;
                if (label != null)
                {
                    label.Text = Number.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "設定 NumberLabel 失敗");
            }
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            Logger.Info("🔧 設置所有事件處理器");
            
            // 設置區域懸浮事件 - SettingsArea 改為 Border，使用 FrameworkElement 綁事件
            var settingsArea = this.FindName("SettingsArea") as FrameworkElement;
            if (settingsArea != null)
            {
                settingsArea.MouseEnter += SettingsArea_MouseEnter;
                settingsArea.MouseLeave += SettingsArea_MouseLeave;
                Logger.Info("✅ 設置區域事件處理器已設置");
            }

            // 按鈕事件 - 使用 FindName
            var hideButton = this.FindName("HideButton") as Button;
            var closeButton = this.FindName("CloseButton") as Button;
            
            if (hideButton != null)
                hideButton.Click += (s, e) => HideRequested?.Invoke(this, EventArgs.Empty);
            if (closeButton != null)
                closeButton.Click += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
            
            // 拖拽手柄事件 - 使用 FindName
            var dragHandle = this.FindName("DragHandle") as FrameworkElement;
            if (dragHandle != null)
            {
                dragHandle.MouseLeftButtonDown += DragHandle_MouseLeftButtonDown;
                Logger.Info("✅ 拖拽手柄事件處理器已設置");
            }
            else
            {
                Logger.Error("❌ 找不到 DragHandle 控件，無法設置事件處理器");
            }

            // 調整控制點事件
            SetupResizeHandlers();
            
            // 添加測試事件
            TestMouseEvents();
            
            Logger.Info("✅ 所有事件處理器設置完成");
        }

        private void SettingsArea_MouseEnter(object sender, MouseEventArgs e)
        {
            // 懸浮時顯示兩個按鈕，隱藏3點按鈕
            var settingsButton = this.FindName("SettingsButton") as Button;
            var actionButtons = this.FindName("ActionButtons") as StackPanel;
            
            if (settingsButton != null)
                settingsButton.Visibility = Visibility.Collapsed;
            if (actionButtons != null)
                actionButtons.Visibility = Visibility.Visible;
        }

        private void SettingsArea_MouseLeave(object sender, MouseEventArgs e)
        {
            // 離開區域時恢復 3 點按鈕
            var settingsButton = this.FindName("SettingsButton") as Button;
            var actionButtons = this.FindName("ActionButtons") as StackPanel;
            if (settingsButton != null) settingsButton.Visibility = Visibility.Visible;
            if (actionButtons != null) actionButtons.Visibility = Visibility.Collapsed;
        }

        private void SetupResizeHandlers()
        {
            Logger.Info("🔧 設置調整控制項事件處理器");
            
            // 使用 FindName 設置事件
            var topLeftResize = this.FindName("TopLeftResize") as Ellipse;
            if (topLeftResize != null)
            {
                topLeftResize.MouseLeftButtonDown += (s, e) => {
                    Logger.Info("🖱️ TopLeftResize 點擊");
                    StartResize(ResizeDirection.TopLeft, e);
                };
                Logger.Info("✅ TopLeftResize 事件處理器已設置");
            }
            
            var topRightResize = this.FindName("TopRightResize") as Ellipse;
            if (topRightResize != null)
            {
                topRightResize.MouseLeftButtonDown += (s, e) => {
                    Logger.Info("🖱️ TopRightResize 點擊");
                    StartResize(ResizeDirection.TopRight, e);
                };
                Logger.Info("✅ TopRightResize 事件處理器已設置");
            }
            
            var bottomLeftResize = this.FindName("BottomLeftResize") as Ellipse;
            if (bottomLeftResize != null)
            {
                bottomLeftResize.MouseLeftButtonDown += (s, e) => {
                    Logger.Info("🖱️ BottomLeftResize 點擊");
                    StartResize(ResizeDirection.BottomLeft, e);
                };
                Logger.Info("✅ BottomLeftResize 事件處理器已設置");
            }
            
            var bottomRightResize = this.FindName("BottomRightResize") as Ellipse;
            if (bottomRightResize != null)
            {
                bottomRightResize.MouseLeftButtonDown += (s, e) => {
                    Logger.Info("🖱️ BottomRightResize 點擊");
                    StartResize(ResizeDirection.BottomRight, e);
                };
                Logger.Info("✅ BottomRightResize 事件處理器已設置");
            }

            // 中點控制點已移除，僅保留四角

            // 統一的結束和移動事件
            this.MouseLeftButtonUp += EnhancedSelectionBox_MouseLeftButtonUp;
            this.MouseMove += EnhancedSelectionBox_MouseMove;
            
            Logger.Info("✅ 調整控制項事件處理器設置完成");
        }

        #region 拖拽功能

        private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Logger.Info("🖱️ 拖拽開始");
            _isDragging = true;
            _lastPosition = e.GetPosition(null);
            // 讓整個 UserControl 捕獲滑鼠，不是 DragHandle
            this.CaptureMouse();
            e.Handled = true;
        }

        // 新增測試方法
        private void TestMouseEvents()
        {
            // 直接測試滑鼠事件
            var dragHandle = this.FindName("DragHandle") as FrameworkElement;
            if (dragHandle != null)
            {
                dragHandle.MouseEnter += (s, e) => Logger.Info("🖱️ 拖拽手柄 - 滑鼠進入");
                dragHandle.MouseLeave += (s, e) => Logger.Info("🖱️ 拖拽手柄 - 滑鼠離開");
                
                // 強制設置基本屬性
                dragHandle.IsHitTestVisible = true;
                dragHandle.Focusable = true;
                
                Logger.Info("✅ 拖拽手柄測試事件已添加");
            }
            else
            {
                Logger.Error("❌ 找不到 DragHandle 控件");
            }

            var topLeftResize = this.FindName("TopLeftResize") as Ellipse;
            if (topLeftResize != null)
            {
                topLeftResize.MouseEnter += (s, e) => Logger.Info("🖱️ TopLeft - 滑鼠進入");
                topLeftResize.MouseLeave += (s, e) => Logger.Info("🖱️ TopLeft - 滑鼠離開");
                
                // 強制設置基本屬性
                topLeftResize.IsHitTestVisible = true;
                topLeftResize.Focusable = true;
                
                Logger.Info("✅ TopLeft 測試事件已添加");
            }
            else
            {
                Logger.Error("❌ 找不到 TopLeftResize 控件");
            }
            
            // 測試整個 UserControl 的滑鼠事件
            this.MouseEnter += (s, e) => Logger.Info("🖱️ UserControl - 滑鼠進入");
            this.MouseLeave += (s, e) => Logger.Info("🖱️ UserControl - 滑鼠離開");
            Logger.Info("✅ UserControl 測試事件已添加");
        }

        #endregion

        #region 調整大小功能

        private enum ResizeDirection
        {
            None,
            Top, Bottom, Left, Right,
            TopLeft, TopRight, BottomLeft, BottomRight
        }

        private void StartResize(ResizeDirection direction, MouseButtonEventArgs e)
        {
            Logger.Info($"🔧 開始調整大小: {direction}");
            _currentResize = direction;
            _lastPosition = e.GetPosition(null);
            this.CaptureMouse();
            e.Handled = true;
        }

        private void EnhancedSelectionBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentResize != ResizeDirection.None)
            {
                Logger.Info("🔧 結束調整大小");
                _currentResize = ResizeDirection.None;
                this.ReleaseMouseCapture();
            }
            else if (_isDragging)
            {
                Logger.Info("🖱️ 拖拽結束");
                _isDragging = false;
                this.ReleaseMouseCapture();
            }
        }

        private void EnhancedSelectionBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentResize != ResizeDirection.None && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(null);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                ResizeBox(deltaX, deltaY);
                _lastPosition = currentPosition;
            }
            else if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(null);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                Logger.Info($"🖱️ 拖拽移動: Delta: {deltaX}, {deltaY}");

                // 移動整個控件
                var newLeft = Canvas.GetLeft(this) + deltaX;
                var newTop = Canvas.GetTop(this) + deltaY;
                
                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);

                _lastPosition = currentPosition;
            }
        }

        private void ResizeBox(double deltaX, double deltaY)
        {
            var currentLeft = Canvas.GetLeft(this);
            var currentTop = Canvas.GetTop(this);
            var currentWidth = this.ActualWidth;
            var currentHeight = this.ActualHeight;

            Logger.Info($"🔧 調整大小: {_currentResize}, Delta: {deltaX}, {deltaY}");

            switch (_currentResize)
            {
                case ResizeDirection.Top:
                    Canvas.SetTop(this, currentTop + deltaY);
                    this.Height = Math.Max(50, currentHeight - deltaY);
                    break;

                case ResizeDirection.Bottom:
                    this.Height = Math.Max(50, currentHeight + deltaY);
                    break;

                case ResizeDirection.Left:
                    Canvas.SetLeft(this, currentLeft + deltaX);
                    this.Width = Math.Max(50, currentWidth - deltaX);
                    break;

                case ResizeDirection.Right:
                    this.Width = Math.Max(50, currentWidth + deltaX);
                    break;

                case ResizeDirection.TopLeft:
                    Canvas.SetLeft(this, currentLeft + deltaX);
                    Canvas.SetTop(this, currentTop + deltaY);
                    this.Width = Math.Max(50, currentWidth - deltaX);
                    this.Height = Math.Max(50, currentHeight - deltaY);
                    break;

                case ResizeDirection.TopRight:
                    Canvas.SetTop(this, currentTop + deltaY);
                    this.Width = Math.Max(50, currentWidth + deltaX);
                    this.Height = Math.Max(50, currentHeight - deltaY);
                    break;

                case ResizeDirection.BottomLeft:
                    Canvas.SetLeft(this, currentLeft + deltaX);
                    this.Width = Math.Max(50, currentWidth - deltaX);
                    this.Height = Math.Max(50, currentHeight + deltaY);
                    break;

                case ResizeDirection.BottomRight:
                    this.Width = Math.Max(50, currentWidth + deltaX);
                    this.Height = Math.Max(50, currentHeight + deltaY);
                    break;
            }
        }

        #endregion

        public void SetPosition(double left, double top, double width, double height)
        {
            Logger.Info($"🔧 設置位置: X={left}, Y={top}, W={width}, H={height}");
            
            // 設置 UserControl 的尺寸
            this.Width = width;
            this.Height = height;
            
            // 如果在 Canvas 中，設置 Canvas 位置
            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);
            
            // 強制更新佈局
            this.UpdateLayout();
            
            Logger.Info($"✅ 位置設置完成: ActualWidth={this.ActualWidth}, ActualHeight={this.ActualHeight}");
        }

        /// <summary>
        /// 讓選框高亮閃爍幾次（不阻塞 UI）。
        /// </summary>
        public async Task FlashAsync(int times = 2, int periodMs = 150)
        {
            if (_isFlashing) return;
            _isFlashing = true;
            try
            {
                var mainBorder = this.FindName("MainBorder") as Shape;
                if (mainBorder == null)
                {
                    Logger.Warn("[EnhancedSelectionBox] 未找到 MainBorder，改用整體透明度閃爍");
                    for (int i = 0; i < times; i++)
                    {
                        this.Opacity = 0.5;
                        await Task.Delay(periodMs);
                        this.Opacity = 1.0;
                        await Task.Delay(periodMs);
                    }
                    return;
                }

                var originalStroke = mainBorder.Stroke;
                var originalThickness = mainBorder.StrokeThickness;
                var highlightBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // 金黃

                for (int i = 0; i < times; i++)
                {
                    mainBorder.Stroke = highlightBrush;
                    mainBorder.StrokeThickness = Math.Max(3, originalThickness + 2);
                    await Task.Delay(periodMs);
                    mainBorder.Stroke = originalStroke;
                    mainBorder.StrokeThickness = originalThickness;
                    await Task.Delay(periodMs);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[EnhancedSelectionBox] FlashAsync 發生例外");
            }
            finally
            {
                _isFlashing = false;
            }
        }
    }
}