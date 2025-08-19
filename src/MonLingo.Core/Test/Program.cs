using System;
using System.Threading.Tasks;
using MonLingo.Core.Test;

namespace Phase4TestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("MonLingo Phase 4 驗證測試開始...");
            Console.WriteLine("==========================================");
            
            try
            {
                // 運行 Phase 4 驗證測試
                await Phase4ValidationTest.RunCompleteTestAsync();
                
                Console.WriteLine("\n==========================================");
                Console.WriteLine("所有測試完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 測試執行失敗: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }
            
            Console.WriteLine("\n按任意鍵退出...");
            Console.ReadKey();
        }
    }
}
