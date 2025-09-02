using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 硬幣管理服務實現（基於 Gaminik Coins 消耗模型設計）
    /// </summary>
    public class CoinsService : ICoinsService
    {
        private readonly IUserService _userService;
        private readonly IConfigService _configService;
        private readonly IApiClient _apiClient;
        private readonly INotificationService _notificationService;

        public CoinsService(
            IUserService userService,
            IConfigService configService,
            IApiClient apiClient,
            INotificationService notificationService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// 高級翻譯消耗硬幣
        /// </summary>
        public async Task<bool> ConsumeCoinsForPremiumTranslationAsync(MonLingo.Core.Models.TranslationEngine engine, int textLength, string description = "高級翻譯")
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null) return false;

                var cost = CalculateTranslationCost(engine, textLength);
                
                // 檢查餘額是否足夠
                if (user.Coins < cost)
                {
                    _notificationService?.ShowWarning($"硬幣餘額不足！需要 {cost} 硬幣，目前餘額 {user.Coins} 硬幣");
                    return false;
                }

                // 扣除硬幣
                user.Coins -= cost;
                await _userService.UpdateUserAsync(user);

                // 記錄交易歷史
                var detailedDescription = $"{description}（{engine}，{textLength}字符）";
                await RecordCoinsTransactionAsync(TransactionType.Consume, cost, detailedDescription, "翻譯");

                System.Diagnostics.Debug.WriteLine($"Consumed {cost} coins for {engine} translation ({textLength} characters)");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Consume coins for translation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 購買硬幣
        /// </summary>
        public async Task<bool> PurchaseCoinsAsync(int amount, decimal costUSD, string paymentMethod)
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null) return false;

                // 這裡應該與支付服務整合，驗證支付是否成功
                // 目前先模擬支付成功
                
                // 增加硬幣
                user.Coins += amount;
                await _userService.UpdateUserAsync(user);

                // 記錄購買歷史
                await RecordCoinsTransactionAsync(
                    TransactionType.Purchase, 
                    amount, 
                    $"購買硬幣（{paymentMethod}）", 
                    "購買", 
                    costUSD);

                _notificationService?.ShowSuccess($"購買成功！獲得 {amount} 硬幣");
                return true;
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError("購買硬幣失敗");
                System.Diagnostics.Debug.WriteLine($"Purchase coins failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 獲取硬幣餘額
        /// </summary>
        public async Task<int> GetCoinsBalanceAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            return user?.Coins ?? 0;
        }

        /// <summary>
        /// 刷新硬幣餘額（從伺服器同步）
        /// </summary>
        public async Task RefreshCoinsBalanceAsync()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null || string.IsNullOrEmpty(user.SessionToken)) return;

                // 從伺服器獲取最新的使用者資料
                var response = await _apiClient.GetUserByTokenAsync(user.SessionToken);
                if (response?.Success == true && response.User != null)
                {
                    // 只更新硬幣餘額，保留其他本地變更
                    user.Coins = response.User.Coins;
                    await _userService.UpdateUserAsync(user);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Refresh coins balance failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 獲取硬幣交易歷史
        /// </summary>
        public async Task<List<CoinsTransaction>> GetCoinsHistoryAsync(int limit = 50)
        {
            try
            {
                var history = await _configService.GetAsync<List<CoinsTransaction>>("CoinsHistory") ?? new List<CoinsTransaction>();
                
                // 按時間倒序排列，取最近的記錄
                history.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                
                return history.Count > limit ? history.GetRange(0, limit) : history;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get coins history failed: {ex.Message}");
                return new List<CoinsTransaction>();
            }
        }

        /// <summary>
        /// 計算翻譯成本
        /// </summary>
        public int CalculateTranslationCost(MonLingo.Core.Models.TranslationEngine engine, int textLength)
        {
            return TranslationCost.CalculateCoins(engine, textLength);
        }

        /// <summary>
        /// 檢查硬幣餘額是否足夠
        /// </summary>
        public async Task<bool> HasSufficientCoinsAsync(MonLingo.Core.Models.TranslationEngine engine, int textLength)
        {
            var balance = await GetCoinsBalanceAsync();
            var cost = CalculateTranslationCost(engine, textLength);
            return balance >= cost;
        }

        /// <summary>
        /// 贈送硬幣（管理員功能）
        /// </summary>
        public async Task<bool> GrantCoinsAsync(int amount, string reason)
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null) return false;

                user.Coins += amount;
                await _userService.UpdateUserAsync(user);

                // 記錄贈送歷史
                await RecordCoinsTransactionAsync(TransactionType.Reward, amount, $"系統贈送：{reason}", "系統");

                _notificationService?.ShowSuccess($"獲得系統贈送 {amount} 硬幣：{reason}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Grant coins failed: {ex.Message}");
                return false;
            }
        }

        #region 私有輔助方法

        private async Task RecordCoinsTransactionAsync(TransactionType type, int amount, string description, string source, decimal? costUSD = null)
        {
            var transaction = new CoinsTransaction
            {
                Type = type,
                Amount = amount,
                Description = description,
                Source = source,
                CostUSD = costUSD,
                Timestamp = DateTime.Now
            };

            await _configService.AppendToListAsync("CoinsHistory", transaction);
        }

        #endregion
    }

    /// <summary>
    /// 貨幣管理統一服務實現
    /// </summary>
    public class CurrencyService : ICurrencyService
    {
        private readonly IPointsService _pointsService;
        private readonly ICoinsService _coinsService;
        private readonly ILicenseService _licenseService;
        private readonly IUserService _userService;

        public CurrencyService(
            IPointsService pointsService,
            ICoinsService coinsService,
            ILicenseService licenseService,
            IUserService userService)
        {
            _pointsService = pointsService ?? throw new ArgumentNullException(nameof(pointsService));
            _coinsService = coinsService ?? throw new ArgumentNullException(nameof(coinsService));
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// 轉換翻譯引擎枚舉類型
        /// </summary>
        private MonLingo.Core.Models.TranslationEngine ConvertEngineType(MonLingo.Core.Service.TranslationEngine engine)
        {
            return engine switch
            {
                MonLingo.Core.Service.TranslationEngine.Google => MonLingo.Core.Models.TranslationEngine.Google,
                MonLingo.Core.Service.TranslationEngine.DeepL => MonLingo.Core.Models.TranslationEngine.DeepL,
                MonLingo.Core.Service.TranslationEngine.Microsoft => MonLingo.Core.Models.TranslationEngine.Bing,
                MonLingo.Core.Service.TranslationEngine.Baidu => MonLingo.Core.Models.TranslationEngine.Baidu,
                MonLingo.Core.Service.TranslationEngine.Local => MonLingo.Core.Models.TranslationEngine.Local,
                _ => MonLingo.Core.Models.TranslationEngine.Google
            };
        }

        /// <summary>
        /// 檢查是否可以進行翻譯（積分或硬幣足夠）
        /// </summary>
        public async Task<bool> CanPerformTranslationAsync(MonLingo.Core.Service.TranslationEngine engine, int textLength)
        {
            // Pro會員無限制
            if (_licenseService.IsPro()) return true;

            var modelEngine = ConvertEngineType(engine);

            // 基礎翻譯使用積分
            if (engine == MonLingo.Core.Service.TranslationEngine.Google)
            {
                var pointsCost = TranslationCost.CalculatePoints(textLength);
                var pointsBalance = await _pointsService.GetPointsBalanceAsync();
                return pointsBalance >= pointsCost;
            }

            // 高級翻譯使用硬幣
            return await _coinsService.HasSufficientCoinsAsync(modelEngine, textLength);
        }

        /// <summary>
        /// 執行翻譯消費（優先使用積分，再使用硬幣）
        /// </summary>
        public async Task<bool> ConsumeForTranslationAsync(MonLingo.Core.Service.TranslationEngine engine, int textLength)
        {
            // Pro會員無限制
            if (_licenseService.IsPro()) return true;

            var modelEngine = ConvertEngineType(engine);

            // 基礎翻譯使用積分
            if (engine == MonLingo.Core.Service.TranslationEngine.Google)
            {
                var pointsCost = TranslationCost.CalculatePoints(textLength);
                return await _pointsService.ConsumePointsForTranslationAsync(pointsCost, "Google翻譯");
            }

            // 高級翻譯使用硬幣
            return await _coinsService.ConsumeCoinsForPremiumTranslationAsync(modelEngine, textLength);
        }

        /// <summary>
        /// 獲取使用者貨幣狀態
        /// </summary>
        public async Task<CurrencyStatus> GetCurrencyStatusAsync()
        {
            var pointsBalance = await _pointsService.GetPointsBalanceAsync();
            var coinsBalance = await _coinsService.GetCoinsBalanceAsync();
            var isProMember = _licenseService.IsPro();
            var remainingTranslations = _licenseService.GetRemainingTranslations();

            return new CurrencyStatus
            {
                PointsBalance = pointsBalance,
                CoinsBalance = coinsBalance,
                IsProMember = isProMember,
                RemainingTranslations = remainingTranslations,
                LastUpdated = DateTime.Now
            };
        }

        /// <summary>
        /// 同步所有貨幣餘額
        /// </summary>
        public async Task SyncAllBalancesAsync()
        {
            await _coinsService.RefreshCoinsBalanceAsync();
            await _licenseService.ValidateLicenseAsync();
        }
    }
}
