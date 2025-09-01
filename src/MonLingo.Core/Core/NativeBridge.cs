using System;
using System.Runtime.InteropServices;

namespace MonLingo.Core
{
    /// <summary>
    /// 對 MonLingo.Native.dll 的 P/Invoke 封裝
    /// 嚴格對應 PRD §2.3 與 §11.3 的 API 定義
    /// </summary>
    public static class NativeBridge
    {
        private const string DllName = "MonLingo.Native.dll";

        #region 螢幕擷取模組 (§8.1)

        /// <summary>
        /// 檢查系統是否支援 Windows Graphics Capture API
        /// </summary>
        /// <returns>true 表示支援，false 表示不支援</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool graphics_capture_is_supported();

        /// <summary>
        /// 啟動基於視窗控制代碼的連續擷取會話
        /// </summary>
        /// <param name="hwnd">目標視窗控制代碼</param>
        /// <returns>true 表示啟動成功，false 表示失敗</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool screenshot_window_loop_start(IntPtr hwnd);

        /// <summary>
        /// 從共享緩衝區讀取最新影像資料
        /// 返回影像寬度和高度，支援生產者-消費者模式
        /// </summary>
        /// <param name="buffer">影像資料緩衝區</param>
        /// <param name="size">緩衝區大小（輸入），實際資料大小（輸出）</param>
        /// <param name="width">影像寬度（輸出）</param>
        /// <param name="height">影像高度（輸出）</param>
        /// <returns>true 表示讀取成功，false 表示無新資料或失敗</returns>
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

        /// <summary>
        /// 執行單次視窗擷取
        /// </summary>
        /// <param name="hwnd">目標視窗控制代碼</param>
        /// <param name="buffer">影像資料緩衝區</param>
        /// <param name="size">緩衝區大小</param>
        /// <returns>true 表示擷取成功，false 表示失敗</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool screenshot_window_once(IntPtr hwnd, [Out] byte[] buffer, int size);

        #endregion

        #region OCR 引擎模組 (§9)

        /// <summary>
        /// 初始化 PaddleOCR 引擎和相關資源
        /// </summary>
        /// <returns>true 表示初始化成功，false 表示失敗</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_init();

        /// <summary>
        /// 初始化 PaddleOCR 引擎和相關資源（帶參數版本）
        /// </summary>
        /// <param name="fullOffline">是否使用完全離線模式</param>
        /// <param name="timeStamp">時間戳</param>
        /// <returns>true 表示初始化成功，false 表示失敗</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_init(bool fullOffline, int timeStamp);

        /// <summary>
        /// 設置 PaddleOCR 日誌級別
        /// </summary>
        /// <param name="level">日誌級別 (0=禁用, 1=錯誤, 2=警告, 3=信息, 4=調試)</param>
        /// <returns>true 表示設置成功，false 表示失敗</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_set_log_level(int level);

        /// <summary>
        /// 銷毀 OCR 引擎並清理記憶體
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ocr_destroy();

        /// <summary>
        /// 對影像執行完整的 OCR 處理管線
        /// 包含 k-means 聚類和角度校正
        /// </summary>
        /// <param name="imageData">影像資料</param>
        /// <param name="size">影像資料大小</param>
        /// <param name="width">影像寬度</param>
        /// <param name="height">影像高度</param>
        /// <returns>OCR 結果指標，需呼叫 ocr_release_result 釋放</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ocr_run_pipeline(byte[] imageData, int size, int width, int height);

        /// <summary>
        /// 獲取 OCR 結果的行數
        /// </summary>
        /// <param name="result">OCR 結果指標</param>
        /// <returns>行數</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ocr_get_line_count(IntPtr result);

        /// <summary>
        /// 獲取指定行的邊界框
        /// </summary>
        /// <param name="result">OCR 結果指標</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="x">邊界框 X 座標</param>
        /// <param name="y">邊界框 Y 座標</param>
        /// <param name="width">邊界框寬度</param>
        /// <param name="height">邊界框高度</param>
        /// <returns>true 表示成功，false 表示索引無效</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_get_line_bounding_box(
            IntPtr result, 
            int lineIndex, 
            out int x, 
            out int y, 
            out int width, 
            out int height);

