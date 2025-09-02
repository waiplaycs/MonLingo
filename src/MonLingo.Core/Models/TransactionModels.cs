using System;
using System.Collections.Generic;

namespace MonLingo.Core.Models
{
    public enum TransactionType
    {
        Add = 1,
        Deduct = 2,
        Purchase = 3,
        Reward = 4,
        Refund = 5,
        Consume = 6
    }

    public class CoinsTransaction
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public TransactionType Type { get; set; }
        public int Amount { get; set; }
        public int Balance { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Timestamp { get; set; }
        public string Reference { get; set; }
        public string Source { get; set; }
    // 允許為空，對應 CoinsService 的可選參數
    public decimal? CostUSD { get; set; }
    }

    public class PointsTransaction
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public TransactionType Type { get; set; }
        public int Amount { get; set; }
        public int Balance { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Timestamp { get; set; }
        public string Reference { get; set; }
        public string Source { get; set; }
        public RewardActivityType? ActivityType { get; set; }
    }

    public enum RewardActivityType
    {
        DailyLogin = 1,
        Translation = 2,
        ScreenCapture = 3,
        AudioTranslation = 4,
        ShareApp = 5,
        RateApp = 6,
        InviteFriend = 7,
        CompleteTutorial = 8,
        DailyCheckIn = 9,
        WatchAd = 10
    }

    public class RewardActivity
    {
        public RewardActivityType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Points { get; set; }
        public int PointsReward { get; set; }
        public int CoinsReward { get; set; }
        public int? MaxDailyRewards { get; set; }
        public int DailyLimit { get; set; }
        public bool IsActive { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class UserActivityRecord
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public RewardActivityType ActivityType { get; set; }
        public DateTime CompletedAt { get; set; }
        public DateTime Date { get; set; }
        public int RewardPoints { get; set; }
        public int PointsEarned { get; set; }
        public int CoinsEarned { get; set; }
        public int Count { get; set; }
        public bool IsRewarded { get; set; }
    }

    public static class TranslationCost
    {
        // 舊版相容：依引擎與長度計算硬幣消耗
        public static int CalculateCoins(TranslationEngine engine, int textLength)
        {
            // 粗略成本模型：不同引擎基礎係數 * 長度分段
            int baseCost = engine switch
            {
                TranslationEngine.Google => 0, // Google 使用積分
                TranslationEngine.Bing => 0,
                TranslationEngine.Baidu => 0,
                TranslationEngine.DeepL => 2,
                TranslationEngine.OpenAI => 5,
                TranslationEngine.Azure => 2,
                TranslationEngine.Local => 0, // 本地翻譯免費
                _ => 1
            };

            if (baseCost == 0) return 0;

            // 以每 1000 字符為一段的分段費率
            int segments = Math.Max(1, (int)Math.Ceiling(textLength / 1000.0));
            return baseCost * segments;
        }

        // 舊版相容：Google 等基礎引擎使用積分
        public static int CalculatePoints(int textLength)
        {
            // 每 500 字元 1 點
            return Math.Max(1, (int)Math.Ceiling(textLength / 500.0));
        }

        // 新版簡化介面仍可提供（供其他地方使用）
        public static int GetCost(TranslationEngine engine)
        {
            return engine switch
            {
                TranslationEngine.Google => 1,
                TranslationEngine.Bing => 1,
                TranslationEngine.Baidu => 1,
                TranslationEngine.DeepL => 2,
                TranslationEngine.OpenAI => 5,
                TranslationEngine.Azure => 2,
                _ => 1
            };
        }
    }
}
