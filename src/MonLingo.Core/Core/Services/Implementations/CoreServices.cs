using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonLingo.Core.Services;
using Newtonsoft.Json;

namespace MonLingo.Core.Services.Implementations
{
    /// <summary>
    /// 配置服務實現
    /// 使用JSON格式儲存設定到 %AppData%/MonLingo/config.json
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();
        private readonly string _configFilePath;
        private readonly object _lock = new object();

        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        public ConfigService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "MonLingo");
            
            // 確保配置目錄存在
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            
            _configFilePath = Path.Combine(configDir, "config.json");
            
            // 載入預設設定
            LoadDefaultSettings();
        }

        public T GetSetting<T>(string key, T defaultValue = default)
        {
            lock (_lock)
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is T directValue)
                            return directValue;
                        
                        // 嘗試轉換類型
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"設定值轉換失敗 {key}: {ex.Message}");
                        return defaultValue;
                    }
                }
                
                return defaultValue;
            }
        }

        public void SetSetting<T>(string key, T value)
        {
            lock (_lock)
            {
                var oldValue = _settings.TryGetValue(key, out var existing) ? existing : null;
                _settings[key] = value;
                
                // 觸發變更事件
                SettingChanged?.Invoke(this, new SettingChangedEventArgs(key, oldValue, value));
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                Dictionary<string, object> settingsToSave;
                
                lock (_lock)
                {
                    settingsToSave = new Dictionary<string, object>(_settings);
                }

                var json = JsonConvert.SerializeObject(settingsToSave, Formatting.Indented);
                var tempFilePath = _configFilePath + ".tmp";
                
                // 原子性寫入：先寫入臨時檔案，再重命名
                await Task.Run(() => File.WriteAllText(tempFilePath, json));
                
                // 備份現有檔案
                if (File.Exists(_configFilePath))
                {
                    var backupPath = _configFilePath + ".bak";
                    File.Copy(_configFilePath, backupPath, true);
                }
                
                // 移動臨時檔案到目標位置
                File.Move(tempFilePath, _configFilePath);
                
                System.Diagnostics.Debug.WriteLine($"設定已儲存到: {_configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"儲存設定失敗: {ex.Message}");
                throw;
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("設定檔案不存在，使用預設設定");
                    return;
                }

                var json = await Task.Run(() => File.ReadAllText(_configFilePath));
                var loadedSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                lock (_lock)
                {
                    _settings.Clear();
                    if (loadedSettings != null)
                    {
                        foreach (var kvp in loadedSettings)
                        {
                            _settings[kvp.Key] = kvp.Value;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"設定已從 {_configFilePath} 載入");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入設定失敗: {ex.Message}");
                // 載入失敗時保持預設設定
            }
        }

        private void LoadDefaultSettings()
        {
            // 載入預設設定
            _settings["Language"] = "zh-TW";
            _settings["Theme"] = "Light";
            _settings["AutoStart"] = false;
            _settings["HotKey_Translate"] = "Ctrl+Q";
            _settings["HotKey_Audio"] = "Ctrl+Shift+Q";
            _settings["TranslationEngine"] = "Google";
            _settings["OCR_Language"] = "Auto";
            _settings["Window_MainBar_X"] = 100.0;
            _settings["Window_MainBar_Y"] = 50.0;
            _settings["Translation_ShowOriginal"] = true;
            _settings["Translation_AutoCopy"] = false;
            _settings["Audio_InputDevice"] = "Default";
            _settings["Audio_SampleRate"] = 16000;
            _settings["History_MaxItems"] = 1000;
            _settings["History_AutoSave"] = true;
        }
    }

    /// <summary>
    /// 熱鍵服務實現
    /// 使用Win32 API註冊全域熱鍵
    /// </summary>
    public class HotKeyService : IHotKeyService, IDisposable
    {
        private readonly Dictionary<int, HotKeyInfo> _registeredHotKeys = new Dictionary<int, HotKeyInfo>();
        private int _nextHotKeyId = 1;
        private bool _isDisposed = false;

        public event EventHandler<HotKeyPressedEventArgs> HotKeyPressed;

        public bool RegisterHotKey(HotKeyInfo hotKey)
        {
            try
            {
                hotKey.Id = _nextHotKeyId++;
                
                // 轉換修飾鍵
                int modifiers = 0;
                if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                    modifiers |= 0x0001; // MOD_ALT
                if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                    modifiers |= 0x0002; // MOD_CONTROL
                if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                    modifiers |= 0x0004; // MOD_SHIFT
                if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
                    modifiers |= 0x0008; // MOD_WIN

                // 使用Native橋接器註冊熱鍵
                bool success = MonLingo.Core.Interop.NativeBridge.register_global_hotkey(
                    modifiers, 
                    (int)System.Windows.Input.KeyInterop.VirtualKeyFromKey(hotKey.Key));

                if (success)
                {
                    _registeredHotKeys[hotKey.Id] = hotKey;
                    System.Diagnostics.Debug.WriteLine($"熱鍵註冊成功: {hotKey.Description}");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"熱鍵註冊失敗: {hotKey.Description}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"熱鍵註冊異常: {ex.Message}");
                return false;
            }
        }

        public bool UnregisterHotKey(HotKeyInfo hotKey)
        {
            try
            {
                if (_registeredHotKeys.ContainsKey(hotKey.Id))
                {
                    int modifiers = 0;
                    if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                        modifiers |= 0x0001;
                    if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                        modifiers |= 0x0002;
                    if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                        modifiers |= 0x0004;
                    if (hotKey.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
                        modifiers |= 0x0008;

                    bool success = MonLingo.Core.Interop.NativeBridge.unregister_global_hotkey(
                        modifiers, 
                        (int)System.Windows.Input.KeyInterop.VirtualKeyFromKey(hotKey.Key));

                    if (success)
                    {
                        _registeredHotKeys.Remove(hotKey.Id);
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消熱鍵註冊異常: {ex.Message}");
                return false;
            }
        }

        public void UnregisterAll()
        {
            try
            {
                MonLingo.Core.Interop.NativeBridge.unregister_all_hotkeys();
                _registeredHotKeys.Clear();
                System.Diagnostics.Debug.WriteLine("所有熱鍵已取消註冊");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消所有熱鍵註冊異常: {ex.Message}");
            }
        }

        // 此方法應該由Native回呼調用
        internal void OnHotKeyPressed(int hotKeyId)
        {
            if (_registeredHotKeys.TryGetValue(hotKeyId, out var hotKey))
            {
                try
                {
                    // 在UI執行緒上執行
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        hotKey.Action?.Invoke();
                        HotKeyPressed?.Invoke(this, new HotKeyPressedEventArgs(hotKey));
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"熱鍵處理異常: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                UnregisterAll();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// 翻譯服務實現
    /// 模擬翻譯引擎，實際實現將整合真實的翻譯API
    /// </summary>
    public class TranslateService : ITranslateService
    {
        private string _currentEngine = "Google";
        private readonly string[] _availableEngines = { "Google", "DeepL", "Microsoft", "Offline" };

        public async Task<TranslationResult> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "zh-TW")
        {
            var startTime = DateTime.Now;
            
            try
            {
                // 模擬翻譯延遲
                await Task.Delay(800);
                
                var result = new TranslationResult
                {
                    OriginalText = text,
                    SourceLanguage = sourceLang == "auto" ? await DetectLanguageAsync(text) : sourceLang,
                    TargetLanguage = targetLang,
                    Engine = _currentEngine,
                    ProcessingTime = DateTime.Now - startTime,
                    IsSuccess = true,
                    Confidence = 0.95
                };

                // 模擬翻譯結果
                result.TranslatedText = SimulateTranslation(text, result.SourceLanguage, targetLang);
                
                return result;
            }
            catch (Exception ex)
            {
                return new TranslationResult
                {
                    OriginalText = text,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ProcessingTime = DateTime.Now - startTime
                };
            }
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            // 模擬語言檢測
            await Task.Delay(100);
            
            // 簡單的語言檢測邏輯
            if (text.Any(c => c >= 0x4e00 && c <= 0x9fff)) // 中文字符範圍
                return "zh";
            if (text.All(c => c <= 127)) // ASCII字符
                return "en";
            
            return "auto";
        }

        public string[] GetAvailableEngines()
        {
            return _availableEngines.Clone() as string[];
        }

        public void SetCurrentEngine(string engineName)
        {
            if (_availableEngines.Contains(engineName))
            {
                _currentEngine = engineName;
            }
        }

        private string SimulateTranslation(string text, string sourceLang, string targetLang)
        {
            // 模擬翻譯邏輯
            var translations = new Dictionary<string, string>
            {
                ["Hello World"] = "你好世界",
                ["Good morning"] = "早安",
                ["Thank you"] = "謝謝",
                ["How are you?"] = "你好嗎？",
                ["I love you"] = "我愛你"
            };

            if (translations.TryGetValue(text, out var translation))
                return translation;
            
            return $"[{_currentEngine}翻譯] {text}";
        }
    }

    // 空實作：IConfigurationService
    public class ConfigurationService : IConfigurationService
    {
        public Task LoadConfigurationAsync() => LoadAsync();
        public T GetValue<T>(string key, T defaultValue = default) => defaultValue;
        public void SetValue<T>(string key, T value) { }
        public Task SaveAsync() => Task.CompletedTask;
        public Task LoadAsync() => Task.CompletedTask;
        public bool HasValue(string key) => false;
        public void RemoveValue(string key) { }
    }

    // 空實作：ILoggingService
    public class LoggingService : ILoggingService
    {
        public Task InitializeAsync() => Task.CompletedTask;
        public void LogInfo(string message, params object[] args) { }
        public void LogWarning(string message, params object[] args) { }
        public void LogError(string message, Exception exception = null, params object[] args) { }
        public void LogDebug(string message, params object[] args) { }
        public void LogTrace(string message, params object[] args) { }
    }

    // 空實作：ILanguageDetectionService
    public class LanguageDetectionService : ILanguageDetectionService
    {
        public Task<string> DetectLanguageAsync(string text) => Task.FromResult("auto");
        public Task<LanguageDetectionResult> DetectLanguageWithConfidenceAsync(string text) => Task.FromResult(new LanguageDetectionResult { Language = "auto", Confidence = 1.0, IsReliable = true, AlternativeLanguages = new string[0] });
        public string[] GetSupportedLanguages() => new[] { "en", "zh-TW" };
        public bool IsLanguageSupported(string languageCode) => true;
    }

    // 空實作：ITranslationService
    public class TranslationService : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "zh-TW") => Task.FromResult(new TranslationResult { OriginalText = text, TranslatedText = text, SourceLanguage = sourceLang, TargetLanguage = targetLang, IsSuccess = true });
        public Task<TranslationResult[]> TranslateBatchAsync(string[] texts, string sourceLang = "auto", string targetLang = "zh-TW") => Task.FromResult(new[] { new TranslationResult { OriginalText = texts.Length > 0 ? texts[0] : "", TranslatedText = texts.Length > 0 ? texts[0] : "", SourceLanguage = sourceLang, TargetLanguage = targetLang, IsSuccess = true } });
        public string[] GetAvailableEngines() => new[] { "Google", "DeepL" };
        public void SetEngine(string engineName) { }
        public string GetCurrentEngine() => "Google";
    }

    // 空實作：IDictionaryService
    public class DictionaryService : IDictionaryService
    {
        public Task<DictionaryResult> LookupAsync(string word, string language = "en") => Task.FromResult(new DictionaryResult { Word = word, Language = language, Entries = new DictionaryEntry[0], Pronunciations = new string[0], IsFound = false });
        public Task<string[]> GetSuggestionsAsync(string partialWord, string language = "en") => Task.FromResult(new string[0]);
        public Task<bool> CheckSpellingAsync(string word, string language = "en") => Task.FromResult(true);
        public string[] GetSupportedLanguages() => new[] { "en", "zh-TW" };
    }

    // 空實作：IProgressService
    public class ProgressService : IProgressService
    {
        public void ShowProgress(string title, string message, bool isIndeterminate = false) { }
        public void UpdateProgress(double percentage, string message = null) { }
        public void HideProgress() { }
        public bool IsProgressVisible => false;
        public event EventHandler<ProgressEventArgs> ProgressChanged;
    }

    // 空實作：IDataService
    public class DataService : IDataService
    {
        public Task InitializeAsync() => Task.CompletedTask;
        public Task SaveTranslationHistoryAsync(TranslationHistory item) => Task.CompletedTask;
        public Task<TranslationHistory[]> GetTranslationHistoryAsync(int limit = 100) => Task.FromResult(new TranslationHistory[0]);
        public Task ClearHistoryAsync() => Task.CompletedTask;
        public Task BackupDataAsync(string filePath) => Task.CompletedTask;
        public Task RestoreDataAsync(string filePath) => Task.CompletedTask;
    }

    // 空實作：IUserService
    public class UserService : IUserService
    {
        public Task<UserPreferences> GetUserPreferencesAsync() => Task.FromResult(new UserPreferences());
        public Task SaveUserPreferencesAsync(UserPreferences preferences) => Task.CompletedTask;
        public Task<UserStatistics> GetUserStatisticsAsync() => Task.FromResult(new UserStatistics());
        public Task UpdateUsageStatisticsAsync(string action, object data = null) => Task.CompletedTask;
    }

    // 空實作：ISettingsService
    public class SettingsService : ISettingsService
    {
        public Task<AppSettings> GetAppSettingsAsync() => Task.FromResult(new AppSettings());
        public Task SaveAppSettingsAsync(AppSettings settings) => Task.CompletedTask;
        public Task ResetToDefaultAsync() => Task.CompletedTask;
        public Task ImportSettingsAsync(string filePath) => Task.CompletedTask;
        public Task ExportSettingsAsync(string filePath) => Task.CompletedTask;
        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;
    }
}
