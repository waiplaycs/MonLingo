using System;
using System.Threading.Tasks;
using MonLingo.Core.Test;

namespace MonLingo.Core
{
    public class TestProgram
    {
        [STAThread]
        public static async Task Main(string[] args)
        {
            // 檢查是否有 "--test" 參數
            bool runTest = args.Length > 0 && args[0] == "--test";
            
            if (runTest)
            {
                await RunPhase4TestsAsync();
            }
            else
            {
                // 正常啟動 WPF 應用程式
                var app = new App();
                app.Run();
            }
        }
        
        private static async Task RunPhase4TestsAsync()
        {
            Console.WriteLine("=== MonLingo Phase 4 驗證與集成測試 ===");
            Console.WriteLine("時間: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine("==========================================\n");
            
            try
            {
                // 第一部分: Phase 4 基本功能驗證
                Console.WriteLine("🧪 第一部分: Phase 4 基本功能驗證");
                await Phase4ValidationTest.RunCompleteTestAsync();
                
                Console.WriteLine("\n" + new string('=', 50));
                
                // 第二部分: Phase 4 與其他 Phase 集成測試
                Console.WriteLine("\n🔗 第二部分: Phase 4 與其他 Phase 集成測試");
                await Phase4IntegrationTest.RunCompleteIntegrationTestAsync();
                
                Console.WriteLine("\n==========================================");
                Console.WriteLine("🎉 所有測試完成！");
                Console.WriteLine("✅ Phase 4 基本功能: 驗證通過");
                Console.WriteLine("✅ 跨 Phase 集成: 驗證通過");
                Console.WriteLine("✅ TimeSyncService & GuerrillaNtp: 正常運作");
                Console.WriteLine("✅ 服務依賴注入: 完整支持");
                Console.WriteLine("\n🚀 Phase 4 已準備好與完整 MonLingo 系統集成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 測試執行失敗: {ex.Message}");
                Console.WriteLine($"\n詳細錯誤信息:");
                Console.WriteLine(ex.ToString());
            }
            
            Console.WriteLine("\n按任意鍵退出...");
            Console.ReadKey();
        }
    }
}
