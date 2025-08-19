using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonLingo.Core.Services;
using MonLingo.Core.Services.Implementations;

namespace MonLingo.Core.Infrastructure
{
    /// <summary>
    /// 事件聚合器介面
    /// 用於鬆散耦合的事件通信
    /// </summary>
    public interface IEventAggregator
    {
        Task PublishAsync<T>(T eventArgs) where T : class;
        void Publish<T>(T eventArgs) where T : class;
        void Subscribe<T>(Func<T, Task> handler) where T : class;
        void Unsubscribe<T>(Func<T, Task> handler) where T : class;
    }

    /// <summary>
    /// 通知服務介面
    /// </summary>
    public interface INotificationService
    {
        Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info);
        Task ShowErrorAsync(string message, Exception exception = null);
        Task ShowSuccessAsync(string message);
        Task ShowWarningAsync(string message);
        Task ShowInfoAsync(string message);
        
        // 同步版本 (為了向後相容)
        void ShowError(string message, Exception exception = null);
        void ShowWarning(string message);
        void ShowInfo(string message);
    }

    /// <summary>
    /// 依賴注入容器配置
    /// 註冊所有服務和介面
    /// </summary>
    public static class ServiceContainer
    {
        private static IServiceProvider _serviceProvider;
        private static bool _isInitialized = false;

        /// <summary>
        /// 獲取指定類型的服務實例 (必須存在)
        /// </summary>
        public static T GetRequiredService<T>() where T : class
        {
            if (!_isInitialized)
                throw new InvalidOperationException("ServiceContainer has not been initialized.");

            var service = _serviceProvider.GetService<T>();
            if (service == null)
                throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");

            return service;
        }

        /// <summary>
        /// 初始化服務容器
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            var services = new ServiceCollection();
            
            // 註冊核心服務
            RegisterServices(services);
            
            // 建立服務提供者
            _serviceProvider = services.BuildServiceProvider();
            
            // 初始化服務
            await InitializeServicesAsync();
            
            _isInitialized = true;
        }

        /// <summary>
        /// 註冊所有服務
        /// </summary>
        private static void RegisterServices(IServiceCollection services)
        {
            // 註冊核心服務
            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IConfigService, ConfigService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IHotKeyService, HotKeyService>();
            
            // 註冊媒體服務
            services.AddSingleton<ICaptureService, CaptureService>();
            services.AddSingleton<IAudioService, AudioService>();
            
            // 註冊圖像處理服務
            services.AddSingleton<IImagePreprocessor, ImagePreprocessor>();
            services.AddSingleton<ITextRegionDetector, TextRegionDetector>();
            
            // 註冊學習服務
            services.AddSingleton<ILanguageDetectionService, LanguageDetectionService>();
            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<IDictionaryService, DictionaryService>();
            services.AddSingleton<IProgressService, ProgressService>();
            
            // 註冊資料服務
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            
            // 註意: Phase 4 服務 (TimeSyncService, DownloadService) 使用獨立的服務註冊系統
            // 參見 MonLingo.Core.Service.Services.AddCoreServices() 方法
        }

        /// <summary>
        /// 初始化服務
        /// </summary>
        private static async Task InitializeServicesAsync()
        {
            try
            {
                var configService = GetService<IConfigService>();
                await configService.LoadAsync();

                var dataService = GetService<IDataService>();
                await dataService.InitializeAsync();

                var loggingService = GetService<ILoggingService>();
                await loggingService.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"服務初始化失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 獲取服務實例
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (!_isInitialized)
                throw new InvalidOperationException("服務容器尚未初始化");

            return _serviceProvider?.GetService<T>() ?? throw new InvalidOperationException($"無法獲取服務: {typeof(T).Name}");
        }

        /// <summary>
        /// 檢查服務是否可用
        /// </summary>
        public static bool IsServiceAvailable<T>() where T : class
        {
            return _isInitialized && _serviceProvider?.GetService<T>() != null;
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public static void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _isInitialized = false;
        }
    }

    /// <summary>
    /// 事件聚合器實現
    /// 提供發布/訂閱模式的事件通信機制
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();

        public async Task PublishAsync<T>(T eventArgs) where T : class
        {
            if (eventArgs == null) return;

            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                var tasks = handlers.Select(h => h(eventArgs));
                await Task.WhenAll(tasks);
            }
        }

        public void Publish<T>(T eventArgs) where T : class
        {
            _ = PublishAsync(eventArgs);
        }

        public void Subscribe<T>(Func<T, Task> handler) where T : class
        {
            var eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
                _handlers[eventType] = new List<Func<object, Task>>();

            _handlers[eventType].Add(args => handler((T)args));
        }

        public void Unsubscribe<T>(Func<T, Task> handler) where T : class
        {
            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.RemoveAll(h => h.Method == handler.Method);
                if (!handlers.Any())
                    _handlers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 通知服務實現
    /// 提供應用程式通知功能
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IEventAggregator _eventAggregator;

        public NotificationService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        public async Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info)
        {
            var notification = new NotificationEventArgs
            {
                Message = message,
                Type = type,
                Timestamp = DateTime.Now
            };

            await _eventAggregator.PublishAsync(notification);
        }

        public async Task ShowErrorAsync(string message, Exception exception = null)
        {
            var errorMessage = exception != null ? $"{message}: {exception.Message}" : message;
            await ShowNotificationAsync(errorMessage, NotificationType.Error);
        }

        public async Task ShowSuccessAsync(string message)
        {
            await ShowNotificationAsync(message, NotificationType.Success);
        }

        public async Task ShowWarningAsync(string message)
        {
            await ShowNotificationAsync(message, NotificationType.Warning);
        }

        public async Task ShowInfoAsync(string message)
        {
            await ShowNotificationAsync(message, NotificationType.Info);
        }

        // 同步版本實現
        public void ShowError(string message, Exception exception = null)
        {
            _ = ShowErrorAsync(message, exception);
        }

        public void ShowWarning(string message)
        {
            _ = ShowWarningAsync(message);
        }

        public void ShowInfo(string message)
        {
            _ = ShowInfoAsync(message);
        }
    }
}

namespace MonLingo.Core.Services
{
    /// <summary>
    /// 通知類型
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 通知事件參數
    /// </summary>
    public class NotificationEventArgs
    {
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 開始翻譯事件參數
    /// </summary>
    public class StartTranslationEvent
    {
        public string SourceText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 開始音訊翻譯事件參數
    /// </summary>
    public class StartAudioTranslationEvent
    {
        public string AudioSource { get; set; }
        public string TargetLanguage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 翻譯完成事件參數
    /// </summary>
    public class TranslationCompletedEvent
    {
        public string SourceText { get; set; }
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        // 新增 Result 屬性以支援現有代碼
        public object Result { get; set; }
    }

    /// <summary>
    /// 截圖完成事件參數
    /// </summary>
    public class CaptureCompletedEvent
    {
        public System.Drawing.Rectangle Region { get; set; }
        public byte[] ImageData { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        // 新增 Result 屬性以支援現有代碼
        public object Result { get; set; }
    }

    /// <summary>
    /// 歷史記錄項目
    /// </summary>
    public class HistoryEntry
    {
        public int Id { get; set; }
        public string SourceText { get; set; }
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public DateTime Timestamp { get; set; }
        public string TranslationType { get; set; } // "Text", "Audio", "OCR"
        
        // 新增缺失的屬性以支援現有代碼
        public string OriginalText { get; set; }
        public string Engine { get; set; }
        public string Type { get; set; }
    }
}
