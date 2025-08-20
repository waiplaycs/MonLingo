using System;
using System.Runtime.InteropServices;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// DLL 函數測試工具 - 用於檢查哪些函數實際存在
    /// </summary>
    public static class DllFunctionTester
    {
        private const string DllName = "MonLingo.Native.dll";
        
        // 測試不同的 OCR 初始化函數
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ocr_init")]
        public static extern bool TestOcrInit();
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateOcrInitOptions")]
        public static extern IntPtr TestCreateOcrInitOptions();
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "RunOcrPipeline")]
        public static extern IntPtr TestRunOcrPipeline(IntPtr options, byte[] imageData, int size);
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ocr_run_pipeline")]
        public static extern IntPtr TestOcrRunPipeline(byte[] imageData, int size, int width, int height);
        
        /// <summary>
        /// 測試 DLL 函數可用性
        /// </summary>
        public static void TestDllFunctions()
        {
            Console.WriteLine("=== DLL 函數測試開始 ===");
            
            // 測試 ocr_init
            try
            {
                TestOcrInit();
                Console.WriteLine("✓ ocr_init() - 函數存在");
            }
            catch (EntryPointNotFoundException)
            {
                Console.WriteLine("✗ ocr_init() - 函數不存在");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? ocr_init() - 函數存在但調用失敗: {ex.Message}");
            }
            
            // 測試 CreateOcrInitOptions
            try
            {
                TestCreateOcrInitOptions();
                Console.WriteLine("✓ CreateOcrInitOptions() - 函數存在");
            }
            catch (EntryPointNotFoundException)
            {
                Console.WriteLine("✗ CreateOcrInitOptions() - 函數不存在");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? CreateOcrInitOptions() - 函數存在但調用失敗: {ex.Message}");
            }
            
            // 測試 RunOcrPipeline
            try
            {
                TestRunOcrPipeline(IntPtr.Zero, new byte[100], 100);
                Console.WriteLine("✓ RunOcrPipeline() - 函數存在");
            }
            catch (EntryPointNotFoundException)
            {
                Console.WriteLine("✗ RunOcrPipeline() - 函數不存在");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? RunOcrPipeline() - 函數存在但調用失敗: {ex.Message}");
            }
            
            // 測試 ocr_run_pipeline
            try
            {
                TestOcrRunPipeline(new byte[100], 100, 640, 480);
                Console.WriteLine("✓ ocr_run_pipeline() - 函數存在");
            }
            catch (EntryPointNotFoundException)
            {
                Console.WriteLine("✗ ocr_run_pipeline() - 函數不存在");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? ocr_run_pipeline() - 函數存在但調用失敗: {ex.Message}");
            }
            
            Console.WriteLine("=== DLL 函數測試結束 ===");
        }
    }
}
