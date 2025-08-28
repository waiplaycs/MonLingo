using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using MonLingo.Core.View.Controls;
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
            this.WindowState = WindowState.Normal;
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Background = Brushes.Transparent;
        }

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
                if (d is EnhancedSelectionBox)
                    return true;
                var parent = VisualTreeHelper.GetParent(d);
                if (parent == null && d is FrameworkElement fe)
                    d = fe.Parent;
                else
                    d = parent;
            }
            return false;
        }

        // 承載任意 UIElement（例如 EnhancedSelectionBox），並設定其位置
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
                if (element is EnhancedSelectionBox eb)
                {
                    Logger.Info($"[SelectionBoxOverlay] AddElement: Box#{eb.Number} at X={rect.X},Y={rect.Y},W={rect.Width},H={rect.Height}");
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
                if (_hostedElements.Count == 0)
                {
                    this.Hide();
                }
            }
        }

        // 依編號取得框
        public EnhancedSelectionBox GetBoxByNumber(int number)
        {
            try
            {
                foreach (var element in _hostedElements)
                {
                    if (element is EnhancedSelectionBox box && box.Number == number)
                    {
                        return box;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[SelectionBoxOverlay] GetBoxByNumber 發生例外");
            }
            return null;
        }

        public bool HasBox(int number)
        {
            var had = GetBoxByNumber(number) != null;
            try
            {
                var nums = _hostedElements.OfType<EnhancedSelectionBox>().Select(b => b.Number).ToArray();
                Logger.Info($"[SelectionBoxOverlay] HasBox({number})={had}; 現有編號=[{string.Join(",", nums)}]; Count={nums.Length}");
            }
            catch { }
            return had;
        }

        public bool ShowBoxByNumber(int number)
        {
            var box = GetBoxByNumber(number);
            if (box == null)
            {
                Logger.Info($"[SelectionBoxOverlay] ShowBoxByNumber: 找不到編號 {number} 的框");
                return false;
            }

            try
            {
                box.Visibility = Visibility.Visible;
                if (!this.IsVisible)
                {
                    this.Show();
                }
                this.Topmost = true;
                this.Activate();
                Logger.Info($"[SelectionBoxOverlay] 已顯示編號 {number} 的框");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[SelectionBoxOverlay] 顯示編號 {number} 的框失敗");
                return false;
            }
        }

        public async Task<bool> FlashBoxByNumberAsync(int number)
        {
            try
            {
                var box = GetBoxByNumber(number);
                if (box == null) return false;
                if (box.Visibility != Visibility.Visible) return false;
                await box.FlashAsync(2, 150);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[SelectionBoxOverlay] FlashBoxByNumberAsync 失敗: {number}");
                return false;
            }
        }

        // 可見性查詢
        public bool IsBoxVisible(int number)
        {
            var box = GetBoxByNumber(number);
            bool result = box != null && box.Visibility == Visibility.Visible;
            Logger.Info($"[SelectionBoxOverlay] IsBoxVisible({number})={result}");
            return result;
        }

        // 回傳所有區域（可選是否包含隱藏框）
        public List<(Rect rect, int number)> GetAllRegions(bool includeHidden)
        {
            var results = new List<(Rect rect, int number)>();
            try
            {
                foreach (var element in _hostedElements)
                {
                    if (element is EnhancedSelectionBox box)
                    {
                        if (!includeHidden && box.Visibility != Visibility.Visible) continue;
                        double left = Canvas.GetLeft(box);
                        double top = Canvas.GetTop(box);
                        double width = box.ActualWidth > 0 ? box.ActualWidth : box.Width;
                        double height = box.ActualHeight > 0 ? box.ActualHeight : box.Height;
                        if (width > 1 && height > 1)
                        {
                            results.Add((new Rect(left, top, width, height), box.Number));
                        }
                    }
                }
                results = results.OrderBy(r => r.number).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[SelectionBoxOverlay] GetAllRegions(includeHidden) 失敗");
            }
            return results;
        }

        // 舊方法：預設排除隱藏框
        public List<(Rect rect, int number)> GetAllRegions()
        {
            return GetAllRegions(includeHidden: false);
        }

        protected override void OnClosed(EventArgs e)
        {
            _instance = null;
            base.OnClosed(e);
        }
    }
}
