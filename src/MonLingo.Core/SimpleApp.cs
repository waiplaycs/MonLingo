using System;
using System.Threading;
using System.Windows;
using MonLingo.View.Windows;

namespace MonLingo.Core
{
    /// <summary>
    /// 最小化啟動程式 - 僅用於測試 UI
    /// </summary>
    public class SimpleApp : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // 確保執行緒為 STA 模式
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
                }

                // 檢查是否為測試模式
                if (args.Length > 0 && args[0] == "--test")
                {
                    // 測試模式：運行測試程式
                    TestProgram.Main(args).Wait();
                    return;
                }

                // 最小化 WPF 應用程式
                var app = new SimpleApp();
                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"應用程式啟動失敗: {ex.Message}\n\n詳細錯誤: {ex}", 
                    "MonLingo - 啟動錯誤", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 直接建立主視窗，使用安全的初始化方式
                var mainWindow = new MainBarWindow();
                
                // 確保視窗可見
                mainWindow.Show();
                mainWindow.Activate();
                
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UI 啟動失敗: {ex.Message}\n\n詳細信息: {ex.ToString()}", "MonLingo", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
