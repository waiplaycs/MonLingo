using System;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 螢幕擷取服務實現
    /// 基於 PRD §2.3.1 的完整螢幕擷取系統
    /// </summary>
    public class ScreenCaptureService : IScreenCaptureService
    {
        private static bool _nativeInitialized = false;
        private static readonly object _initLock = new object();
        
        private bool _isCapturing = false;
        private IntPtr _targetWindow = IntPtr.Zero;
        
        public bool IsCapturing => _isCapturing;
        
        /// <summary>
        /// 確保 Native.dll 已初始化
        /// </summary>
        private static void EnsureNativeInitialized()
        {
            if (_nativeInitialized) return;
            
            lock (_initLock)
            {
                if (_nativeInitialized) return;
                
                Console.WriteLine("[DEBUG] Initializing Native.dll...");
                int result = NativeBridge.native_initialize();
                Console.WriteLine($"[DEBUG] native_initialize() returned: {result}");
                
                if (result == 0) // 0 = Success based on ResultCode
                {
                    _nativeInitialized = true;
                    Console.WriteLine("[DEBUG] Native.dll initialized successfully");
                }
                else
                {
                    throw new InvalidOperationException($"Failed to initialize Native.dll: error code {result}");
                }
            }
        }
        
        /// <summary>
        /// 開始擷取指定視窗（PRD §2.3.1 完整實現）
        /// </summary>
        public bool StartCapture(IntPtr hwnd)
        {
            if (_isCapturing) return false;
            
            try
            {
                Console.WriteLine($"[DEBUG] StartCapture called with hwnd={hwnd}");
                
                // 確保 Native.dll 已初始化
                EnsureNativeInitialized();
                
                // 檢查 Graphics Capture 支援
                bool isSupported = NativeBridge.graphics_capture_is_supported();
                Console.WriteLine($"[DEBUG] Graphics Capture supported: {isSupported}");
                
                if (!isSupported)
                {
                    throw new NotSupportedException("Graphics Capture API not supported");
                }
                
                // 啟動擷取迴圈 (每100ms捕獲一次)
                int result = NativeBridge.screenshot_window_loop_start(hwnd, 100);
                Console.WriteLine($"[DEBUG] screenshot_window_loop_start returned: {result}");
                
                if (result == 0) // 0 = Success
                {
                    _isCapturing = true;
                    _targetWindow = hwnd;
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start capture: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 讀取最新擷取的幀（PRD §2.3.1）
        /// </summary>
        public CaptureFrame ReadFrame()
        {
            if (!_isCapturing) return null;
            
            try
            {
                Console.WriteLine($"[DEBUG] ScreenCaptureService.ReadFrame: _isCapturing={_isCapturing}");
                var buffer = new byte[1920 * 1080 * 4]; // 4K 緩衝區
                int size = buffer.Length;
                int width = 0, height = 0;
                
                Console.WriteLine($"[DEBUG] Calling screenshot_window_loop_read with buffer.Length={buffer.Length}");
                int result = NativeBridge.screenshot_window_loop_read(buffer, ref size, ref width, ref height);
                Console.WriteLine($"[DEBUG] screenshot_window_loop_read returned: result={result}, size={size}, width={width}, height={height}");
                
                if (result == 0) // 0 = Success
                {
                    return new CaptureFrame
                    {
                        ImageData = buffer,
                        Size = size,
                        Width = width,
                        Height = height,
                        Timestamp = DateTime.Now
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read frame: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 停止擷取
        /// </summary>
        public void StopCapture()
        {
            if (_isCapturing)
            {
                try
                {
                    NativeBridge.screenshot_window_close();
                    _isCapturing = false;
                    _targetWindow = IntPtr.Zero;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to stop capture: {ex.Message}", ex);
                }
            }
        }
        
        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            StopCapture();
        }
        
        /// <summary>
        /// 清理 Native.dll 資源（靜態方法，應在應用程式結束時調用）
        /// </summary>
        public static void CleanupNative()
        {
            lock (_initLock)
            {
                if (_nativeInitialized)
                {
                    Console.WriteLine("[DEBUG] Cleaning up Native.dll...");
                    NativeBridge.native_cleanup();
                    _nativeInitialized = false;
                    Console.WriteLine("[DEBUG] Native.dll cleanup completed");
                }
            }
        }
    }
    
    /// <summary>
    /// Native Bridge 靜態類別
    /// 基於 PRD §2.3.1 NativeMethods 完整實現
    /// </summary>
    public static class NativeBridge
    {
        private const string DllName = "MonLingo.Native.dll";
        
        // === 螢幕擷取管線 (基於實際匯出函式) ===
        // === Native DLL 初始化 (必須先呼叫) ===
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern int native_initialize();
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void native_cleanup();
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern bool graphics_capture_is_supported();
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern bool screenshot_window_once(IntPtr hwnd, byte[] buffer, ref int size);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern int screenshot_window_loop_start(IntPtr hwnd, int intervalMs);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern int screenshot_window_loop_read(byte[] buffer, ref int size, ref int width, ref int height);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void screenshot_window_close();
        
        // === OCR 處理 (完整 API) ===
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern bool ocr_init(bool fullOffline, int timeStamp);

        /// <summary>
        /// 設置 PaddleOCR 日誌級別
        /// </summary>
        /// <param name="level">日誌級別 (0=禁用, 1=錯誤, 2=警告, 3=信息, 4=調試)</param>
        /// <returns>true 表示設置成功，false 表示失敗</returns>
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern bool ocr_set_log_level(int level);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void ocr_destroy();
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern IntPtr ocr_run_pipeline(byte[] imageData, int size, int width, int height);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern IntPtr ocr_get_line(IntPtr resultPtr, int lineIndex);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern bool ocr_get_word_content(IntPtr wordPtr, System.Text.StringBuilder content, int capacity);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern float ocr_get_word_confidence(IntPtr wordPtr);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern int ocr_get_line_count(IntPtr resultPtr);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern bool ocr_get_line_content(IntPtr resultPtr, int index, System.Text.StringBuilder content, int capacity);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        public static extern bool ocr_get_line_bounding_box(IntPtr resultPtr, int index, [System.Runtime.InteropServices.Out] int[] quad8);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern int ocr_get_line_word_count(IntPtr linePtr);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern IntPtr ocr_get_line_word(IntPtr linePtr, int wordIndex);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        public static extern bool ocr_get_word_bounding_box(IntPtr wordPtr, [System.Runtime.InteropServices.Out] int[] quad8);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void ocr_release_result(IntPtr resultPtr);
        
        // === 全域熱鍵管理 ===
        public delegate void NativeCallbackDelegate(int messageType, int value);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void SetCallback(NativeCallbackDelegate callback);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern bool RegisterGlobalHotKey(int modifiers, int key);
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public static extern void UnhookAll();
        
        [System.Runtime.InteropServices.DllImport(DllName, CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, EntryPoint = "get_window_under_cursor")]
        public static extern IntPtr GetWindowUnderCursor();
    }
}
