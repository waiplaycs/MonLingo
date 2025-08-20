using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 積分管理服務實現（基於 Gaminik AddPointsManager 設計，PRD §11.3 完整實現）
    /// </summary>
    public class PointsService : IPointsService
    {
        private readonly IUserService _userService;
        private readonly IConfigService _configService;
        private readonly IApiClient _apiClient;
        private readonly INotificationService _notificationService;

        // 活動配置
        private readonly Dictionary<RewardActivityType, RewardActivity> _rewardConfig;

        public PointsService(
            IUserService userService, 
            IConfigService configService,
            IApiClient apiClient,
            INotificationService notificationService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // 初始化獎勵活動配置
            _rewardConfig = new Dictionary<RewardActivityType, RewardActivity>
            {
                [RewardActivityType.DailyCheckIn] = new RewardActivity
                {
                    Type = RewardActivityType.DailyCheckIn,
                    PointsReward = 20, // 基礎簽到20積分
                    CoinsReward = 0,
                    DailyLimit = 1,
                    IsEnabled = true
                },
                [RewardActivityType.ShareApp] = new RewardActivity
                {
                    Type = RewardActivityType.ShareApp,
                    PointsReward = 30,
                    CoinsReward = 0,
                    DailyLimit = 3, // 每日最多3次分享
                    IsEnabled = true
                },
                [RewardActivityType.WatchAd] = new RewardActivity
                {
                    Type = RewardActivityType.WatchAd,
                    PointsReward = 50, // 廣告獎勵較高
                    CoinsReward = 5,   // 還贈送少量硬幣
                    DailyLimit = 5,    // 每日最多5次
                    IsEnabled = true
                },
                [RewardActivityType.InviteFriend] = new RewardActivity
                {
                    Type = RewardActivityType.InviteFriend,
                    PointsReward = 100,
                    CoinsReward = 20,
                    DailyLimit = -1, // 無限制
                    IsEnabled = true
                }
            };
        }

        /// <summary>
        /// 激勵廣告獲取積分（PRD §11.3）
        /// </summary>
        public async Task<int> AddPointsFromRewardedAdAsync()
        {
            try
            {
                // 檢查活動是否可用
                if (!await IsActivityAvailableAsync(RewardActivityType.WatchAd))
                {
                    _notificationService?.ShowWarning("今日觀看廣告次數已達上限");
                    return 0;
                }

                var user = await _userService.GetCurrentUserAsync();
                if (user == null) return 0;

                var config = _rewardConfig[RewardActivityType.WatchAd];
                
                // 更新使用者積分和硬幣
                user.Points += config.PointsReward;
                user.Coins += config.CoinsReward;
                await _userService.UpdateUserAsync(user);

                // 記錄交易歷史
                await RecordPointsTransactionAsync(TransactionType.Reward, config.PointsReward, "觀看廣告獎勵", "廣告");
                if (config.CoinsReward > 0)
                {
                    await RecordCoinsTransactionAsync(TransactionType.Reward, config.CoinsReward, "觀看廣告獎勵", "廣告");
                }

                // 更新活動記錄
                await UpdateActivityRecordAsync(RewardActivityType.WatchAd, config.PointsReward, config.CoinsReward);

                _notificationService?.ShowSuccess($"觀看廣告完成！獲得 {config.PointsReward} 積分、{config.CoinsReward} 硬幣");
                return config.PointsReward;
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError("積分獲取失敗");
                System.Diagnostics.Debug.WriteLine($"Add points from ad failed: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 每日簽到積分（PRD §11.3）
        /// </summary>
        public async Task<int> AddPointsFromDailyCheckInAsync()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null) return 0;

                var today = DateTime.Today;
                var lastCheckIn = await GetLastCheckInDateAsync();

                // 檢查是否已經簽到
                if (lastCheckIn.Date == today)
                {
                    _notificationService?.ShowWarning("今日已經簽到過了");
                    return 0;
                }

                // 計算連續簽到獎勵
                var consecutiveDays = CalculateConsecutiveDays(lastCheckIn, today);
                var basePoints = _rewardConfig[RewardActivityType.DailyCheckIn].PointsReward;
                var bonusPoints = Math.Min(consecutiveDays * 5, 50); // 連續獎勵，最高50
                var totalPoints = basePoints + bonusPoints;

                // 更新積分
                user.Points += totalPoints;
                await _userService.UpdateUserAsync(user);

                // 記錄交易
                await RecordPointsTransactionAsync(TransactionType.Reward, totalPoints, $"每日簽到（連續{consecutiveDays + 1}天）", "簽到");

                // 更新簽到記錄
                await SaveCheckInDateAsync(today);
                await UpdateConsecutiveDaysAsync(consecutiveDays + 1);
                await UpdateActivityRecordAsync(RewardActivityType.DailyCheckIn, totalPoints, 0);

                _notificationService?.ShowSuccess($"簽到成功！獲得 {totalPoints} 積分（連續{consecutiveDays + 1}天）");
                return totalPoints;
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError("簽到失敗");
                System.Diagnostics.Debug.WriteLine($"Daily check-in failed: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 分享獲取積分（PRD §11.3）
        /// </summary>
        public async Task<int> AddPointsFromSharingAsync()
        {
            try
            {
                // 檢查活動是否可用
                if (!await IsActivityAvailableAsync(RewardActivityType.ShareApp))
                {
                    _notificationService?.ShowWarning("今日分享積分已達上限");
                    return 0;
                }

                var user = await _userService.GetCurrentUserAsync();
                if (user == null) return 0;

                var config = _rewardConfig[RewardActivityType.ShareApp];
                
                // 更新積分
                user.Points += config.PointsReward;
                await _userService.UpdateUserAsync(user);

                // 記錄交易
                await RecordPointsTransactionAsync(TransactionType.Reward, config.PointsReward, "分享應用", "分享");
                await UpdateActivityRecordAsync(RewardActivityType.ShareApp, config.PointsReward, 0);

                _notificationService?.ShowSuccess($"分享成功！獲得 {config.PointsReward} 積分");
                return config.PointsReward;
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError("分享積分獲取失敗");
                System.Diagnostics.Debug.WriteLine($"Add points from sharing failed: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 邀請朋友獲取積分
        /// </summary>
        public async Task<int> AddPointsFromInviteAsync(string inviteCode)
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null) return 0;

                var config = _rewardConfig[RewardActivityType.InviteFriend];
                
                // 更新積分和硬幣
                user.Points += config.PointsReward;
                user.Coins += config.CoinsReward;
                await _userService.UpdateUserAsync(user);

                // 記錄交易
                await RecordPointsTransactionAsync(TransactionType.Reward, config.PointsReward, $"邀請朋友（邀請碼：{inviteCode}）", "邀請");
                await RecordCoinsTransactionAsync(TransactionType.Reward, config.CoinsReward, $"邀請朋友（邀請碼：{inviteCode}）", "邀請");
                await UpdateActivityRecordAsync(RewardActivityType.InviteFriend, config.PointsReward, config.CoinsReward);

                _notificationService?.ShowSuccess($"邀請成功！獲得 {config.PointsReward} 積分、{config.CoinsReward} 硬幣");
                return config.PointsReward;
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError("邀請積分獲取失敗");
                System.Diagnostics.Debug.WriteLine($"Add points from invite failed: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 積分消耗（基礎翻譯）（PRD §11.3）
        /// </summary>
        public async Task<bool> ConsumePointsForTranslationAsync(int cost, string description = "基礎翻譯")
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null || user.Points < cost) return false;

                user.Points -= cost;
                await _userService.UpdateUserAsync(user);

                // 記錄消費歷史
                await RecordPointsTransactionAsync(TransactionType.Consume, cost, description, "翻譯");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Consume points failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 獲取積分餘額
        /// </summary>
        public async Task<int> GetPointsBalanceAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            return user?.Points ?? 0;
        }

        /// <summary>
        /// 獲取積分交易歷史
        /// </summary>
        public async Task<List<PointsTransaction>> GetPointsHistoryAsync(int limit = 50)
        {
            try
            {
                var history = await _configService.GetAsync<List<PointsTransaction>>("PointsHistory") ?? new List<PointsTransaction>();
                
                // 按時間倒序排列，取最近的記錄
                history.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                
                return history.Count > limit ? history.GetRange(0, limit) : history;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get points history failed: {ex.Message}");
                return new List<PointsTransaction>();
            }
        }

        /// <summary>
        /// 檢查活動是否可用
        /// </summary>
        public async Task<bool> IsActivityAvailableAsync(RewardActivityType activityType)
        {
            try
            {
                if (!_rewardConfig.TryGetValue(activityType, out var config) || !config.IsEnabled)
                    return false;

                // 檢查活動是否過期
                if (config.ExpiryDate.HasValue && config.ExpiryDate.Value < DateTime.Now)
                    return false;

                // 檢查每日限制
                if (config.DailyLimit > 0)
                {
                    var todayActivity = await GetTodayActivityAsync(activityType);
                    return todayActivity.Count < config.DailyLimit;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Check activity availability failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 獲取今日活動統計
        /// </summary>
        public async Task<UserActivityRecord> GetTodayActivityAsync(RewardActivityType activityType)
        {
            try
            {
                var today = DateTime.Today.ToString("yyyyMMdd");
                var key = $"Activity_{activityType}_{today}";
                
                var record = await _configService.GetAsync<UserActivityRecord>(key);
                if (record == null)
                {
                    record = new UserActivityRecord
                    {
                        UserId = _userService.GetCurrentUser()?.UserId,
                        Date = DateTime.Today,
                        ActivityType = activityType,
                        Count = 0,
                        PointsEarned = 0,
                        CoinsEarned = 0
                    };
                }

                return record;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get today activity failed: {ex.Message}");
                return new UserActivityRecord();
            }
        }

        #region 私有輔助方法

        private async Task<DateTime> GetLastCheckInDateAsync()
        {
            return await _configService.GetAsync<DateTime>("LastCheckInDate");
        }

        private async Task SaveCheckInDateAsync(DateTime date)
        {
            await _configService.SaveAsync("LastCheckInDate", date);
        }

        private int CalculateConsecutiveDays(DateTime lastCheckIn, DateTime today)
        {
            var daysDifference = (today - lastCheckIn.Date).Days;
            if (daysDifference == 1)
            {
                return _configService.GetAsync<int>("ConsecutiveCheckInDays").Result;
            }
            return 0;
        }

        private async Task UpdateConsecutiveDaysAsync(int days)
        {
            await _configService.SaveAsync("ConsecutiveCheckInDays", days);
        }

        private async Task RecordPointsTransactionAsync(TransactionType type, int amount, string description, string source)
        {
            var transaction = new PointsTransaction
            {
                Type = type,
                Amount = amount,
                Description = description,
                Source = source,
                Timestamp = DateTime.Now
            };

            await _configService.AppendToListAsync("PointsHistory", transaction);
        }

        private async Task RecordCoinsTransactionAsync(TransactionType type, int amount, string description, string source)
        {
            var transaction = new CoinsTransaction
            {
                Type = type,
                Amount = amount,
                Description = description,
                Source = source,
                Timestamp = DateTime.Now
            };

            await _configService.AppendToListAsync("CoinsHistory", transaction);
        }

        private async Task UpdateActivityRecordAsync(RewardActivityType activityType, int pointsEarned, int coinsEarned)
        {
            var todayActivity = await GetTodayActivityAsync(activityType);
            todayActivity.Count++;
            todayActivity.PointsEarned += pointsEarned;
            todayActivity.CoinsEarned += coinsEarned;

            var today = DateTime.Today.ToString("yyyyMMdd");
            var key = $"Activity_{activityType}_{today}";
            await _configService.SaveAsync(key, todayActivity);
        }

        #endregion
    }
}
