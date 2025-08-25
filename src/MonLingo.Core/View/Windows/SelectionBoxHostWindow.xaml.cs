using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using NLog;

namespace MonLingo.Core.View.Windows
{
    public partial class SelectionBoxHostWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public SelectionBoxHostWindow()
        {
            InitializeComponent();
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
        }

        public void Host(UIElement element, Rect screenBounds)
        {
            try
            {
                Logger.Info($"[SelectionBoxHostWindow] Host element at {screenBounds}");
                Width = Math.Max(1, screenBounds.Width);
                Height = Math.Max(1, screenBounds.Height);
                Left = screenBounds.Left;
                Top = screenBounds.Top;

                // 確保 grid 只有一個 child
                if (Root.Children.Count > 0)
                    Root.Children.Clear();

                // 將 element 放入，並定位在 (0,0)
                if (element is FrameworkElement fe)
                {
                    fe.HorizontalAlignment = HorizontalAlignment.Left;
                    fe.VerticalAlignment = VerticalAlignment.Top;
                }

                if (element is Control c)
                {
                    // 在宿主內以 Canvas 定位
                    Canvas.SetLeft(c, 0);
                    Canvas.SetTop(c, 0);
                }

                // 使用 Canvas 以保留 EnhancedSelectionBox 的 Canvas 定位語意
                var canvas = new Canvas { Background = System.Windows.Media.Brushes.Transparent };
                canvas.Children.Add(element);
                Root.Children.Add(canvas);

                Show();
                Activate();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[SelectionBoxHostWindow] Failed to host element");
                throw;
            }
        }
    }
}
