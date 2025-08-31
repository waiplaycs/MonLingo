using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonLingo.Core.Service;

namespace MonLingo.Core.Infrastructure
{
    /// <summary>
    /// Phase 5 服務容器配置
    /// 負責註冊所有翻譯相關服務
    /// </summary>
    public static class Phase5ServiceContainer
    {
        private static IServiceProvider _serviceProvider;
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化 Phase 5 服務容器
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            var services = new ServiceCollection();
            
            // 註冊核心翻譯服務
            RegisterCoreServices(services);
            
            // 註冊商業化服務（Phase 6）
            RegisterBusinessServices(services);
            
            // 註冊 UI 相關服務
            RegisterUIServices(services);
            
            // 註冊配置服務
            RegisterConfigurationServices(services);
            
            // 建立服務提供者
            _serviceProvider = services.BuildServiceProvider();
            _isInitialized = true;
        }

        /// <summary>
        /// 註冊核心翻譯服務
        /// </summary>
        private static void RegisterCoreServices(IServiceCollection services)
        {
            // 先註冊基礎服務
            services.AddSingleton<MonLingo.Core.Service.INotificationService, MonLingo.Core.Service.NotificationService>();
            services.AddSingleton<MonLingo.Core.Service.IConfigService, MonLingo.Core.Service.ConfigService>();
            
            // 註冊語言配置服務
            services.AddSingleton<MonLingo.Core.Service.ILanguageConfigService, MonLingo.Core.Service.LanguageConfigService>();
            
            // 註冊顯示服務
            services.AddSingleton<MonLingo.Core.Service.IDisplayService, MonLingo.Core.Service.DisplayService>();
            
            // 再註冊依賴基礎服務的服務
            services.AddSingleton<MonLingo.Core.Service.IScreenCaptureService, MonLingo.Core.Service.ScreenCaptureService>();
            // 🎯 使用真正的 PaddleOCR 服務（已下載模型）
            services.AddSingleton<MonLingo.Core.Service.IOcrService, MonLingo.Core.Service.RealOcrService>();
            services.AddSingleton<MonLingo.Core.Service.ITranslateService, MonLingo.Core.Service.TranslateService>();
            
            // 最後註冊高層服務
            services.AddSingleton<MonLingo.Core.Service.ITranslationPipelineManager, MonLingo.Core.Service.TranslationPipelineManager>();
            
            // UI 橋接服務
            services.AddSingleton<MonLingo.Core.Service.UITranslationBridge>();
        }

        /// <summary>
        /// 註冊 UI 相關服務  
        /// </summary>
        private static void RegisterUIServices(IServiceCollection services)
        {
            // 通知服務（重用現有的）
            services.AddSingleton<ISimpleNotificationService, Phase5NotificationService>();
            services.AddSingleton<ISimpleEventAggregator, Phase5EventAggregator>();
        }

        /// <summary>
        /// 註冊配置服務
        /// </summary>
        private static void RegisterConfigurationServices(IServiceCollection services)
        {
            // 配置服務已在 RegisterCoreServices 中註冊
            // 這裡可以添加額外的配置相關服務
        }

        /// <summary>
        /// 註冊商業化服務（Phase 6）
        /// </summary>
        private static void RegisterBusinessServices(IServiceCollection services)
        {
            // 註冊商業化核心服務
            services.AddSingleton<MonLingo.Core.Service.IUserService, MonLingo.Core.Service.UserService>();
            services.AddSingleton<MonLingo.Core.Service.ILicenseService, MonLingo.Core.Service.LicenseService>();
            services.AddSingleton<MonLingo.Core.Service.ICurrencyService, MonLingo.Core.Service.CurrencyService>();
            services.AddSingleton<MonLingo.Core.Service.IPointsService, MonLingo.Core.Service.PointsService>();
            services.AddSingleton<MonLingo.Core.Service.ICoinsService, MonLingo.Core.Service.CoinsService>();
            
            // 註冊 API 客戶端（如果需要）
            services.AddSingleton<MonLingo.Core.Service.IApiClient, MonLingo.Core.Service.ApiClient>();
        }

        /// <summary>
        /// 獲取服務實例
        /// </summary>
        public static T GetService<T>()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// 獲取可選服務實例
        /// </summary>
        public static T GetOptionalService<T>()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// 檢查服務是否已註冊
        /// </summary>
        public static bool IsServiceRegistered<T>()
        {
            if (!_isInitialized)
                return false;

            return _serviceProvider.GetService<T>() != null;
        }

        /// <summary>
        /// 清理服務容器
        /// </summary>
        public static void Cleanup()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _serviceProvider = null;
            _isInitialized = false;
        }
    }

    /// <summary>
    /// Phase 5 專用通知服務實現（整合現有架構）
    /// </summary>
    public class Phase5NotificationService : ISimpleNotificationService
    {
        public void ShowNotification(string title, string message)
        {
            try
            {
                System.Windows.MessageBox.Show(message, title, 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知顯示失敗: {ex.Message}");
            }
        }

        public void ShowError(string message)
        {
            try
            {
                System.Windows.MessageBox.Show(message, "錯誤", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"錯誤通知顯示失敗: {ex.Message}");
            }
        }

        public void ShowWarning(string message)
        {
            try
            {
                System.Windows.MessageBox.Show(message, "警告", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"警告通知顯示失敗: {ex.Message}");
            }
        }

        public void ShowInfo(string message)
        {
            try
            {
                System.Windows.MessageBox.Show(message, "資訊", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"資訊通知顯示失敗: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Phase 5 專用事件聚合器實現
    /// </summary>
    public class Phase5EventAggregator : ISimpleEventAggregator
    {
        public void Publish<T>(T eventObject) where T : class
        {
            // 基本事件發布實現
            System.Diagnostics.Debug.WriteLine($"Phase 5 事件發布: {typeof(T).Name}");
        }

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            // 基本事件訂閱實現
            System.Diagnostics.Debug.WriteLine($"Phase 5 事件訂閱: {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Phase 5 捕獲請求事件
    /// </summary>
    public class Phase5CaptureRequestedEvent
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    /// <summary>
    /// Phase 5 區域選擇事件
    /// </summary>
    public class Phase5RegionSelectionEvent
    {
        public int RegionType { get; set; }
        public DateTime Timestamp { get; } = DateTime.Now;
    }
}
