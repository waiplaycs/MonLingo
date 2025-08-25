using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using NLog;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 獨立的選擇框覆蓋層 - 用於保存選擇框而不依賴於區域選擇窗口
    /// </summary>
    public partial class SelectionBoxOverlay : Window
    {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static SelectionBoxOverlay _instance;
    private List<SelectionBox> _selectionBoxes = new List<SelectionBox>();
    private readonly List<UIElement> _hostedElements = new List<UIElement>();

        public static SelectionBoxOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SelectionBoxOverlay();
                }
                return _instance;
            }
        }

        private SelectionBoxOverlay()
        {
            InitializeComponent();
            InitializeWindow();
            this.SourceInitialized += (s, e) => AttachHwndHook();
        }

        private void InitializeWindow()
        {
            // 設置窗口屬性
            this.WindowState = WindowState.Normal;
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Background = Brushes.Transparent;
        }

        // 只讓框本身可互動，其餘位置點穿到系統
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;
        private void AttachHwndHook()
        {
            try
            {
                var source = (HwndSource)PresentationSource.FromVisual(this);
                source?.AddHook(WndProc);
                Logger.Info("[SelectionBoxOverlay] HwndSource Hook 已附加");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[SelectionBoxOverlay] 附加 HwndSource Hook 失敗");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    try
                    {
                        long lp = lParam.ToInt64();
                        int x = unchecked((short)(lp & 0xFFFF));
                        int y = unchecked((short)((lp >> 16) & 0xFFFF));
                        var screenPt = new Point(x, y);
                        var pt = this.PointFromScreen(screenPt);
                        var hit = VisualTreeHelper.HitTest(this, pt)?.VisualHit as DependencyObject;
                        if (!IsOverHostedElement(hit))
                        {
                            handled = true;
                            return new IntPtr(HTTRANSPARENT);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "[SelectionBoxOverlay] WM_NCHITTEST 處理失敗");
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private bool IsOverHostedElement(DependencyObject d)
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

        public void AddSelectionBox(SelectionBox box)
        {
            if (box != null && !_selectionBoxes.Contains(box))
            {
                _selectionBoxes.Add(box);
                box.AddToCanvas(MainCanvas);
                
                // 確保窗口顯示
                if (!this.IsVisible)
                {
                    this.Show();
                }
            }
        }

        // 通用：承載任意 UIElement（例如 EnhancedSelectionBox），並設定其位置
        public void AddElement(UIElement element, Rect rect)
        {
            if (element == null) return;
            if (!_hostedElements.Contains(element))
            {
                _hostedElements.Add(element);
                if (element is FrameworkElement fe)
                {
                    fe.Width = rect.Width;
                    fe.Height = rect.Height;
                }
                Canvas.SetLeft(element, rect.Left);
                Canvas.SetTop(element, rect.Top);
                MainCanvas.Children.Add(element);

                if (!this.IsVisible)
                    this.Show();
                this.Topmost = true;
                this.Activate();
            }
        }

        public void RemoveElement(UIElement element)
        {
            if (element == null) return;
            if (_hostedElements.Contains(element))
            {
                MainCanvas.Children.Remove(element);
                _hostedElements.Remove(element);
                if (_hostedElements.Count == 0 && _selectionBoxes.Count == 0)
                {
                    this.Hide();
                }
            }
        }

        public void RemoveSelectionBox(SelectionBox box)
        {
            if (box != null && _selectionBoxes.Contains(box))
            {
                box.RemoveFromCanvas();
                _selectionBoxes.Remove(box);
                
                // 如果沒有選擇框了，隱藏窗口
                if (_selectionBoxes.Count == 0 && _hostedElements.Count == 0)
                {
                    this.Hide();
                }
            }
        }

        public void ClearAllBoxes()
        {
            foreach (var box in _selectionBoxes.ToArray())
            {
                RemoveSelectionBox(box);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _instance = null;
            base.OnClosed(e);
        }
    }
}
