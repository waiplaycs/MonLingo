using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MonLingo.Core.View.Controls;
using MonLingo.Core.View.Windows;
using MonLingo.ViewModel;

namespace MonLingo.Smoke
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Application();
            app.Startup += async (s, e) =>
            {
                var overlay = SelectionBoxOverlay.Instance;
                var vm = new WorkingMainBarWindowViewModel(null);

                // 準備：建立 1 號框
                var box1 = new EnhancedSelectionBox { Number = 1 };
                overlay.AddElement(box1, new Rect(100, 100, 200, 100));

                //(1) 已存在且可見 → 按 10 只應閃爍，不應新建
                vm.SelectRegionCommand.Execute(null);
                await Task.Delay(200);
                var regionsAll1 = overlay.GetAllRegions(includeHidden: true).Where(r => r.number == 1).Count();
                Console.WriteLine($"Case1 Visible Flash, Count(1)={regionsAll1}");

                // (2) 隱藏 → 按 10 應顯示
                box1.Visibility = Visibility.Hidden;
                vm.SelectRegionCommand.Execute(null);
                await Task.Delay(200);
                var visibleAfterShow = overlay.IsBoxVisible(1);
                Console.WriteLine($"Case2 Hidden Show, Visible(1)={visibleAfterShow}");

                // (3) 2 號不存在 → 按 11 進入新建（無法自動畫框，僅驗證不會誤判為存在）
                var has2Before = overlay.HasBox(2);
                Console.WriteLine($"Case3 Before Has2={has2Before}");

                // 結束
                await Task.Delay(250);
                app.Shutdown();
            };
            app.Run();
        }
    }
}
