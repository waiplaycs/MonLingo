using System;

namespace MonLingo.Core
{
    /// <summary>
    /// 專門用於DLL函數測試的簡單程式
    /// </summary>
    public class DllTestProgram
    {
        /// <summary>
        /// DLL測試主程式入口點
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("🚀 MonLingo DLL 函數測試工具");
            Console.WriteLine("基於分析報告的 PaddleOCR 函數檢查");
            Console.WriteLine("========================================");
            
            try
            {
                // 測試 DLL 函數可用性
                Console.WriteLine("🔍 正在測試 Native DLL 函數...");
                MonLingo.Core.Service.DllFunctionTesterNew.TestAllOcrFunctions();
                Console.WriteLine();
                
                Console.WriteLine("✅ DLL 函數測試完成！");
                
                Console.WriteLine("\n📋 基於測試結果更新 NativeBridge:");
                Console.WriteLine("   1. 使用 ✓ 標記的函數來更新 P/Invoke 聲明");
                Console.WriteLine("   2. 移除 ✗ 標記的不存在函數");
                Console.WriteLine("   3. 調整 ? 標記函數的參數類型和調用約定");
                
                Console.WriteLine("\n按任意鍵退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ DLL 測試失敗: {ex.Message}");
                Console.WriteLine("\n🔧 請檢查:");
                Console.WriteLine("   1. MonLingo.Native.dll 是否在同一目錄");
                Console.WriteLine("   2. DLL 是否為正確的架構 (x64/x86)");
                Console.WriteLine("   3. 缺少的依賴 DLL");
                
                Console.WriteLine("\n按任意鍵退出...");
                Console.ReadKey();
            }
        }
    }
}
