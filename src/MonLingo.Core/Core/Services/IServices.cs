using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MonLingo.Core.Services
{
    /// <summary>
    /// 配置服務介面
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 獲取設定值
        /// </summary>
        T GetSetting<T>(string key, T defaultValue = default);
        
        /// <summary>
        /// 設置設定值
        /// </summary>
        void SetSetting<T>(string key, T value);
        
        /// <summary>
        /// 儲存設定到檔案
        /// </summary>
        Task SaveAsync();
        
        /// <summary>
        /// 從檔案載入設定
        /// </summary>
        Task LoadAsync();
        
        /// <summary>
        /// 設定變更事件
        /// </summary>
        event EventHandler<SettingChangedEventArgs> SettingChanged;
    }

    /// <summary>
    /// 熱鍵服務介面
    /// </summary>
    public interface IHotKeyService : IDisposable
    {
        /// <summary>
        /// 註冊熱鍵
        /// </summary>
        bool RegisterHotKey(HotKeyInfo hotKey);
        
        /// <summary>
        /// 取消註冊熱鍵
        /// </summary>
        bool UnregisterHotKey(HotKeyInfo hotKey);
        
        /// <summary>
        /// 取消所有熱鍵
        /// </summary>
        void UnregisterAll();
        
        /// <summary>
        /// 熱鍵按下事件
        /// </summary>
        event EventHandler<HotKeyPressedEventArgs> HotKeyPressed;
    }

    /// <summary>
    /// 翻譯服務介面
    /// </summary>
    public interface ITranslateService
    {
        /// <summary>
        /// 翻譯文字
        /// </summary>
        Task<TranslationResult> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "zh-TW");
        
        /// <summary>
        /// 檢測語言
        /// </summary>
        Task<string> DetectLanguageAsync(string text);
        
        /// <summary>
        /// 獲取可用的翻譯引擎
        /// </summary>
        string[] GetAvailableEngines();
        
        /// <summary>
        /// 設置當前翻譯引擎
        /// </summary>
        void SetCurrentEngine(string engineName);
    }

    /// <summary>
    /// 螢幕擷取服務介面
    /// </summary>
    public interface ICaptureService
    {
        /// <summary>
        /// 開始區域選擇
        /// </summary>
        Task<CaptureResult> StartRegionCaptureAsync();
        
        /// <summary>
        /// 擷取指定區域
        /// </summary>
        Task<CaptureResult> CaptureRegionAsync(System.Windows.Rect region);
        
        /// <summary>
        /// 擷取螢幕指定區域 (使用 System.Drawing.Rectangle)
        /// </summary>
        Task<CaptureResult> CaptureScreenRegionAsync(System.Drawing.Rectangle region);
        
        /// <summary>
        /// 擷取全螢幕
        /// </summary>
        Task<CaptureResult> CaptureFullScreenAsync();
        
        /// <summary>
        /// 擷取完成事件
        /// </summary>
        event EventHandler<CaptureCompletedEventArgs> CaptureCompleted;
    }

    /// <summary>
    /// 音訊服務介面
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// 開始音訊擷取
        /// </summary>
        Task StartCaptureAsync();
        
        /// <summary>
        /// 開始錄音
        /// </summary>
        Task<AudioResult> StartRecordingAsync();
        
        /// <summary>
        /// 停止錄音
        /// </summary>
        Task<AudioResult> StopRecordingAsync();
        
        /// <summary>
        /// 停止音訊擷取
        /// </summary>
        void StopCapture();
        
        /// <summary>
        /// 播放音訊
        /// </summary>
        Task PlayAudioAsync(byte[] audioData);
        
        /// <summary>
        /// 是否正在擷取
        /// </summary>
        bool IsCapturing { get; }
        
        /// <summary>
        /// 是否正在錄音
        /// </summary>
        bool IsRecording { get; }
        
        /// <summary>
        /// 音訊資料事件
        /// </summary>
        event EventHandler<AudioDataEventArgs> AudioDataReceived;
    }

    /// <summary>
    /// 配置服務介面 (基礎設定管理)
    /// </summary>
    public interface IConfigurationService
    {
        Task LoadConfigurationAsync();
        /// <summary>
        /// 獲取設定值
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default);
        
        /// <summary>
        /// 設置設定值
        /// </summary>
        void SetValue<T>(string key, T value);
        
        /// <summary>
        /// 儲存設定
        /// </summary>
        Task SaveAsync();
        
        /// <summary>
        /// 載入設定
        /// </summary>
        Task LoadAsync();
        
        /// <summary>
        /// 設定是否存在
        /// </summary>
        bool HasValue(string key);
        
        /// <summary>
        /// 移除設定
        /// </summary>
        void RemoveValue(string key);
    }

    /// <summary>
    /// 日誌服務介面
    /// </summary>
    public interface ILoggingService
    {
        Task InitializeAsync();
        /// <summary>
        /// 記錄資訊
        /// </summary>
        void LogInfo(string message, params object[] args);
        
        /// <summary>
        /// 記錄警告
        /// </summary>
        void LogWarning(string message, params object[] args);
        
        /// <summary>
        /// 記錄錯誤
        /// </summary>
        void LogError(string message, Exception exception = null, params object[] args);
        
        /// <summary>
        /// 記錄除錯資訊
        /// </summary>
        void LogDebug(string message, params object[] args);
        
        /// <summary>
        /// 記錄追蹤資訊
        /// </summary>
        void LogTrace(string message, params object[] args);
    }

    /// <summary>
    /// 語言檢測服務介面
    /// </summary>
    public interface ILanguageDetectionService
    {
        /// <summary>
        /// 檢測文字語言
        /// </summary>
        Task<string> DetectLanguageAsync(string text);
        
        /// <summary>
        /// 檢測文字語言 (帶信心度)
        /// </summary>
        Task<LanguageDetectionResult> DetectLanguageWithConfidenceAsync(string text);
        
        /// <summary>
        /// 獲取支援的語言列表
        /// </summary>
        string[] GetSupportedLanguages();
        
        /// <summary>
        /// 檢查是否支援指定語言
        /// </summary>
        bool IsLanguageSupported(string languageCode);
    }

    /// <summary>
    /// 翻譯服務介面 (擴展版)
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// 翻譯文字
        /// </summary>
        Task<TranslationResult> TranslateAsync(string text, string sourceLang = "auto", string targetLang = "zh-TW");
        
        /// <summary>
        /// 批次翻譯
        /// </summary>
        Task<TranslationResult[]> TranslateBatchAsync(string[] texts, string sourceLang = "auto", string targetLang = "zh-TW");
        
        /// <summary>
        /// 獲取支援的翻譯引擎
        /// </summary>
        string[] GetAvailableEngines();
        
        /// <summary>
        /// 設置翻譯引擎
        /// </summary>
        void SetEngine(string engineName);
        
        /// <summary>
        /// 獲取當前引擎
        /// </summary>
        string GetCurrentEngine();
    }

    /// <summary>
    /// 詞典服務介面
    /// </summary>
    public interface IDictionaryService
    {
        /// <summary>
        /// 查詢單詞
        /// </summary>
        Task<DictionaryResult> LookupAsync(string word, string language = "en");
        
        /// <summary>
        /// 獲取單詞建議
        /// </summary>
        Task<string[]> GetSuggestionsAsync(string partialWord, string language = "en");
        
        /// <summary>
        /// 檢查拼寫
        /// </summary>
        Task<bool> CheckSpellingAsync(string word, string language = "en");
        
        /// <summary>
        /// 獲取支援的語言
        /// </summary>
        string[] GetSupportedLanguages();
    }

    /// <summary>
    /// 進度服務介面
    /// </summary>
    public interface IProgressService
    {
        /// <summary>
        /// 顯示進度
        /// </summary>
        void ShowProgress(string title, string message, bool isIndeterminate = false);
        
        /// <summary>
        /// 更新進度
        /// </summary>
        void UpdateProgress(double percentage, string message = null);
        
        /// <summary>
        /// 隱藏進度
        /// </summary>
        void HideProgress();
        
        /// <summary>
        /// 進度是否顯示中
        /// </summary>
        bool IsProgressVisible { get; }
        
        /// <summary>
        /// 進度變更事件
        /// </summary>
        event EventHandler<ProgressEventArgs> ProgressChanged;
    }

    /// <summary>
    /// 資料服務介面
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// 初始化資料庫
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// 儲存翻譯歷史
        /// </summary>
        Task SaveTranslationHistoryAsync(TranslationHistory item);
        
        /// <summary>
        /// 獲取翻譯歷史
        /// </summary>
        Task<TranslationHistory[]> GetTranslationHistoryAsync(int limit = 100);
        
        /// <summary>
        /// 清除歷史記錄
        /// </summary>
        Task ClearHistoryAsync();
        
        /// <summary>
        /// 備份資料
        /// </summary>
        Task BackupDataAsync(string filePath);
        
        /// <summary>
        /// 還原資料
        /// </summary>
        Task RestoreDataAsync(string filePath);
    }

    /// <summary>
    /// 使用者服務介面
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 獲取使用者偏好設定
        /// </summary>
        Task<UserPreferences> GetUserPreferencesAsync();
        
        /// <summary>
        /// 儲存使用者偏好設定
        /// </summary>
        Task SaveUserPreferencesAsync(UserPreferences preferences);
        
        /// <summary>
        /// 獲取使用者統計
        /// </summary>
        Task<UserStatistics> GetUserStatisticsAsync();
        
        /// <summary>
        /// 更新使用統計
        /// </summary>
        Task UpdateUsageStatisticsAsync(string action, object data = null);
    }

    /// <summary>
    /// 設定服務介面 (進階設定管理)
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 獲取應用程式設定
        /// </summary>
        Task<AppSettings> GetAppSettingsAsync();
        
        /// <summary>
        /// 儲存應用程式設定
        /// </summary>
        Task SaveAppSettingsAsync(AppSettings settings);
        
        /// <summary>
        /// 重設為預設設定
        /// </summary>
        Task ResetToDefaultAsync();
        
        /// <summary>
        /// 匯入設定
        /// </summary>
        Task ImportSettingsAsync(string filePath);
        
        /// <summary>
        /// 匯出設定
        /// </summary>
        Task ExportSettingsAsync(string filePath);
        
        /// <summary>
        /// 設定變更事件
        /// </summary>
        event EventHandler<SettingsChangedEventArgs> SettingsChanged;
    }

    #region 事件參數和資料結構

    /// <summary>
    /// 設定變更事件參數
    /// </summary>
    public class SettingChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public SettingChangedEventArgs(string key, object oldValue, object newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// 熱鍵資訊
    /// </summary>
    public class HotKeyInfo
    {
        public int Id { get; set; }
        public System.Windows.Input.ModifierKeys Modifiers { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public string Description { get; set; }
        public Action Action { get; set; }
    }

    /// <summary>
    /// 熱鍵按下事件參數
    /// </summary>
    public class HotKeyPressedEventArgs : EventArgs
    {
        public HotKeyInfo HotKey { get; }

        public HotKeyPressedEventArgs(HotKeyInfo hotKey)
        {
            HotKey = hotKey;
        }
    }

    /// <summary>
    /// 翻譯結果
    /// </summary>
    public class TranslationResult
    {
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public double Confidence { get; set; }
        public string Engine { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 擷取結果
    /// </summary>
    public class CaptureResult
    {
        public byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public System.Windows.Rect Region { get; set; }
        public System.Drawing.Rectangle CapturedRegion { get; set; }
        public System.Drawing.Bitmap Image { get; set; }
        public string ExtractedText { get; set; }
        public bool Success { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 擷取完成事件參數
    /// </summary>
    public class CaptureCompletedEventArgs : EventArgs
    {
        public CaptureResult Result { get; set; }

        public CaptureCompletedEventArgs() { }

        public CaptureCompletedEventArgs(CaptureResult result)
        {
            Result = result;
        }
    }

    /// <summary>
    /// 音訊資料事件參數
    /// </summary>
    public class AudioDataEventArgs : EventArgs
    {
        public byte[] AudioData { get; }
        public int SampleRate { get; }
        public int Channels { get; }

        public AudioDataEventArgs(byte[] audioData, int sampleRate, int channels)
        {
            AudioData = audioData;
            SampleRate = sampleRate;
            Channels = channels;
        }
    }

    /// <summary>
    /// 擷取錯誤事件參數
    /// </summary>
    public class CaptureErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public CaptureResult Result { get; set; }
        public string Error { get; set; }

        public CaptureErrorEventArgs() { }

        public CaptureErrorEventArgs(Exception exception, CaptureResult result = null)
        {
            Exception = exception;
            Result = result;
            Error = exception?.Message;
        }
    }

    /// <summary>
    /// 音訊錄製完成事件參數
    /// </summary>
    public class AudioRecordingCompletedEventArgs : EventArgs
    {
        public AudioResult Result { get; set; }

        public AudioRecordingCompletedEventArgs() { }

        public AudioRecordingCompletedEventArgs(AudioResult result)
        {
            Result = result;
        }
    }

    /// <summary>
    /// 音訊播放完成事件參數
    /// </summary>
    public class AudioPlaybackCompletedEventArgs : EventArgs
    {
        public string Text { get; set; }
        public string Voice { get; set; }
        public bool Success { get; set; }

        public AudioPlaybackCompletedEventArgs() { }

        public AudioPlaybackCompletedEventArgs(string text = "", string voice = "", bool success = true)
        {
            Text = text;
            Voice = voice;
            Success = success;
        }
    }

    /// <summary>
    /// 音訊錯誤事件參數
    /// </summary>
    public class AudioErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public AudioResult Result { get; set; }
        public string Error { get; set; }

        public AudioErrorEventArgs() { }

        public AudioErrorEventArgs(Exception exception, AudioResult result = null)
        {
            Exception = exception;
            Result = result;
            Error = exception?.Message;
        }
    }

    /// <summary>
    /// 音訊處理結果
    /// </summary>
    public class AudioResult
    {
        public bool Success { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public byte[] AudioData { get; set; }
        public TimeSpan Duration { get; set; }
        public int SampleRate { get; set; }
        public string InputDevice { get; set; }
        public int DataSize { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 語言檢測結果
    /// </summary>
    public class LanguageDetectionResult
    {
        public string Language { get; set; }
        public double Confidence { get; set; }
        public bool IsReliable { get; set; }
        public string[] AlternativeLanguages { get; set; }
    }

    /// <summary>
    /// 詞典查詢結果
    /// </summary>
    public class DictionaryResult
    {
        public string Word { get; set; }
        public string Language { get; set; }
        public DictionaryEntry[] Entries { get; set; }
        public string[] Pronunciations { get; set; }
        public bool IsFound { get; set; }
    }

    /// <summary>
    /// 詞典條目
    /// </summary>
    public class DictionaryEntry
    {
        public string PartOfSpeech { get; set; }
        public string Definition { get; set; }
        public string[] Examples { get; set; }
        public string[] Synonyms { get; set; }
        public string[] Antonyms { get; set; }
    }

    /// <summary>
    /// 進度事件參數
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public double Percentage { get; set; }
        public string Message { get; set; }
        public bool IsIndeterminate { get; set; }
        public bool IsCompleted { get; set; }

        public ProgressEventArgs(double percentage, string message, bool isIndeterminate = false)
        {
            Percentage = percentage;
            Message = message;
            IsIndeterminate = isIndeterminate;
            IsCompleted = percentage >= 100;
        }
    }

    /// <summary>
    /// 翻譯歷史記錄
    /// </summary>
    public class TranslationHistory
    {
        public int Id { get; set; }
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public string Engine { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsFavorite { get; set; }
    }

    /// <summary>
    /// 使用者偏好設定
    /// </summary>
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

    /// <summary>
    /// 使用者統計
    /// </summary>
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

    /// <summary>
    /// 應用程式設定
    /// </summary>
    public class AppSettings
    {
        public GeneralSettings General { get; set; } = new();
        public TranslationSettings Translation { get; set; } = new();
        public CaptureSettings Capture { get; set; } = new();
        public AudioSettings Audio { get; set; } = new();
        public HotKeySettings HotKeys { get; set; } = new();
    }

    /// <summary>
    /// 一般設定
    /// </summary>
    public class GeneralSettings
    {
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool CheckUpdatesOnStart { get; set; } = true;
        public string Language { get; set; } = "zh-TW";
        public string Theme { get; set; } = "Light";
    }

    /// <summary>
    /// 翻譯設定
    /// </summary>
    public class TranslationSettings
    {
        public string DefaultEngine { get; set; } = "Google";
        public string DefaultSourceLanguage { get; set; } = "auto";
        public string DefaultTargetLanguage { get; set; } = "zh-TW";
        public bool AutoDetectLanguage { get; set; } = true;
        public bool AutoCopyResult { get; set; } = false;
        public bool ShowOriginalText { get; set; } = true;
        public int MaxHistoryItems { get; set; } = 1000;
    }

    /// <summary>
    /// 擷取設定
    /// </summary>
    public class CaptureSettings
    {
        public bool ShowCapturePreview { get; set; } = true;
        public bool AutoTranslateAfterCapture { get; set; } = true;
        public int CaptureDelay { get; set; } = 500;
        public bool HighlightCapturedArea { get; set; } = true;
        public string CaptureSound { get; set; } = "Default";
    }

    /// <summary>
    /// 音訊設定
    /// </summary>
    public class AudioSettings
    {
        public string InputDevice { get; set; } = "Default";
        public string OutputDevice { get; set; } = "Default";
        public int SampleRate { get; set; } = 16000;
        public int Channels { get; set; } = 1;
        public double Volume { get; set; } = 1.0;
        public bool NoiseReduction { get; set; } = true;
    }

    /// <summary>
    /// 熱鍵設定
    /// </summary>
    public class HotKeySettings
    {
        public HotKeyInfo CaptureHotKey { get; set; }
        public HotKeyInfo AudioCaptureHotKey { get; set; }
        public HotKeyInfo ShowHideHotKey { get; set; }
        public HotKeyInfo TranslateClipboardHotKey { get; set; }
    }

    /// <summary>
    /// 設定變更事件參數
    /// </summary>
    public class SettingsChangedEventArgs : EventArgs
    {
        public string Section { get; set; }
        public string Key { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }

        public SettingsChangedEventArgs(string section, string key, object oldValue, object newValue)
        {
            Section = section;
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// 圖像前處理服務介面
    /// 提供圖像載入、角度檢測、校正、K-means聚類等功能
    /// </summary>
    public interface IImagePreprocessor : IDisposable
    {
        /// <summary>
        /// 從位元組陣列載入圖像
        /// </summary>
        /// <param name="imageData">圖像位元組陣列</param>
        /// <returns>OpenCV Mat 物件</returns>
        Mat LoadImage(byte[] imageData);

        /// <summary>
        /// 從 System.Drawing.Bitmap 轉換為 OpenCV Mat
        /// </summary>
        /// <param name="bitmap">System.Drawing.Bitmap 物件</param>
        /// <returns>OpenCV Mat 物件</returns>
        Mat LoadImageFromBitmap(System.Drawing.Bitmap bitmap);

        /// <summary>
        /// 檢測圖像的旋轉角度
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <returns>檢測到的角度（度數）</returns>
        double DetectAngle(Mat image);

        /// <summary>
        /// 校正圖像角度
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <param name="angle">要校正的角度（度數）</param>
        /// <returns>校正後的圖像</returns>
        Mat CorrectAngle(Mat image, double angle);

        /// <summary>
        /// 使用 K-means 進行圖像區域聚類
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <param name="clusterCount">聚類數量</param>
        /// <returns>聚類後的圖像</returns>
        Mat ApplyKMeansClustering(Mat image, int clusterCount = 3);
    }

    /// <summary>
    /// 文字區域檢測服務介面
    /// 提供文字區域檢測和版面分析功能
    /// </summary>
    public interface ITextRegionDetector : IDisposable
    {
        /// <summary>
        /// 檢測圖像中的文字區域
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <param name="clusterCount">K-means 聚類數量</param>
        /// <returns>檢測到的文字區域列表</returns>
        List<OpenCvSharp.Rect> DetectTextRegions(Mat image, int clusterCount = 3);

        /// <summary>
        /// 分析圖像版面結構
        /// </summary>
        /// <param name="image">輸入圖像</param>
        /// <returns>版面分析結果</returns>
        LayoutAnalysisResult AnalyzeLayout(Mat image);
    }

    /// <summary>
    /// 版面分析結果
    /// </summary>
    public class LayoutAnalysisResult
    {
        /// <summary>
        /// 檢測到的文字區域
        /// </summary>
        public List<OpenCvSharp.Rect> TextRegions { get; set; } = new List<OpenCvSharp.Rect>();

        /// <summary>
        /// 圖像尺寸
        /// </summary>
        public OpenCvSharp.Size ImageSize { get; set; }

        /// <summary>
        /// 區域數量
        /// </summary>
        public int RegionCount { get; set; }

        /// <summary>
        /// 文字密度（文字區域面積/總面積）
        /// </summary>
        public double TextDensity { get; set; }

        /// <summary>
        /// 主要文字方向
        /// </summary>
        public TextOrientation PrimaryOrientation { get; set; }
    }

    /// <summary>
    /// 文字方向枚舉
    /// </summary>
    public enum TextOrientation
    {
        /// <summary>
        /// 水平文字
        /// </summary>
        Horizontal,

        /// <summary>
        /// 垂直文字
        /// </summary>
        Vertical,

        /// <summary>
        /// 混合方向
        /// </summary>
        Mixed
    }

    #endregion
}
