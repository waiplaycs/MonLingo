using System;
using System.Runtime.InteropServices;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 改進的 DLL 函數測試工具 - 基於分析報告的實際函數
    /// </summary>
    public static class DllFunctionTesterNew
    {
        private const string DllName = "MonLingo.Native.dll";
        
        /// <summary>
        /// 測試所有 OCR 相關函數
        /// </summary>
        public static void TestAllOcrFunctions()
        {
            Console.WriteLine("=== 改進的 DLL 函數測試開始 ===");
            
            // 測試初始化函數
            TestFunction("ocr_init", () => ocr_init(IntPtr.Zero));
            TestFunction("CreateOcrInitOptions", () => CreateOcrInitOptions());
            
            // 測試處理函數
            TestFunction("RunOcrPipeline", () => RunOcrPipeline(IntPtr.Zero, IntPtr.Zero));
            TestFunction("CreateOcrProcessOptions", () => CreateOcrProcessOptions());
            
            // 測試結果讀取函數
            TestFunction("ocr_get_line_count", () => ocr_get_line_count(IntPtr.Zero));
            TestFunction("ocr_get_line_content", () => ocr_get_line_content(IntPtr.Zero, 0));
            TestFunction("ocr_get_word_confidence", () => ocr_get_word_confidence(IntPtr.Zero, 0, 0));
            
            // 測試圖像角度檢測 (分析報告中提到)
            TestFunction("GetImageAngle", () => GetImageAngle(IntPtr.Zero));
            
            // 測試畫面擷取功能
            TestFunction("screenshot_window_loop_start", () => screenshot_window_loop_start(IntPtr.Zero));
            TestFunction("screenshot_window_loop_read", () => screenshot_window_loop_read());
            TestFunction("screenshot_window_loop_stop", () => screenshot_window_loop_stop());
            
            Console.WriteLine("=== 改進的 DLL 函數測試結束 ===");
        }
        
        private static void TestFunction(string functionName, Action test)
        {
            try
            {
                test();
                Console.WriteLine($"✓ {functionName}() - 函數存在且可調用");
            }
            catch (EntryPointNotFoundException)
            {
                Console.WriteLine($"✗ {functionName}() - 函數不存在");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? {functionName}() - 函數存在但調用失敗: {ex.Message}");
            }
        }

        // OCR 初始化函數
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ocr_init(IntPtr options);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateOcrInitOptions();

        // OCR 處理函數
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr RunOcrPipeline(IntPtr image, IntPtr options);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateOcrProcessOptions();

        // OCR 結果讀取函數
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ocr_get_line_count(IntPtr result);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ocr_get_line_content(IntPtr result, int lineIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern float ocr_get_word_confidence(IntPtr result, int lineIndex, int wordIndex);

        // 圖像處理函數
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern float GetImageAngle(IntPtr image);

        // 畫面擷取函數
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool screenshot_window_loop_start(IntPtr hwnd);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr screenshot_window_loop_read();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void screenshot_window_loop_stop();
    }
}
