using System.Collections.Generic;

namespace MonLingo.Core.Models
{
    public class LanguageConfig
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string NativeName { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
    }

    public class LanguageGroup
    {
        public string Name { get; set; }
        public List<LanguageConfig> Languages { get; set; } = new();
    }
}
