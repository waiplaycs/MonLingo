using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MonLingo.Core.Interop
{
    /// <summary>
    /// NativeBridge - Native.dll 互操作介面
    /// 對應 Gaminik.Interop.NativeMethods 的功能
    /// 這是 MonLingo.Core.dll 與底層原生功能溝通的唯一途徑
    /// </summary>
    public static class NativeBridge
    {
        private const string DllName = "MonLingo.Native.dll";

        #region 螢幕擷取 API

        /// <summary>
        /// 檢查系統是否支援 Graphics Capture API
        /// </summary>
        /// <returns>是否支援</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool graphics_capture_is_supported();

        /// <summary>
        /// 執行單次視窗擷取
        /// </summary>
        /// <param name="hwnd">視窗控制代碼</param>
        /// <param name="buffer">影像資料緩衝區</param>
        /// <param name="size">緩衝區大小</param>
        /// <returns>是否成功</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool screenshot_window_once(IntPtr hwnd, byte[] buffer, ref int size);

        /// <summary>
        /// 啟動基於視窗控制代碼的連續擷取會話
        /// </summary>
        /// <param name="hwnd">視窗控制代碼</param>
        /// <returns>是否成功啟動</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool screenshot_window_loop_start(IntPtr hwnd);

        /// <summary>
        /// 從共享緩衝區讀取最新影像資料
        /// </summary>
        /// <param name="buffer">影像資料緩衝區</param>
        /// <param name="size">影像資料大小</param>
        /// <param name="width">影像寬度</param>
        /// <param name="height">影像高度</param>
        /// <returns>是否成功讀取</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool screenshot_window_loop_read(
            [Out] byte[] buffer, 
            ref int size, 
            ref int width, 
            ref int height);

        /// <summary>
        /// 停止擷取會話並清理資源
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void screenshot_window_close();

        #endregion

        #region OCR API

        /// <summary>
        /// 初始化 OCR 引擎和相關資源
        /// </summary>
        /// <returns>是否成功初始化</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_init();

        /// <summary>
        /// 銷毀 OCR 引擎並清理記憶體
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ocr_destroy();

        /// <summary>
        /// 對影像執行完整的 OCR 處理管線
        /// </summary>
        /// <param name="imageData">影像資料</param>
        /// <param name="size">影像資料大小</param>
        /// <param name="width">影像寬度</param>
        /// <param name="height">影像高度</param>
        /// <returns>OCR 結果指標</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ocr_run_pipeline(
            byte[] imageData, 
            int size, 
            int width, 
            int height);

        /// <summary>
        /// 獲取 OCR 結果的行數
        /// </summary>
        /// <param name="resultPtr">OCR 結果指標</param>
        /// <returns>行數</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ocr_get_line_count(IntPtr resultPtr);

        /// <summary>
        /// 獲取指定行的 OCR 結果內容
        /// </summary>
        /// <param name="resultPtr">OCR 結果指標</param>
        /// <param name="index">行索引</param>
        /// <param name="content">輸出內容緩衝區</param>
        /// <param name="capacity">緩衝區容量</param>
        /// <returns>是否成功</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_get_line_content(
            IntPtr resultPtr, 
            int index, 
            [Out] StringBuilder content, 
            int capacity);

        /// <summary>
        /// 獲取指定行中單詞的數量
        /// </summary>
        /// <param name="resultPtr">OCR 結果指標</param>
        /// <param name="lineIndex">行索引</param>
        /// <returns>單詞數量</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ocr_get_word_count(IntPtr resultPtr, int lineIndex);

        /// <summary>
        /// 獲取單詞的文字內容
        /// </summary>
        /// <param name="resultPtr">OCR 結果指標</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="wordIndex">單詞索引</param>
        /// <param name="content">輸出內容緩衝區</param>
        /// <param name="capacity">緩衝區容量</param>
        /// <returns>是否成功</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_get_word_content(
            IntPtr resultPtr, 
            int lineIndex, 
            int wordIndex,
            [Out] StringBuilder content, 
            int capacity);

        /// <summary>
        /// 獲取單詞識別的信心度分數
        /// </summary>
        /// <param name="resultPtr">OCR 結果指標</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="wordIndex">單詞索引</param>
        /// <returns>信心度 (0.0-1.0)</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ocr_get_word_confidence(IntPtr resultPtr, int lineIndex, int wordIndex);

        /// <summary>
        /// 釋放 OCR 結果記憶體
        /// </summary>
        /// <param name="resultPtr">OCR 結果指標</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ocr_release_result(IntPtr resultPtr);

        #endregion

        #region 視窗管理 API

        /// <summary>
        /// 獲取滑鼠下方的視窗控制代碼
        /// </summary>
        /// <returns>視窗控制代碼</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr get_window_under_cursor();

        /// <summary>
        /// 移動視窗到指定位置
        /// </summary>
        /// <param name="hwnd">視窗控制代碼</param>
        /// <param name="x">X 座標</param>
        /// <param name="y">Y 座標</param>
        /// <returns>是否成功</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool move_window_chrome(IntPtr hwnd, int x, int y);

        /// <summary>
        /// 調整視窗大小
        /// </summary>
        /// <param name="hwnd">視窗控制代碼</param>
        /// <param name="width">寬度</param>
        /// <param name="height">高度</param>
        /// <returns>是否成功</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool resize_window_chrome(IntPtr hwnd, int width, int height);

        /// <summary>
        /// 獲取視窗標題
        /// </summary>
        /// <param name="hwnd">視窗控制代碼</param>
        /// <param name="title">輸出標題緩衝區</param>
        /// <param name="capacity">緩衝區容量</param>
        /// <returns>是否成功</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool get_window_title(IntPtr hwnd, [Out] StringBuilder title, int capacity);

        #endregion

        #region 全域熱鍵 API

        /// <summary>
        /// 註冊全域熱鍵
        /// </summary>
        /// <param name="modifiers">修飾鍵</param>
        /// <param name="key">按鍵</param>
        /// <returns>是否成功註冊</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool register_global_hotkey(int modifiers, int key);

        /// <summary>
        /// 取消註冊全域熱鍵
        /// </summary>
        /// <param name="modifiers">修飾鍵</param>
        /// <param name="key">按鍵</param>
        /// <returns>是否成功取消註冊</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool unregister_global_hotkey(int modifiers, int key);

        /// <summary>
        /// 取消註冊所有熱鍵
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void unregister_all_hotkeys();

        #endregion

        #region 音訊處理 API

        /// <summary>
        /// 開始音訊擷取
        /// </summary>
        /// <param name="deviceId">音訊裝置ID</param>
        /// <returns>是否成功開始</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool audio_start_capture(int deviceId);

        /// <summary>
        /// 停止音訊擷取
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void audio_stop_capture();

        /// <summary>
        /// 讀取音訊資料
        /// </summary>
        /// <param name="buffer">音訊資料緩衝區</param>
        /// <param name="size">緩衝區大小</param>
        /// <returns>實際讀取的資料大小</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int audio_read_data(byte[] buffer, int size);

        #endregion

        #region 實用工具

        /// <summary>
        /// 獲取最後一個錯誤訊息
        /// </summary>
        /// <param name="errorMessage">輸出錯誤訊息緩衝區</param>
        /// <param name="capacity">緩衝區容量</param>
        /// <returns>是否有錯誤</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool get_last_error(
            [Out] StringBuilder errorMessage, 
            int capacity);

        /// <summary>
        /// 獲取 Native.dll 版本資訊
        /// </summary>
        /// <param name="version">輸出版本緩衝區</param>
        /// <param name="capacity">緩衝區容量</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void get_version_info(
            [Out] StringBuilder version, 
            int capacity);

        #endregion

        #region 回呼函式

        /// <summary>
        /// Native 層回呼委派
        /// </summary>
        /// <param name="messageType">訊息類型</param>
        /// <param name="value">值</param>
        public delegate void NativeCallbackDelegate(int messageType, int value);

        /// <summary>
        /// 設定 Native 層回呼函式
        /// </summary>
        /// <param name="callback">回呼函式</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_callback(NativeCallbackDelegate callback);

        #endregion

        #region 高階封裝方法

        /// <summary>
        /// 安全地獲取 OCR 結果文字
        /// </summary>
        /// <param name="resultPtr">OCR 結果指標</param>
        /// <returns>識別的文字</returns>
        public static string GetOcrResultText(IntPtr resultPtr)
        {
            if (resultPtr == IntPtr.Zero)
                return string.Empty;

            try
            {
                int lineCount = ocr_get_line_count(resultPtr);
                var result = new StringBuilder();

                for (int i = 0; i < lineCount; i++)
                {
                    var lineContent = new StringBuilder(1024);
                    if (ocr_get_line_content(resultPtr, i, lineContent, 1024))
                    {
                        if (result.Length > 0)
                            result.AppendLine();
                        result.Append(lineContent.ToString());
                    }
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetOcrResultText 錯誤: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 安全地檢查 Native.dll 是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        public static bool IsNativeDllAvailable()
        {
            try
            {
                var version = new StringBuilder(256);
                get_version_info(version, 256);
                return !string.IsNullOrEmpty(version.ToString());
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsNativeDllAvailable 錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 獲取最後錯誤訊息（安全版本）
        /// </summary>
        /// <returns>錯誤訊息</returns>
        public static string GetLastErrorMessage()
        {
            try
            {
                var errorMessage = new StringBuilder(1024);
                if (get_last_error(errorMessage, 1024))
                {
                    return errorMessage.ToString();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                return $"無法獲取錯誤訊息: {ex.Message}";
            }
        }

        #endregion
    }

    #region 列舉和結構

    /// <summary>
    /// Native 層訊息類型
    /// </summary>
    public enum NativeMessageType
    {
        HotKeyPressed = 1,
        CaptureCompleted = 2,
        OcrCompleted = 3,
        AudioDataAvailable = 4,
        Error = 99
    }

    /// <summary>
    /// 熱鍵修飾符
    /// </summary>
    [Flags]
    public enum HotKeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    #endregion
}
