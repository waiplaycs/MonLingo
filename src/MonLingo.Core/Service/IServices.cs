using Microsoft.Extensions.DependencyInjection;
using MonLingo.Core.Service;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 服務註冊與配置
    /// </summary>
    public static class Services
    {
        /// <summary>
        /// 註冊所有核心服務
        /// </summary>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // 註冊設定服務
            services.AddSingleton<IConfigService, ConfigService>();

            // 註冊熱鍵服務
            services.AddSingleton<IHotkeyService, HotkeyService>();

            // 註冊音訊服務 (Phase 4)
            services.AddTransient<IAudioService, AudioService>();

            // 註冊轉錄服務 (Phase 4)
            services.AddSingleton<ITranscriptionService, TranscriptionService>();

            // 註冊時間同步服務 (Phase 4) - 使用 GuerrillaNtp 2.0.1
            services.AddSingleton<ITimeSyncService, TimeSyncService>();

            // 註冊下載服務 (Phase 4)
            services.AddSingleton<IDownloadService, DownloadService>();

            // 註冊商業化服務 (Phase 6)
            services.AddSingleton<IApiClient, ApiClient>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ILicenseService, LicenseService>();
            services.AddSingleton<IPointsService, PointsService>();
            services.AddSingleton<ICoinsService, CoinsService>();
            services.AddSingleton<ICurrencyService, CurrencyService>();

            return services;
        }
    }
}