using System;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 語言配置服務介面
    /// 負責管理 OCR 和翻譯的語言設定，確保設定持久化和同步
    /// 支援語言記憶功能，自動記住用戶的輸入輸出語言偏好
    /// </summary>
    public interface ILanguageConfigService
    {
        /// <summary>
        /// 獲取當前源語言代碼
        /// </summary>
        Task<string> GetSourceLanguageAsync();

        /// <summary>
        /// 獲取當前目標語言代碼
        /// </summary>
        Task<string> GetTargetLanguageAsync();

        /// <summary>
        /// 設定源語言並記住用戶偏好
        /// </summary>
        Task SetSourceLanguageAsync(string languageCode);

        /// <summary>
        /// 設定目標語言並記住用戶偏好
        /// </summary>
        Task SetTargetLanguageAsync(string languageCode);

        /// <summary>
        /// 獲取完整的語言配置
        /// </summary>
        Task<LanguageConfig> GetLanguageConfigAsync();

        /// <summary>
        /// 更新完整的語言配置
        /// </summary>
        Task UpdateLanguageConfigAsync(LanguageConfig config);

        /// <summary>
        /// 重置為預設語言設定
        /// </summary>
        Task ResetToDefaultAsync();

        /// <summary>
        /// 語言配置變更事件
        /// </summary>
        event EventHandler LanguageConfigChanged;
    }

    /// <summary>
    /// 語言配置服務實現
    /// 提供語言記憶功能，確保 OCR 和翻譯層記住用戶的語言偏好
    /// </summary>
    public class LanguageConfigService : ILanguageConfigService
    {
        private readonly IConfigService _configService;
        private LanguageConfig _currentConfig;
        private readonly object _lock = new object();

        public event EventHandler LanguageConfigChanged;

        public LanguageConfigService(IConfigService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _ = InitializeAsync(); // 異步初始化但不等待
        }

        /// <summary>
        /// 異步初始化語言配置
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                var config = await GetLanguageConfigAsync();
                lock (_lock)
                {
                    _currentConfig = config;
                }
            }
            catch (Exception ex)
            {
                // 初始化失敗時使用預設配置
                System.Diagnostics.Debug.WriteLine($"[LanguageConfigService] 初始化失敗，使用預設配置: {ex.Message}");
                lock (_lock)
                {
                    _currentConfig = new LanguageConfig
                    {
                        SourceLanguage = "auto",
                        TargetLanguage = "zh-tw",
                        AutoDetectLanguage = true,
                        RememberLanguagePreference = true,
                        LastUpdated = DateTime.Now
                    };
                }
            }
        }

        /// <summary>
        /// 獲取當前源語言代碼
        /// </summary>
        public async Task<string> GetSourceLanguageAsync()
        {
            await EnsureInitializedAsync();
            
            lock (_lock)
            {
                return _currentConfig?.SourceLanguage ?? "auto";
            }
        }

        /// <summary>
        /// 獲取當前目標語言代碼
        /// </summary>
        public async Task<string> GetTargetLanguageAsync()
        {
            await EnsureInitializedAsync();
            
            lock (_lock)
            {
                return _currentConfig?.TargetLanguage ?? "zh-tw";
            }
        }

        /// <summary>
        /// 設定源語言並記住用戶偏好
        /// </summary>
        public async Task SetSourceLanguageAsync(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new ArgumentException("語言代碼不能為空", nameof(languageCode));

            await EnsureInitializedAsync();
            
            LanguageConfig oldConfig;
            lock (_lock)
            {
                if (_currentConfig?.SourceLanguage == languageCode)
                    return; // 沒有變更

                oldConfig = _currentConfig != null ? new LanguageConfig
                {
                    SourceLanguage = _currentConfig.SourceLanguage,
                    TargetLanguage = _currentConfig.TargetLanguage,
                    AutoDetectLanguage = _currentConfig.AutoDetectLanguage,
                    RememberLanguagePreference = _currentConfig.RememberLanguagePreference,
                    LastUpdated = _currentConfig.LastUpdated
                } : new LanguageConfig();

                if (_currentConfig == null)
                    _currentConfig = new LanguageConfig();

                _currentConfig.SourceLanguage = languageCode;
                _currentConfig.LastUpdated = DateTime.Now;
            }

            // 持久化設定
            _configService.SetSetting("SourceLanguage", languageCode);

            // 觸發變更事件
            OnLanguageConfigChanged(oldConfig, _currentConfig, nameof(LanguageConfig.SourceLanguage));
        }

        /// <summary>
        /// 設定目標語言並記住用戶偏好
        /// </summary>
        public async Task SetTargetLanguageAsync(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new ArgumentException("語言代碼不能為空", nameof(languageCode));

            await EnsureInitializedAsync();
            
            LanguageConfig oldConfig;
            lock (_lock)
            {
                if (_currentConfig?.TargetLanguage == languageCode)
                    return; // 沒有變更

                oldConfig = _currentConfig != null ? new LanguageConfig
                {
                    SourceLanguage = _currentConfig.SourceLanguage,
                    TargetLanguage = _currentConfig.TargetLanguage,
                    AutoDetectLanguage = _currentConfig.AutoDetectLanguage,
                    RememberLanguagePreference = _currentConfig.RememberLanguagePreference,
                    LastUpdated = _currentConfig.LastUpdated
                } : new LanguageConfig();

                if (_currentConfig == null)
                    _currentConfig = new LanguageConfig();

                _currentConfig.TargetLanguage = languageCode;
                _currentConfig.LastUpdated = DateTime.Now;
            }

            // 持久化設定
            _configService.SetSetting("TargetLanguage", languageCode);

            // 觸發變更事件
            OnLanguageConfigChanged(oldConfig, _currentConfig, nameof(LanguageConfig.TargetLanguage));
        }

        /// <summary>
        /// 獲取完整的語言配置
        /// </summary>
        public async Task<LanguageConfig> GetLanguageConfigAsync()
        {
            await Task.CompletedTask; // 異步佔位符
            
            // 從配置服務載入設定
            var sourceLanguage = _configService.GetSetting("SourceLanguage", "auto");
            var targetLanguage = _configService.GetSetting("TargetLanguage", "zh-tw");
            var autoDetect = _configService.GetSetting("AutoDetectLanguage", true);
            var rememberPreference = _configService.GetSetting("RememberLanguagePreference", true);

            return new LanguageConfig
            {
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                AutoDetectLanguage = autoDetect,
                RememberLanguagePreference = rememberPreference,
                LastUpdated = DateTime.Now
            };
        }

        /// <summary>
        /// 更新完整的語言配置
        /// </summary>
        public async Task UpdateLanguageConfigAsync(LanguageConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            await EnsureInitializedAsync();
            
            LanguageConfig oldConfig;
            lock (_lock)
            {
                oldConfig = _currentConfig != null ? new LanguageConfig
                {
                    SourceLanguage = _currentConfig.SourceLanguage,
                    TargetLanguage = _currentConfig.TargetLanguage,
                    AutoDetectLanguage = _currentConfig.AutoDetectLanguage,
                    RememberLanguagePreference = _currentConfig.RememberLanguagePreference,
                    LastUpdated = _currentConfig.LastUpdated
                } : new LanguageConfig();

                _currentConfig = new LanguageConfig
                {
                    SourceLanguage = config.SourceLanguage,
                    TargetLanguage = config.TargetLanguage,
                    AutoDetectLanguage = config.AutoDetectLanguage,
                    RememberLanguagePreference = config.RememberLanguagePreference,
                    LastUpdated = DateTime.Now
                };
            }

            // 持久化所有配置
            _configService.SetSetting("SourceLanguage", config.SourceLanguage);
            _configService.SetSetting("TargetLanguage", config.TargetLanguage);
            _configService.SetSetting("AutoDetectLanguage", config.AutoDetectLanguage);
            _configService.SetSetting("RememberLanguagePreference", config.RememberLanguagePreference);

            // 觸發變更事件
            OnLanguageConfigChanged(oldConfig, _currentConfig, "FullConfig");
        }

        /// <summary>
        /// 重置為預設語言設定
        /// </summary>
        public async Task ResetToDefaultAsync()
        {
            var defaultConfig = new LanguageConfig
            {
                SourceLanguage = "auto",
                TargetLanguage = "zh-tw",
                AutoDetectLanguage = true,
                RememberLanguagePreference = true,
                LastUpdated = DateTime.Now
            };

            await UpdateLanguageConfigAsync(defaultConfig);
        }

        /// <summary>
        /// 確保服務已初始化
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_currentConfig == null)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// 觸發語言配置變更事件
        /// </summary>
        private void OnLanguageConfigChanged(LanguageConfig oldConfig, LanguageConfig newConfig, string changedProperty)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LanguageConfigService] 語言配置已變更: {changedProperty}");
                System.Diagnostics.Debug.WriteLine($"  源語言: {oldConfig?.SourceLanguage} -> {newConfig?.SourceLanguage}");
                System.Diagnostics.Debug.WriteLine($"  目標語言: {oldConfig?.TargetLanguage} -> {newConfig?.TargetLanguage}");
                
                LanguageConfigChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LanguageConfigService] 觸發變更事件時發生錯誤: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 語言配置模型
    /// </summary>
    public class LanguageConfig
    {
        /// <summary>
        /// 源語言代碼 (OCR 識別語言)
        /// </summary>
        public string SourceLanguage { get; set; } = "auto";

        /// <summary>
        /// 目標語言代碼 (翻譯輸出語言)
        /// </summary>
        public string TargetLanguage { get; set; } = "zh-tw";

        /// <summary>
        /// 是否自動檢測語言
        /// </summary>
        public bool AutoDetectLanguage { get; set; } = true;

        /// <summary>
        /// 是否記住語言偏好
        /// </summary>
        public bool RememberLanguagePreference { get; set; } = true;

        /// <summary>
        /// 最後更新時間
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
