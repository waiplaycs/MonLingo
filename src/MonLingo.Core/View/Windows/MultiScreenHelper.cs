using System;
using System.Drawing;
using System.Windows.Forms;
using NLog;

namespace MonLingo.Core.View.Windows
{
    /// <summary>
    /// 多螢幕支援工具類
    /// 用於檢測OCR操作的目標螢幕並提供螢幕資訊
    /// </summary>
    public static class MultiScreenHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 獲取包含指定區域的螢幕
        /// </summary>
        /// <param name="region">目標區域</param>
        /// <returns>包含該區域的螢幕工作區域</returns>
        public static Rectangle GetScreenContainingRegion(Rectangle region)
        {
            try
            {
                var regionCenter = new Point(
                    region.X + region.Width / 2,
                    region.Y + region.Height / 2
                );

                Logger.Debug($"🖥️ 檢測區域中心點：({regionCenter.X}, {regionCenter.Y})");

                // 獲取包含該點的螢幕
                var targetScreen = Screen.FromPoint(regionCenter);
                var screenBounds = targetScreen.WorkingArea;

                Logger.Info($"🎯 檢測到目標螢幕：{targetScreen.DeviceName} 工作區域：({screenBounds.X},{screenBounds.Y},{screenBounds.Width},{screenBounds.Height})");

                return screenBounds;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "檢測目標螢幕時發生錯誤，使用主螢幕");
                return Screen.PrimaryScreen.WorkingArea;
            }
        }

        /// <summary>
        /// 獲取包含指定點的螢幕
        /// </summary>
        /// <param name="point">目標點</param>
        /// <returns>包含該點的螢幕工作區域</returns>
        public static Rectangle GetScreenContainingPoint(Point point)
        {
            try
            {
                Logger.Debug($"🖥️ 檢測點位置：({point.X}, {point.Y})");

                var targetScreen = Screen.FromPoint(point);
                var screenBounds = targetScreen.WorkingArea;

                Logger.Info($"🎯 檢測到目標螢幕：{targetScreen.DeviceName} 工作區域：({screenBounds.X},{screenBounds.Y},{screenBounds.Width},{screenBounds.Height})");

                return screenBounds;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "檢測目標螢幕時發生錯誤，使用主螢幕");
                return Screen.PrimaryScreen.WorkingArea;
            }
        }

        /// <summary>
        /// 獲取所有螢幕的資訊
        /// </summary>
        /// <returns>所有螢幕資訊的字符串</returns>
        public static string GetAllScreensInfo()
        {
            try
            {
                var info = "🖥️ 系統螢幕資訊：\n";
                var screens = Screen.AllScreens;
                
                for (int i = 0; i < screens.Length; i++)
                {
                    var screen = screens[i];
                    info += $"   螢幕 {i + 1}: {screen.DeviceName}\n";
                    info += $"      邊界：({screen.Bounds.X},{screen.Bounds.Y},{screen.Bounds.Width},{screen.Bounds.Height})\n";
                    info += $"      工作區：({screen.WorkingArea.X},{screen.WorkingArea.Y},{screen.WorkingArea.Width},{screen.WorkingArea.Height})\n";
                    info += $"      主螢幕：{(screen.Primary ? "是" : "否")}\n\n";
                }

                Logger.Info(info);
                return info;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "獲取螢幕資訊時發生錯誤");
                return "無法獲取螢幕資訊";
            }
        }

        /// <summary>
        /// 檢查是否為多螢幕環境
        /// </summary>
        /// <returns>是否有多個螢幕</returns>
        public static bool IsMultiScreenEnvironment()
        {
            try
            {
                var screenCount = Screen.AllScreens.Length;
                Logger.Debug($"🖥️ 檢測到 {screenCount} 個螢幕");
                return screenCount > 1;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "檢測多螢幕環境時發生錯誤");
                return false;
            }
        }
    }
}
