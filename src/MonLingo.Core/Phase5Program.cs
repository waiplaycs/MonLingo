using System;
using System.Threading.Tasks;
using MonLingo.Core.Test;

namespace MonLingo.Core
{
    /// <summary>
    /// Phase 5 核心翻譯功能啟動程式
    /// </summary>
    public class Phase5Program
    {
        /// <summary>
        /// Phase 5 主程式入口點
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 MonLingo Phase 5 - 核心翻譯功能啟動");
            Console.WriteLine("基於 Gaminik 深度分析的完整實現");
            Console.WriteLine("========================================");
            
            Test.Phase5TranslationTest test = null;
            
            try
            {
                // 創建並執行 Phase 5 測試
                test = new Test.Phase5TranslationTest();
                await test.RunPhase5TestsAsync();
                
                Console.WriteLine("\n🎯 Phase 5 核心功能準備就緒！");
                Console.WriteLine("📋 下一步需要完成:");
                Console.WriteLine("   1. 實現 Native.dll 的 C++ 部分");
                Console.WriteLine("   2. 整合 OCR 引擎 (PaddleOCR)");
                Console.WriteLine("   3. 完善翻譯引擎 API 金鑰配置");
                Console.WriteLine("   4. 實現 UI 顯示層整合");
                
                Console.WriteLine("\n按任意鍵退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Phase 5 啟動失敗: {ex.Message}");
                Console.WriteLine("\n🔧 可能的解決方案:");
                Console.WriteLine("   1. 確認 Native.dll 在輸出目錄中");
                Console.WriteLine("   2. 檢查網路連線 (翻譯服務)");
                Console.WriteLine("   3. 確認 Windows 支援 Graphics Capture API");
                
                Console.WriteLine("\n按任意鍵退出...");
                Console.ReadKey();
            }
            finally
            {
                // 清理資源
                test?.Dispose();
            }
        }
    }
}