        /// <summary>
        /// 獲取指定行的單詞數量
        /// </summary>
        /// <param name="result">OCR 結果指標</param>
        /// <param name="lineIndex">行索引</param>
        /// <returns>單詞數量</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ocr_get_line_word_count(IntPtr result, int lineIndex);

        /// <summary>
        /// 獲取指定行指定單詞的內容
        /// </summary>
        /// <param name="result">OCR 結果指標</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="wordIndex">單詞索引</param>
        /// <returns>單詞內容指標（原生字串）</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ocr_get_line_word(IntPtr result, int lineIndex, int wordIndex);

        /// <summary>
        /// 獲取指定行指定單詞的邊界框
        /// </summary>
        /// <param name="result">OCR 結果指標</param>
        /// <param name="lineIndex">行索引</param>
        /// <param name="wordIndex">單詞索引</param>
        /// <param name="x">邊界框 X 座標</param>
        /// <param name="y">邊界框 Y 座標</param>
        /// <param name="width">邊界框寬度</param>
        /// <param name="height">邊界框高度</param>
        /// <returns>true 表示成功，false 表示索引無效</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ocr_get_word_bounding_box(
            IntPtr result, 
            int lineIndex, 
            int wordIndex, 
            out int x, 
            out int y, 
            out int width, 
            out int height);

        /// <summary>
        /// 釋放 OCR 結果記憶體
        /// </summary>
        /// <param name="result">OCR 結果指標</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ocr_release_result(IntPtr result);

        #endregion

        #region 視窗管理模組 (§2.3.4)

        /// <summary>
        /// 獲取滑鼠下方的視窗控制代碼
        /// </summary>
        /// <returns>視窗控制代碼</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_window_under_cursor")]
        public static extern IntPtr GetWindowUnderCursor();

        #endregion

        #region 加密與安全模組 (§2.3.6)

        /// <summary>
        /// 獲取完整性檢查簽章字串
        /// 使用 IntPtr 模式，需要手動釋放原生記憶體
        /// </summary>
        /// <returns>簽章字串指標（原生字串）</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr get_sign_str();

        /// <summary>
        /// agent 編碼，用於伺服器通訊
        /// </summary>
        /// <param name="input">輸入字串指標</param>
        /// <returns>編碼結果指標（原生字串）</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr agent_encode(IntPtr input);

        /// <summary>
        /// agent 解碼，用於伺服器通訊
        /// </summary>
        /// <param name="input">輸入字串指標</param>
        /// <returns>解碼結果指標（原生字串）</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr agent_decode(IntPtr input);

        /// <summary>
        /// its 編碼，用於內部傳輸安全
        /// </summary>
        /// <param name="input">輸入字串指標</param>
        /// <returns>編碼結果指標（原生字串）</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr its_encode(IntPtr input);

        /// <summary>
        /// its 解碼，用於內部傳輸安全
        /// </summary>
        /// <param name="input">輸入字串指標</param>
        /// <returns>解碼結果指標（原生字串）</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr its_decode(IntPtr input);

        /// <summary>
        /// 釋放原生字串記憶體
        /// 與 get_sign_str、agent_encode/decode、its_encode/decode 配合使用
        /// </summary>
        /// <param name="ptr">原生字串指標</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void free_native_string(IntPtr ptr);

        #endregion

        #region 輔助方法

        /// <summary>
        /// 從原生字串指標轉換為 .NET 字串並釋放原生記憶體
        /// 用於處理 get_sign_str、agent_/its_ 系列函式的返回值
        /// </summary>
        /// <param name="nativePtr">原生字串指標</param>
        /// <returns>.NET 字串</returns>
        public static string MarshalAndFreeString(IntPtr nativePtr)
        {
            if (nativePtr == IntPtr.Zero)
                return null;

            try
            {
                return Marshal.PtrToStringAnsi(nativePtr);
            }
            finally
            {
                free_native_string(nativePtr);
            }
        }

        #endregion
    }
}
