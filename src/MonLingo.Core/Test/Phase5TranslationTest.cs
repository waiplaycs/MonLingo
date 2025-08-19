using System;
using System.Threading.Tasks;
using MonLingo.Core.Infrastructure;
using MonLingo.Core.Service;

namespace MonLingo.Core.Test
{
    /// <summary>
    /// Phase 5 翻譯功能測試 - 基於 Gaminik 深度分析的完整實現
    /// </summary>
    public class Phase5TranslationTest
    {
        public Phase5TranslationTest()
        {
            // 初始化靜態服務容器
            Phase5ServiceContainer.Initialize();
        }

        /// <summary>
        /// 運行 Phase 5 所有測試
        /// </summary>
        public async Task RunPhase5TestsAsync()
        {
            Console.WriteLine("🧪 Phase 5 翻譯功能測試開始...");
            Console.WriteLine();

            try
            {
                // 初始化服務容器
                await InitializeServicesAsync();

                // 測試 1: 核心翻譯管線
                await TestTranslationPipelineAsync();

                // 測試 2: UI 翻譯橋接
                await TestUITranslationBridgeAsync();

                // 測試 3: 事件系統
                await TestEventSystemAsync();

                Console.WriteLine("✅ 所有 Phase 5 測試通過！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Phase 5 測試失敗: {ex.Message}");
                throw;
            }
        }

        private Task InitializeServicesAsync()
        {
            Console.WriteLine("🔧 初始化 Phase 5 服務...");
            
            // 服務容器已經在構造函數中初始化
            
            Console.WriteLine("✅ 服務初始化完成");
            return Task.CompletedTask;
        }

        private async Task TestTranslationPipelineAsync()
        {
            Console.WriteLine("🔄 測試翻譯管線...");

            var pipelineManager = Phase5ServiceContainer.GetService<ITranslationPipelineManager>();
            if (pipelineManager == null)
            {
                throw new InvalidOperationException("翻譯管線未正確註冊");
            }

            // 設置事件處理器
            pipelineManager.TranslationCompleted += (sender, result) =>
            {
                Console.WriteLine($"📄 翻譯完成: {result.SourceText} -> {result.TranslatedText}");
            };

            pipelineManager.ErrorOccurred += (sender, error) =>
            {
                Console.WriteLine($"⚠️ 翻譯錯誤: {error}");
            };

            // 初始化管線
            await pipelineManager.InitializeAsync();

            // 模擬處理單一幀
            await pipelineManager.ProcessSingleFrameAsync();

            // 等待短暫時間讓事件處理
            await Task.Delay(1000);

            Console.WriteLine("✅ 翻譯管線測試完成");
        }

        private async Task TestUITranslationBridgeAsync()
        {
            Console.WriteLine("🌉 測試 UI 翻譯橋接...");

            var bridge = Phase5ServiceContainer.GetService<UITranslationBridge>();
            if (bridge == null)
            {
                throw new InvalidOperationException("UI 翻譯橋接未正確註冊");
            }

            // 測試開始翻譯會話
            await bridge.StartTranslationSessionAsync();
            Console.WriteLine("📊 翻譯會話已開始");

            // 測試快速截圖翻譯
            await bridge.QuickScreenshotTranslationAsync();
            Console.WriteLine("📸 快速截圖翻譯已觸發");

            Console.WriteLine("✅ UI 翻譯橋接測試完成");
        }

        private async Task TestEventSystemAsync()
        {
            Console.WriteLine("📡 測試事件系統...");

            var eventAggregator = Phase5ServiceContainer.GetService<IEventAggregator>();
            if (eventAggregator == null)
            {
                throw new InvalidOperationException("事件聚合器未正確註冊");
            }

            bool eventReceived = false;

            // 訂閱測試事件
            eventAggregator.Subscribe<string>(message =>
            {
                Console.WriteLine($"📩 收到事件: {message}");
                eventReceived = true;
                return Task.CompletedTask;
            });

            // 發布測試事件
            await eventAggregator.PublishAsync("Phase 5 事件系統測試");

            // 等待事件處理
            await Task.Delay(500);

            if (!eventReceived)
            {
                throw new InvalidOperationException("事件系統未正常工作");
            }

            Console.WriteLine("✅ 事件系統測試完成");
        }

        public void Dispose()
        {
            // Phase5ServiceContainer 是靜態的，不需要 Dispose
        }
    }
}
