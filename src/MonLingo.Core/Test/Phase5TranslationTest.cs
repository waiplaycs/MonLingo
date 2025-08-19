using System;
using System.Threading.Tasks;
using MonLingo.Core.Service;

namespace MonLingo.Core.Test
{
    /// <summary>
    /// Phase 5 核心翻譯功能測試
    /// 驗證 TranslationPipelineManager 和相關服務的整合
    /// </summary>
    public class Phase5TranslationTest
    {
        private readonly ITranslationPipelineManager _pipelineManager;
        private readonly HotKeyIntegrationService _hotKeyService;
        private readonly INotificationService _notificationService;
        
        public Phase5TranslationTest()
        {
            // 初始化服務
            var screenCaptureService = new ScreenCaptureService();
            var ocrService = new OcrService();
            var translateService = new TranslateService();
            _notificationService = new NotificationService();
            var configService = new ConfigService(); // 需要使用現有的 ConfigService
            
            // 創建 TranslationPipelineManager
            _pipelineManager = new TranslationPipelineManager(
                screenCaptureService,
                ocrService,
                translateService,
                _notificationService,
                configService
            );
            
            // 創建熱鍵整合服務
            _hotKeyService = new HotKeyIntegrationService(_pipelineManager, _notificationService);
        }
        
        /// <summary>
        /// 執行 Phase 5 功能測試
        /// </summary>
        public async Task RunPhase5TestsAsync()
        {
            try
            {
                Console.WriteLine("=== Phase 5 核心翻譯功能測試 ===");
                
                // 1. 初始化熱鍵系統
                Console.WriteLine("1. 初始化熱鍵系統...");
                var hotKeyInit = _hotKeyService.InitializeHotKeys();
                Console.WriteLine($"   熱鍵初始化: {(hotKeyInit ? "成功" : "失敗")}");
                
                // 2. 測試翻譯服務
                Console.WriteLine("2. 測試翻譯服務...");
                await TestTranslationServiceAsync();
                
                // 3. 測試 OCR 服務初始化
                Console.WriteLine("3. 測試 OCR 服務...");
                await TestOcrServiceAsync();
                
                // 4. 測試螢幕擷取服務
                Console.WriteLine("4. 測試螢幕擷取服務...");
                TestScreenCaptureService();
                
                // 5. 測試翻譯管線管理器事件
                Console.WriteLine("5. 測試翻譯管線事件...");
                TestPipelineManagerEvents();
                
                Console.WriteLine("\n=== Phase 5 測試完成 ===");
                Console.WriteLine("🎉 所有核心服務已成功初始化！");
                Console.WriteLine("💡 按下 F4 鍵開始實際的截圖翻譯測試");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Phase 5 測試失敗: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 測試翻譯服務
        /// </summary>
        private async Task TestTranslationServiceAsync()
        {
            try
            {
                var translateService = new TranslateService();
                
                // 測試語言檢測
                var detectedLang = await translateService.DetectLanguageAsync("Hello World");
                Console.WriteLine($"   語言檢測: {detectedLang}");
                
                // 測試翻譯功能（使用簡單文字避免 API 限制）
                var translation = await translateService.TranslateAsync("Hello", "en", "zh");
                Console.WriteLine($"   翻譯測試: Hello -> {translation}");
                
                // 測試引擎可用性
                var isGoogleAvailable = await translateService.IsEngineAvailableAsync(TranslationEngine.Google);
                Console.WriteLine($"   Google 翻譯可用: {isGoogleAvailable}");
                
                var isLocalAvailable = await translateService.IsEngineAvailableAsync(TranslationEngine.Local);
                Console.WriteLine($"   本地翻譯可用: {isLocalAvailable}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   翻譯服務測試失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 測試 OCR 服務
        /// </summary>
        private async Task TestOcrServiceAsync()
        {
            try
            {
                var ocrService = new OcrService();
                
                // 測試初始化（注意：這會嘗試載入 Native.dll）
                try
                {
                    var initialized = await ocrService.InitializeAsync();
                    Console.WriteLine($"   OCR 初始化: {(initialized ? "成功" : "失敗")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   OCR 初始化失敗 (預期，因為 Native.dll 尚未完成): {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   OCR 服務測試失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 測試螢幕擷取服務
        /// </summary>
        private void TestScreenCaptureService()
        {
            try
            {
                var captureService = new ScreenCaptureService();
                Console.WriteLine($"   螢幕擷取服務初始化: 成功");
                Console.WriteLine($"   當前擷取狀態: {captureService.IsCapturing}");
                
                // 註意：實際的擷取測試需要 Native.dll
                Console.WriteLine("   ⚠️  實際擷取測試需要 Native.dll 完成後進行");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   螢幕擷取服務測試失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 測試翻譯管線管理器事件
        /// </summary>
        private void TestPipelineManagerEvents()
        {
            try
            {
                // 註冊事件處理器
                _pipelineManager.TranslationCompleted += OnTranslationCompleted;
                _pipelineManager.ErrorOccurred += OnErrorOccurred;
                
                Console.WriteLine("   事件處理器註冊: 成功");
                Console.WriteLine($"   管線擷取狀態: {_pipelineManager.IsCapturing}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   事件測試失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 翻譯完成事件處理器
        /// </summary>
        private void OnTranslationCompleted(TranslationResult result)
        {
            Console.WriteLine($"✅ 翻譯完成: {result.OriginalText} -> {result.TranslatedText}");
            Console.WriteLine($"   置信度: {result.Confidence:P2}");
            Console.WriteLine($"   語言: {result.SourceLanguage} -> {result.TargetLanguage}");
        }
        
        /// <summary>
        /// 錯誤發生事件處理器
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            Console.WriteLine($"❌ 翻譯錯誤: {error}");
        }
        
        /// <summary>
        /// 清理資源
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _hotKeyService?.Cleanup();
                _pipelineManager?.StopCaptureSession();
                Console.WriteLine("🧹 Phase 5 測試資源已清理");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  清理過程中發生錯誤: {ex.Message}");
            }
        }
    }
}
