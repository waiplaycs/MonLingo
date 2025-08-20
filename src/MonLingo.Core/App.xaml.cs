using System.Windows;

namespace MonLingo.Core
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 設置 UI 文化
            System.Threading.Thread.CurrentThread.CurrentUICulture = 
                new System.Globalization.CultureInfo("zh-TW");
        }
    }
}
