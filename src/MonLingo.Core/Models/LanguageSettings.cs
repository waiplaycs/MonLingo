using System.Collections.Generic;
using System.Linq;

namespace MonLingo.Core.Models
{
    public class LanguageSettings
    {
        public string SourceLanguage { get; set; } = "auto";
        public string TargetLanguage { get; set; } = "zh-TW";
        public bool AutoDetectLanguage { get; set; } = true;
        public List<LanguageConfig> SupportedLanguages { get; set; } = new();
        public List<LanguageConfig> RecentLanguages { get; set; } = new();
        public List<LanguageConfig> FavoriteLanguages { get; set; } = new();

        // 舊版相容：提供靜態字典與查詢輔助
        public static readonly Dictionary<string, string> SourceLanguages = new Dictionary<string, string>
        {
            {"AUTO", "auto"},
            {"EN", "en"},
            {"中文", "zh-cn"},
            {"繁體", "zh-tw"},
            {"日語", "ja"},
            {"韓語", "ko"},
            {"FR", "fr"},
            {"DE", "de"},
            {"ES", "es"},
            {"RU", "ru"}
        };

        public static readonly Dictionary<string, string> TargetLanguages = new Dictionary<string, string>
        {
            {"中文", "zh-cn"},
            {"繁體", "zh-tw"},
            {"EN", "en"},
            {"日語", "ja"},
            {"韓語", "ko"},
            {"FR", "fr"},
            {"DE", "de"},
            {"ES", "es"},
            {"RU", "ru"}
        };

        public static string GetLanguageCodeByDisplayName(string displayName, bool isSource)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return null;
            var map = isSource ? SourceLanguages : TargetLanguages;
            if (map.TryGetValue(displayName, out var code)) return code;
            // 嘗試忽略大小寫匹配鍵
            var kv = map.FirstOrDefault(kv => string.Equals(kv.Key, displayName, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(kv.Key)) return kv.Value;
            // 若直接傳入代碼則原樣返回
            return displayName.ToLowerInvariant();
        }

        public static string GetDisplayNameByLanguageCode(string code, bool isSource)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            code = code.ToLowerInvariant();
            var map = isSource ? SourceLanguages : TargetLanguages;
            var kv = map.FirstOrDefault(kv => kv.Value.Equals(code, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(kv.Key)) return kv.Key;
            // 常見別名
            if (code.StartsWith("zh")) return isSource ? "中文" : "中文";
            if (code == "auto") return "AUTO";
            return code.ToUpperInvariant();
        }
    }
}
