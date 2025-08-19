using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 設定服務實現，負責設定的讀取、儲存和管理
    /// 依據 PRD §6 規範實現 JSON 序列化、原子寫入、備份機制
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly string _backupFilePath;
        private readonly JsonSerializerSettings _jsonSettings;
        private AppSettings _currentSettings;

        public ConfigService()
        {
            // 使用 %AppData%\MonLingo 作為設定目錄
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonLingo");
            _configFilePath = Path.Combine(_configDirectory, "config.json");
            _backupFilePath = Path.Combine(_configDirectory, "config.backup.json");

            // 設定 JSON 序列化選項
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            // 確保設定目錄存在
            EnsureConfigDirectory();
        }

        /// <summary>
        /// 載入設定檔案
        /// </summary>
        public async Task<AppSettings> LoadAsync()
        {
            try
            {
                // 如果設定檔不存在，載入備份檔或建立預設設定
                if (!File.Exists(_configFilePath))
                {
                    return await LoadFromBackupOrDefault();
                }

                var json = await Task.Run(() => File.ReadAllText(_configFilePath));
                var settings = JsonConvert.DeserializeObject<AppSettings>(json, _jsonSettings);

                // 驗證設定完整性
                if (settings == null || !ValidateSettings(settings))
                {
                    // 設定無效，嘗試載入備份
                    return await LoadFromBackupOrDefault();
                }

                _currentSettings = settings;
                return settings;
            }
            catch (Exception ex)
            {
                // 載入失敗，記錄錯誤並嘗試載入備份
                System.Diagnostics.Debug.WriteLine($"載入設定失敗: {ex.Message}");
                return await LoadFromBackupOrDefault();
            }
        }

        /// <summary>
        /// 儲存設定檔案 (原子寫入)
        /// </summary>
        public async Task SaveAsync(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // 驗證設定有效性
            if (!ValidateSettings(settings))
                throw new ArgumentException("設定驗證失敗", nameof(settings));

            try
            {
                // 先備份現有設定
                await CreateBackup();

                // 使用暫存檔案實現原子寫入
                var tempFilePath = _configFilePath + ".tmp";
                var json = JsonConvert.SerializeObject(settings, _jsonSettings);

                // 寫入暫存檔案
                await Task.Run(() => File.WriteAllText(tempFilePath, json));

                // 原子操作：移動暫存檔案覆蓋原設定檔
                if (File.Exists(_configFilePath))
                {
                    File.Delete(_configFilePath);
                }
                File.Move(tempFilePath, _configFilePath);

                _currentSettings = settings;
            }
            catch (Exception ex)
            {
                // 清理暫存檔案
                var tempFilePath = _configFilePath + ".tmp";
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }

                throw new InvalidOperationException($"儲存設定失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 取得指定設定值
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (_currentSettings == null)
                return defaultValue;

            try
            {
                var property = typeof(AppSettings).GetProperty(key);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(_currentSettings);
                    if (value is T typedValue)
                        return typedValue;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取得設定值失敗 [{key}]: {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// 設定指定設定值
        /// </summary>
        public void SetSetting<T>(string key, T value)
        {
            if (_currentSettings == null)
                _currentSettings = AppSettings.CreateDefault();

            try
            {
                var property = typeof(AppSettings).GetProperty(key);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(_currentSettings, value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定設定值失敗 [{key}]: {ex.Message}");
            }
        }

        /// <summary>
        /// 重設為預設設定
        /// </summary>
        public async Task ResetToDefaultAsync()
        {
            var defaultSettings = AppSettings.CreateDefault();
            await SaveAsync(defaultSettings);
        }

        /// <summary>
        /// 驗證設定檔案完整性
        /// </summary>
        public bool ValidateSettings(AppSettings settings)
        {
            if (settings == null)
                return false;

            try
            {
                return settings.IsValid();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 確保設定目錄存在
        /// </summary>
        private void EnsureConfigDirectory()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"無法建立設定目錄: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 建立設定備份
        /// </summary>
        private async Task CreateBackup()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var content = await Task.Run(() => File.ReadAllText(_configFilePath));
                    await Task.Run(() => File.WriteAllText(_backupFilePath, content));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"建立備份失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 從備份載入或建立預設設定
        /// </summary>
        private async Task<AppSettings> LoadFromBackupOrDefault()
        {
            try
            {
                // 嘗試從備份載入
                if (File.Exists(_backupFilePath))
                {
                    var backupJson = await Task.Run(() => File.ReadAllText(_backupFilePath));
                    var backupSettings = JsonConvert.DeserializeObject<AppSettings>(backupJson, _jsonSettings);

                    if (backupSettings != null && ValidateSettings(backupSettings))
                    {
                        _currentSettings = backupSettings;
                        // 恢復主設定檔
                        await SaveAsync(backupSettings);
                        return backupSettings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入備份設定失敗: {ex.Message}");
            }

            // 建立並儲存預設設定
            var defaultSettings = AppSettings.CreateDefault();
            _currentSettings = defaultSettings;
            
            try
            {
                await SaveAsync(defaultSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"儲存預設設定失敗: {ex.Message}");
            }

            return defaultSettings;
        }
    }
}
