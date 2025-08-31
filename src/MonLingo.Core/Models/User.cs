using System;
using System.Collections.Generic;

namespace MonLingo.Core.Models
{
    public class User
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string Phone { get; set; }
        public AccountLevel AccountLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime RegisterTime { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime ExpireTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsPro { get; set; }
        public string Avatar { get; set; }
        public string SessionToken { get; set; }
        public int Coins { get; set; }
        public int Points { get; set; }
        public UserPreferences Preferences { get; set; }
        public UserStatistics Statistics { get; set; }
    }

    public enum AccountLevel
    {
        Free = 0,
        Basic = 1,
        Premium = 2,
        Enterprise = 3,
        Pro = 4,
        Expired = 5
    }

    public class UserPreferences
    {
        public string DefaultSourceLanguage { get; set; } = "auto";
        public string DefaultTargetLanguage { get; set; } = "zh-TW";
        public string PreferredTranslationEngine { get; set; } = "Google";
        public bool AutoDetectLanguage { get; set; } = true;
        public bool AutoCopyTranslation { get; set; } = false;
        public bool ShowTranslationHistory { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public string Theme { get; set; } = "Light";
        public double WindowOpacity { get; set; } = 1.0;
        public bool TopMost { get; set; } = false;
    }

    public class UserStatistics
    {
        public int TotalTranslations { get; set; }
        public int TotalCaptures { get; set; }
        public int TotalAudioTranslations { get; set; }
        public DateTime FirstUse { get; set; }
        public DateTime LastUse { get; set; }
        public TimeSpan TotalUsageTime { get; set; }
        public Dictionary<string, int> LanguagePairUsage { get; set; } = new();
        public Dictionary<string, int> EngineUsage { get; set; } = new();
    }
}
