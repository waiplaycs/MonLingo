using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 積分管理服務介面
    /// </summary>
    public interface IPointsService
    {
        /// <summary>
        /// 激勵廣告獲取積分
        /// </summary>
        Task<int> AddPointsFromRewardedAdAsync();

        /// <summary>
        /// 每日簽到積分
        /// </summary>
        Task<int> AddPointsFromDailyCheckInAsync();

        /// <summary>
        /// 分享獲取積分
        /// </summary>
        Task<int> AddPointsFromSharingAsync();

        /// <summary>
        /// 邀請朋友獲取積分
        /// </summary>
        Task<int> AddPointsFromInviteAsync(string inviteCode);

        /// <summary>
        /// 積分消耗（基礎翻譯）
        /// </summary>
        Task<bool> ConsumePointsForTranslationAsync(int cost, string description = "基礎翻譯");

        /// <summary>
        /// 獲取積分餘額
        /// </summary>
        Task<int> GetPointsBalanceAsync();

        /// <summary>
        /// 獲取積分交易歷史
        /// </summary>
        Task<List<PointsTransaction>> GetPointsHistoryAsync(int limit = 50);

        /// <summary>
        /// 檢查活動是否可用
        /// </summary>
        Task<bool> IsActivityAvailableAsync(RewardActivityType activityType);

        /// <summary>
        /// 獲取今日活動統計
        /// </summary>
        Task<UserActivityRecord> GetTodayActivityAsync(RewardActivityType activityType);
    }

    /// <summary>
    /// 硬幣管理服務介面
    /// </summary>
    public interface ICoinsService
    {
        /// <summary>
        /// 高級翻譯消耗硬幣
        /// </summary>
        Task<bool> ConsumeCoinsForPremiumTranslationAsync(MonLingo.Core.Models.TranslationEngine engine, int textLength, string description = "高級翻譯");

        /// <summary>
        /// 購買硬幣
        /// </summary>
        Task<bool> PurchaseCoinsAsync(int amount, decimal costUSD, string paymentMethod);

        /// <summary>
        /// 獲取硬幣餘額
        /// </summary>
        Task<int> GetCoinsBalanceAsync();

        /// <summary>
        /// 刷新硬幣餘額（從伺服器同步）
        /// </summary>
        Task RefreshCoinsBalanceAsync();

        /// <summary>
        /// 獲取硬幣交易歷史
        /// </summary>
        Task<List<CoinsTransaction>> GetCoinsHistoryAsync(int limit = 50);

        /// <summary>
        /// 計算翻譯成本
        /// </summary>
        int CalculateTranslationCost(MonLingo.Core.Models.TranslationEngine engine, int textLength);

        /// <summary>
        /// 檢查硬幣餘額是否足夠
        /// </summary>
        Task<bool> HasSufficientCoinsAsync(MonLingo.Core.Models.TranslationEngine engine, int textLength);

        /// <summary>
        /// 贈送硬幣（管理員功能）
        /// </summary>
        Task<bool> GrantCoinsAsync(int amount, string reason);
    }

    /// <summary>
    /// 貨幣管理統一介面
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// 檢查是否可以進行翻譯（積分或硬幣足夠）
        /// </summary>
        Task<bool> CanPerformTranslationAsync(MonLingo.Core.Service.TranslationEngine engine, int textLength);

        /// <summary>
        /// 執行翻譯消費（優先使用積分，再使用硬幣）
        /// </summary>
        Task<bool> ConsumeForTranslationAsync(MonLingo.Core.Service.TranslationEngine engine, int textLength);

        /// <summary>
        /// 獲取使用者貨幣狀態
        /// </summary>
        Task<CurrencyStatus> GetCurrencyStatusAsync();

        /// <summary>
        /// 同步所有貨幣餘額
        /// </summary>
        Task SyncAllBalancesAsync();
    }

    /// <summary>
    /// 貨幣狀態模型
    /// </summary>
    public class CurrencyStatus
    {
        public int PointsBalance { get; set; }
        public int CoinsBalance { get; set; }
        public bool IsProMember { get; set; }
        public int RemainingTranslations { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
