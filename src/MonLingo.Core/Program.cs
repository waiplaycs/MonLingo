using System;
using System.Threading;
using System.Windows;

namespace MonLingo.Core
{
    /// <summary>
    /// MonLingo 應用程式入口點
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 應用程式主入口點
        /// </summary>
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

                // 正常模式：啟動 WPF 應用程式
                var app = new App();
                app.InitializeComponent();
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
    }
}
