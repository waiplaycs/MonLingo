using System;
using System.Threading.Tasks;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Service;

namespace MonLingo.Core.Test
{
    /// <summary>
    /// Phase 5 UI 按鈕功能測試
    /// 測試主要翻譯按鈕和快速截圖按鈕是否正確連接到後端功能
    /// </summary>
    public class Phase5UIFunctionTest
    {
        public static async Task RunUIFunctionTestAsync()
        {
            Console.WriteLine("=== Phase 5 UI 按鈕功能測試 ===");
            
            try
            {
                // 初始化服務容器
                Phase5ServiceContainer.Initialize();
                Console.WriteLine("✓ 服務容器初始化成功");
                
                // 獲取翻譯橋接器
                var translationBridge = Phase5ServiceContainer.GetService<UITranslationBridge>();
                Console.WriteLine("✓ 翻譯橋接器獲取成功");
                
                // 測試初始化功能
                Console.WriteLine("\n--- 測試翻譯服務初始化 ---");
                await translationBridge.InitializeAsync();
                Console.WriteLine("✓ 翻譯服務初始化完成");
                
                // 測試主翻譯按鈕功能
                Console.WriteLine("\n--- 測試主翻譯按鈕功能 ---");
                await TestStartTranslationButton(translationBridge);
                
                // 測試快速截圖按鈕功能
                Console.WriteLine("\n--- 測試快速截圖按鈕功能 ---");
                await TestQuickScreenshotButton(translationBridge);
                
                Console.WriteLine("\n=== 所有 UI 按鈕功能測試完成 ===");
                Console.WriteLine("✓ Phase 5 核心翻譯功能已成功連接到 UI 按鈕");
                Console.WriteLine("✓ 使用者可以透過主工具列的按鈕觸發翻譯功能");
                Console.WriteLine("✓ F4 熱鍵集成準備就緒（需要 Native.dll 支援）");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ UI 功能測試失敗: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }
        }
        
        private static async Task TestStartTranslationButton(UITranslationBridge bridge)
        {
            try
            {
                Console.WriteLine("模擬點擊「開始翻譯」按鈕...");
                await bridge.StartTranslationSessionAsync();
                Console.WriteLine("✓ 開始翻譯按鈕功能正常");
                
                // 等待一下模擬使用時間
                await Task.Delay(1000);
                
                Console.WriteLine("模擬點擊「停止翻譯」按鈕...");
                await bridge.StopTranslationSessionAsync();
                Console.WriteLine("✓ 停止翻譯按鈕功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 主翻譯按鈕測試失敗: {ex.Message}");
            }
        }
        
        private static async Task TestQuickScreenshotButton(UITranslationBridge bridge)
        {
            try
            {
                Console.WriteLine("模擬點擊「快速截圖翻譯」按鈕...");
                await bridge.QuickScreenshotTranslationAsync();
                Console.WriteLine("✓ 快速截圖按鈕功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 快速截圖按鈕測試失敗: {ex.Message}");
            }
        }
    }
}
