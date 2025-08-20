using System;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 授權管理服務介面（基於 Gaminik.Service.LicenseService 設計，PRD §2.2.2 完整實現）
    /// </summary>
    public interface ILicenseService
    {
        /// <summary>
        /// 檢查使用者是否為 Pro 會員（PRD §2.2.2）
        /// </summary>
        bool IsPro();

        /// <summary>
        /// 檢查特定功能是否可用（PRD §2.2.2）
        /// </summary>
        bool IsFeatureAvailable(string featureName);

        /// <summary>
        /// 驗證授權（與伺服器同步）（PRD §2.2.2）
        /// </summary>
        Task<bool> ValidateLicenseAsync();

        /// <summary>
        /// 獲取授權到期時間
        /// </summary>
        DateTime GetExpireTime();

        /// <summary>
        /// 獲取當前帳號等級
        /// </summary>
        AccountLevel GetAccountLevel();

        /// <summary>
        /// 檢查今日翻譯配額是否足夠
        /// </summary>
        bool HasTranslationQuota();

        /// <summary>
        /// 消耗翻譯配額
        /// </summary>
        Task<bool> ConsumeTranslationQuotaAsync();

        /// <summary>
        /// 獲取今日剩餘翻譯次數
        /// </summary>
        int GetRemainingTranslations();
    }

    /// <summary>
    /// 使用者服務介面（PRD §11.1）
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 獲取當前使用者資料（非同步）
        /// </summary>
        Task<User> GetCurrentUserAsync();

        /// <summary>
        /// 獲取當前使用者資料（同步）
        /// </summary>
        User GetCurrentUser();

        /// <summary>
        /// 電子郵件登入（PRD §11.1）
        /// </summary>
        Task<bool> LoginAsync(string email, string password);

        /// <summary>
        /// 手機簡訊登入（PRD §11.1）
        /// </summary>
        Task<bool> LoginViaSmsAsync(string phone, string code);

        /// <summary>
        /// 更新使用者資料
        /// </summary>
        Task UpdateUserAsync(User user);

        /// <summary>
        /// 登出
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// 註冊新使用者
        /// </summary>
        Task<bool> RegisterAsync(string email, string password, string username);

        /// <summary>
        /// 發送驗證碼
        /// </summary>
        Task<bool> SendVerificationCodeAsync(string phone);

        /// <summary>
        /// 重設密碼
        /// </summary>
        Task<bool> ResetPasswordAsync(string email);

        /// <summary>
        /// 檢查登入狀態
        /// </summary>
        bool IsLoggedIn();
    }

    /// <summary>
    /// API客戶端服務介面
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// 電子郵件登入API
        /// </summary>
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// 簡訊登入API
        /// </summary>
        Task<LoginResponse> LoginViaSmsAsync(SmsLoginRequest request);

        /// <summary>
        /// 根據Token獲取使用者資料
        /// </summary>
        Task<LoginResponse> GetUserByTokenAsync(string token);

        /// <summary>
        /// 驗證授權API
        /// </summary>
        Task<LicenseValidationResponse> ValidateLicenseAsync(string sessionToken);

        /// <summary>
        /// 註冊使用者API
        /// </summary>
        Task<LoginResponse> RegisterAsync(string email, string password, string username);

        /// <summary>
        /// 發送驗證碼API
        /// </summary>
        Task<bool> SendVerificationCodeAsync(string phone);

        /// <summary>
        /// 重設密碼API
        /// </summary>
        Task<bool> ResetPasswordAsync(string email);
    }
}
