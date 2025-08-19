using System;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Service;

namespace MonLingo.Core.Test
{
    /// <summary>
    /// 調試服務註冊問題的測試程式
    /// </summary>
    public class ServiceRegistrationDebugger
    {
        public static void DebugServiceRegistration()
        {
            try
            {
                Console.WriteLine("=== 開始調試服務註冊 ===");
                
                // 初始化 Phase5ServiceContainer
                Phase5ServiceContainer.Initialize();
                Console.WriteLine("✓ Phase5ServiceContainer 初始化成功");
                
                // 測試每個服務的註冊
                TestService<IScreenCaptureService>("IScreenCaptureService");
                TestService<IOcrService>("IOcrService");
                TestService<ITranslateService>("ITranslateService");
                TestService<MonLingo.Core.Service.INotificationService>("INotificationService");
                TestService<IConfigService>("IConfigService");
                TestService<ITranslationPipelineManager>("ITranslationPipelineManager");
                TestService<UITranslationBridge>("UITranslationBridge");
                
                Console.WriteLine("=== 服務註冊調試完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 服務註冊調試失敗: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }
        }
        
        private static void TestService<T>(string serviceName)
        {
            try
            {
                var service = Phase5ServiceContainer.GetService<T>();
                Console.WriteLine($"✓ {serviceName}: {service?.GetType().Name ?? "null"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {serviceName}: {ex.Message}");
            }
        }
    }
}
