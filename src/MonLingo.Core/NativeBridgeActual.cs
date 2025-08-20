using System;
using System.Runtime.InteropServices;

namespace MonLingo.Core
{
    /// <summary>
    /// 更新後的 Native Bridge - 基於實際DLL測試結果
    /// 只包含經過驗證存在的函數
    /// </summary>
    public static class NativeBridgeActual
    {
        private const string DllName = "MonLingo.Native.dll";

        #region 螢幕擷取模組 - 已驗證可用

        /// <summary>
        /// 啟動基於視窗控制代碼的連續擷取會話
        /// ✅ 測試結果：函數存在且可調用
        /// </summary>
        /// <param name="hwnd">目標視窗控制代碼</param>
        /// <returns>true 表示啟動成功，false 表示失敗</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool screenshot_window_loop_start(IntPtr hwnd);

        /// <summary>
        /// 從共享緩衝區讀取最新影像資料
        /// ✅ 測試結果：函數存在且可調用
        /// </summary>
        /// <returns>影像資料指標，需要進一步測試參數</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr screenshot_window_loop_read();

        /// <summary>
        /// 停止擷取會話並清理資源
        /// ✅ 測試結果：函數存在且可調用
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void screenshot_window_loop_stop();

        #endregion

        #region OCR 功能 - 需要替代實現
        
        /// <summary>
        /// 注意：以下OCR函數在實際DLL中不存在
        /// ❌ ocr_init - 函數不存在
        /// ❌ CreateOcrInitOptions - 函數不存在  
        /// ❌ RunOcrPipeline - 函數不存在
        /// ❌ CreateOcrProcessOptions - 函數不存在
        /// ❌ ocr_get_line_count - 函數不存在
        /// ❌ ocr_get_line_content - 函數不存在
        /// ❌ ocr_get_word_confidence - 函數不存在
        /// ❌ GetImageAngle - 函數不存在
        /// 
        /// 需要使用PaddleOCR的.NET包裝或其他OCR解決方案
        /// </summary>
        
        #endregion

        #region 測試和偵錯功能

        /// <summary>
        /// 測試DLL是否正確載入
        /// </summary>
        /// <returns>如果能執行此函數表示DLL載入成功</returns>
        public static bool TestDllLoaded()
        {
            try
            {
                // 呼叫一個簡單的函數來測試DLL載入
                screenshot_window_loop_stop(); // 這個函數即使在沒有啟動的情況下也應該能安全調用
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
