using System;
using System.Threading.Tasks;
using System.Windows;
using MonLingo.Core.View.Windows;
using MonLingo.Core.ViewModel;

namespace MonLingo.Core.Test
{
    /// <summary>
    /// 字幕窗口測試類
    /// </summary>
    public static class SubtitleWindowTest
    {
        /// <summary>
        /// 測試字幕窗口顯示
        /// </summary>
        public static void TestSubtitleWindow()
        {
            try
            {
                Console.WriteLine("開始測試字幕窗口...");
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 創建字幕窗口
                    var subtitleWindow = new SubtitleWindow();
                    Console.WriteLine("字幕窗口已創建");
                    
                    // 顯示窗口
                    subtitleWindow.Show();
                    Console.WriteLine($"字幕窗口已顯示，可見性: {subtitleWindow.IsVisible}");
                    
                    // 添加測試字幕
                    var viewModel = subtitleWindow.DataContext as SubtitleViewModel;
                    if (viewModel != null)
                    {
                        viewModel.AddNewLine("測試原文", "測試翻譯結果");
                        Console.WriteLine("已添加測試字幕");
                    }
                    else
                    {
                        Console.WriteLine("ViewModel 為 null");
                    }
                    
                    // 5秒後關閉
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            subtitleWindow.Close();
                            Console.WriteLine("字幕窗口已關閉");
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"測試字幕窗口時發生錯誤: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }
        }
    }
}
