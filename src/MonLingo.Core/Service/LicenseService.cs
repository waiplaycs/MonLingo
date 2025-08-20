using System;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 授權管理服務實現（基於 Gaminik.Service.LicenseService 設計，PRD §2.2.2 完整實現）
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly IUserService _userService;
        private readonly IConfigService _configService;
        private readonly IApiClient _apiClient;

        public LicenseService(IUserService userService, IConfigService configService, IApiClient apiClient)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// 檢查使用者是否為 Pro 會員（PRD §2.2.2）
        /// </summary>
        public bool IsPro()
        {
            var user = _userService.GetCurrentUser();
            return user?.IsPro ?? false;
        }

        /// <summary>
        /// 檢查特定功能是否可用（PRD §2.2.2）
        /// </summary>
        public bool IsFeatureAvailable(string featureName)
        {
            // 功能權限表（基於 Gaminik 分析）
            var freeFeatures = new[]
            {
                "BasicTranslation",
                "ScreenCapture",
                "GoogleTranslate",
                "SimpleOCR",
                "BasicHotkeys"
            };

            var proFeatures = new[]
            {
                "DeepLTranslation",
                "GPTTranslation",
                "AudioTranscription",
                "BatchTranslation",
                "CustomHotkeys",
                "AdvancedOCR",
                "MultiLanguageOCR",
                "ComicMode",
                "AutoTranslation"
            };

            // 免費功能對所有使用者開放
            if (Array.Exists(freeFeatures, f => f.Equals(featureName, StringComparison.OrdinalIgnoreCase)))
                return true;

            // Pro功能需要會員權限
            if (Array.Exists(proFeatures, f => f.Equals(featureName, StringComparison.OrdinalIgnoreCase)))
                return IsPro();

            // 未知功能預設為免費
            return true;
        }

        /// <summary>
        /// 驗證授權（與伺服器同步）（PRD §2.2.2）
        /// </summary>
        public async Task<bool> ValidateLicenseAsync()
        {
            try
            {
                var user = _userService.GetCurrentUser();
                if (user == null || string.IsNullOrEmpty(user.SessionToken)) 
                    return false;

                // 呼叫授權驗證 API
                var response = await _apiClient.ValidateLicenseAsync(user.SessionToken);

                if (response?.IsValid == true)
                {
                    // 更新本地授權狀態
                    user.ExpireTime = response.ExpireTime;
                    user.AccountLevel = response.AccountLevel;

                    await _userService.UpdateUserAsync(user);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // 授權驗證失敗時的處理 - 記錄錯誤但不拋出例外
                System.Diagnostics.Debug.WriteLine($"License validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 獲取授權到期時間
        /// </summary>
        public DateTime GetExpireTime()
        {
            var user = _userService.GetCurrentUser();
            return user?.ExpireTime ?? DateTime.MinValue;
        }

        /// <summary>
        /// 獲取當前帳號等級
        /// </summary>
        public AccountLevel GetAccountLevel()
        {
            var user = _userService.GetCurrentUser();
            if (user == null) return AccountLevel.Free;

            // 檢查是否過期
            if (user.ExpireTime <= DateTime.Now && user.AccountLevel == AccountLevel.Pro)
                return AccountLevel.Expired;

            return user.AccountLevel;
        }

        /// <summary>
        /// 檢查今日翻譯配額是否足夠
        /// </summary>
        public bool HasTranslationQuota()
        {
            var remaining = GetRemainingTranslations();
            return remaining > 0 || IsPro(); // Pro會員無限制
        }

        /// <summary>
        /// 消耗翻譯配額
        /// </summary>
        public async Task<bool> ConsumeTranslationQuotaAsync()
        {
            if (IsPro()) return true; // Pro會員無限制

            var remaining = GetRemainingTranslations();
            if (remaining <= 0) return false;

            // 減少今日翻譯次數
            var today = DateTime.Today.ToString("yyyyMMdd");
            var key = $"DailyTranslations_{today}";
            var currentCount = await _configService.GetAsync<int>(key);
            await _configService.SaveAsync(key, currentCount + 1);

            return true;
        }

        /// <summary>
        /// 獲取今日剩餘翻譯次數
        /// </summary>
        public int GetRemainingTranslations()
        {
            if (IsPro()) return int.MaxValue; // Pro會員無限制

            var today = DateTime.Today.ToString("yyyyMMdd");
            var key = $"DailyTranslations_{today}";
            var usedCount = _configService.GetAsync<int>(key).Result;
            var dailyLimit = 50; // 免費使用者每日50次限制

            return Math.Max(0, dailyLimit - usedCount);
        }
    }
}
