using System;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 設定服務介面，負責設定的讀取、儲存和管理
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 載入設定檔案
        /// </summary>
        Task<AppSettings> LoadAsync();

        /// <summary>
        /// 儲存設定檔案
        /// </summary>
        Task SaveAsync(AppSettings settings);

        /// <summary>
        /// 取得指定設定值
        /// </summary>
        T GetSetting<T>(string key, T defaultValue = default);

        /// <summary>
        /// 設定指定設定值
        /// </summary>
        void SetSetting<T>(string key, T value);

        /// <summary>
        /// 非同步取得指定設定值
        /// </summary>
        Task<T> GetAsync<T>(string key, T defaultValue = default);

        /// <summary>
        /// 非同步設定指定設定值
        /// </summary>
        Task SaveAsync<T>(string key, T value);

        /// <summary>
        /// 非同步移除指定設定值
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// 非同步追加到列表設定
        /// </summary>
        Task AppendToListAsync<T>(string key, T item);

        /// <summary>
        /// 重設為預設設定
        /// </summary>
        Task ResetToDefaultAsync();

        /// <summary>
        /// 驗證設定檔案完整性
        /// </summary>
        bool ValidateSettings(AppSettings settings);
    }

    /// <summary>
    /// 應用程式設定模型
    /// </summary>
    public class AppSettings : INotifyPropertyChanged
    {
        private string _language = "zh-TW";
        private bool _autoStart = false;
        private bool _minimizeToTray = true;
        private double _windowOpacity = 0.95;
        private string _hotkey = "Ctrl+Shift+T";
        private bool _enableSoundEffect = true;
        private string _theme = "Dark";
        private bool _enableAutoUpdate = true;
        private int _maxRetries = 3;
        private int _timeout = 5000;

        /// <summary>
        /// 介面語言
        /// </summary>
        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                OnPropertyChanged(nameof(Language));
            }
        }

        /// <summary>
        /// 自動啟動
        /// </summary>
        public bool AutoStart
        {
            get => _autoStart;
            set
            {
                _autoStart = value;
                OnPropertyChanged(nameof(AutoStart));
            }
        }

        /// <summary>
        /// 最小化到系統匣
        /// </summary>
        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set
            {
                _minimizeToTray = value;
                OnPropertyChanged(nameof(MinimizeToTray));
            }
        }

        /// <summary>
        /// 視窗透明度 (0.1 - 1.0)
        /// </summary>
        public double WindowOpacity
        {
            get => _windowOpacity;
            set
            {
                _windowOpacity = Math.Max(0.1, Math.Min(1.0, value));
                OnPropertyChanged(nameof(WindowOpacity));
            }
        }

        /// <summary>
        /// 翻譯熱鍵
        /// </summary>
        public string Hotkey
        {
            get => _hotkey;
            set
            {
                _hotkey = value;
                OnPropertyChanged(nameof(Hotkey));
            }
        }

        /// <summary>
        /// 啟用音效
        /// </summary>
        public bool EnableSoundEffect
        {
            get => _enableSoundEffect;
            set
            {
                _enableSoundEffect = value;
                OnPropertyChanged(nameof(EnableSoundEffect));
            }
        }

        /// <summary>
        /// 主題 (Light/Dark)
        /// </summary>
        public string Theme
        {
            get => _theme;
            set
            {
                _theme = value;
                OnPropertyChanged(nameof(Theme));
            }
        }

        /// <summary>
        /// 啟用自動更新
        /// </summary>
        public bool EnableAutoUpdate
        {
            get => _enableAutoUpdate;
            set
            {
                _enableAutoUpdate = value;
                OnPropertyChanged(nameof(EnableAutoUpdate));
            }
        }

        /// <summary>
        /// 最大重試次數
        /// </summary>
        public int MaxRetries
        {
            get => _maxRetries;
            set
            {
                _maxRetries = Math.Max(1, Math.Min(10, value));
                OnPropertyChanged(nameof(MaxRetries));
            }
        }

        /// <summary>
        /// 連線逾時 (毫秒)
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set
            {
                _timeout = Math.Max(1000, Math.Min(30000, value));
                OnPropertyChanged(nameof(Timeout));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 創建預設設定
        /// </summary>
        public static AppSettings CreateDefault()
        {
            return new AppSettings();
        }

        /// <summary>
        /// 驗證設定值有效性
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Language) &&
                   !string.IsNullOrEmpty(Hotkey) &&
                   !string.IsNullOrEmpty(Theme) &&
                   WindowOpacity >= 0.1 && WindowOpacity <= 1.0 &&
                   MaxRetries >= 1 && MaxRetries <= 10 &&
                   Timeout >= 1000 && Timeout <= 30000;
        }
    }
}
