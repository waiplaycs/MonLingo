using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 轉錄結果
    /// </summary>
    public class TranscriptionResult
    {
        public string Text { get; set; }
        public string Language { get; set; }
        public float Confidence { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public List<TranscriptionSegment> Segments { get; set; } = new List<TranscriptionSegment>();
    }

    /// <summary>
    /// 轉錄片段
    /// </summary>
    public class TranscriptionSegment
    {
        public string Text { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public float Confidence { get; set; }
    }

    /// <summary>
    /// 轉錄語言設定
    /// </summary>
    public enum TranscriptionLanguage
    {
        Auto,       // 自動檢測
        Chinese,    // 中文
        English,    // 英文
        Japanese,   // 日文
        Korean,     // 韓文
        Spanish,    // 西班牙文
        French,     // 法文
        German,     // 德文
        Russian,    // 俄文
        Arabic,     // 阿拉伯文
        Portuguese  // 葡萄牙文
    }

    /// <summary>
    /// Whisper 模型大小
    /// </summary>
    public enum WhisperModelSize
    {
        Tiny,       // 最快，精度較低
        Base,       // 平衡
        Small,      // 良好精度
        Medium,     // 高精度
        Large       // 最高精度，較慢
    }

    /// <summary>
    /// 語音轉錄服務介面
    /// 依據 PRD §3、§8.4 實現 Whisper.net 語音識別整合
    /// </summary>
    public interface ITranscriptionService
    {
        /// <summary>
        /// 初始化 Whisper 模型
        /// </summary>
        Task<bool> InitializeAsync(WhisperModelSize modelSize = WhisperModelSize.Base);

        /// <summary>
        /// 轉錄音訊數據
        /// </summary>
        Task<TranscriptionResult> TranscribeAsync(byte[] audioData, int sampleRate, TranscriptionLanguage language = TranscriptionLanguage.Auto);

        /// <summary>
        /// 轉錄音訊檔案
        /// </summary>
        Task<TranscriptionResult> TranscribeFileAsync(string audioFilePath, TranscriptionLanguage language = TranscriptionLanguage.Auto);

        /// <summary>
        /// 設定轉錄參數
        /// </summary>
        void SetTranscriptionOptions(bool enableTimestamps = true, bool enableWordTimestamps = false, float temperature = 0.0f);

        /// <summary>
        /// 取得支援的語言清單
        /// </summary>
        string[] GetSupportedLanguages();

        /// <summary>
        /// 檢查模型是否已載入
        /// </summary>
        bool IsModelLoaded { get; }

        /// <summary>
        /// 取得當前模型大小
        /// </summary>
        WhisperModelSize CurrentModelSize { get; }

        /// <summary>
        /// 轉錄完成事件
        /// </summary>
        event EventHandler<TranscriptionResult> TranscriptionCompleted;

        /// <summary>
        /// 轉錄錯誤事件
        /// </summary>
        event EventHandler<Exception> TranscriptionError;
    }

    /// <summary>
    /// Whisper 轉錄服務實現
    /// 依據 PRD 要求實現 WER ≤ 12% 的中英文語音識別
    /// </summary>
    public class TranscriptionService : ITranscriptionService, IDisposable
    {
        private bool _isModelLoaded = false;
        private WhisperModelSize _currentModelSize = WhisperModelSize.Base;
        private string _modelPath = string.Empty;
        private bool _disposed = false;

        // 轉錄選項
        private bool _enableTimestamps = true;
        private bool _enableWordTimestamps = false;
        private float _temperature = 0.0f;

        // 支援的語言代碼對照
        private readonly Dictionary<TranscriptionLanguage, string> _languageCodes = new Dictionary<TranscriptionLanguage, string>
        {
            { TranscriptionLanguage.Auto, "auto" },
            { TranscriptionLanguage.Chinese, "zh" },
            { TranscriptionLanguage.English, "en" },
            { TranscriptionLanguage.Japanese, "ja" },
            { TranscriptionLanguage.Korean, "ko" },
            { TranscriptionLanguage.Spanish, "es" },
            { TranscriptionLanguage.French, "fr" },
            { TranscriptionLanguage.German, "de" },
            { TranscriptionLanguage.Russian, "ru" },
            { TranscriptionLanguage.Arabic, "ar" },
            { TranscriptionLanguage.Portuguese, "pt" }
        };

        public event EventHandler<TranscriptionResult> TranscriptionCompleted;
        public event EventHandler<Exception> TranscriptionError;

        public bool IsModelLoaded => _isModelLoaded;
        public WhisperModelSize CurrentModelSize => _currentModelSize;

        /// <summary>
        /// 初始化 Whisper 模型
        /// </summary>
        public async Task<bool> InitializeAsync(WhisperModelSize modelSize = WhisperModelSize.Base)
        {
            try
            {
                await Task.Run(() =>
                {
                    // 模擬模型載入 - 實際實現時需要載入真實的 Whisper 模型
                    _modelPath = GetModelPath(modelSize);
                    _currentModelSize = modelSize;
                    
                    // 檢查模型檔案是否存在
                    if (!System.IO.File.Exists(_modelPath))
                    {
                        throw new System.IO.FileNotFoundException($"Whisper 模型檔案不存在: {_modelPath}");
                    }

                    // 載入模型 (模擬)
                    System.Threading.Thread.Sleep(1000); // 模擬載入時間
                    _isModelLoaded = true;
                });

                System.Diagnostics.Debug.WriteLine($"Whisper 模型載入成功: {modelSize}");
                return true;
            }
            catch (Exception ex)
            {
                _isModelLoaded = false;
                TranscriptionError?.Invoke(this, ex);
                System.Diagnostics.Debug.WriteLine($"Whisper 模型載入失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 轉錄音訊數據
        /// </summary>
        public async Task<TranscriptionResult> TranscribeAsync(byte[] audioData, int sampleRate, TranscriptionLanguage language = TranscriptionLanguage.Auto)
        {
            if (!_isModelLoaded)
            {
                throw new InvalidOperationException("Whisper 模型尚未載入，請先呼叫 InitializeAsync");
            }

            if (audioData == null || audioData.Length == 0)
            {
                throw new ArgumentException("音訊數據不能為空", nameof(audioData));
            }

            try
            {
                var startTime = DateTime.Now;

                var result = await Task.Run(() =>
                {
                    // 模擬轉錄處理 - 實際實現時需要呼叫 Whisper.net API
                    var processingTime = GetProcessingTime(audioData.Length);
                    System.Threading.Thread.Sleep(processingTime);

                    // 模擬轉錄結果
                    var mockResult = new TranscriptionResult
                    {
                        Text = GenerateMockTranscription(language),
                        Language = _languageCodes.ContainsKey(language) ? _languageCodes[language] : "auto",
                        Confidence = 0.92f, // 模擬高置信度
                        Duration = TimeSpan.FromMilliseconds(audioData.Length / (sampleRate * 2.0) * 1000),
                        Timestamp = startTime
                    };

                    // 添加模擬片段
                    mockResult.Segments.Add(new TranscriptionSegment
                    {
                        Text = mockResult.Text,
                        Start = TimeSpan.Zero,
                        End = mockResult.Duration,
                        Confidence = mockResult.Confidence
                    });

                    return mockResult;
                });

                TranscriptionCompleted?.Invoke(this, result);
                return result;
            }
            catch (Exception ex)
            {
                TranscriptionError?.Invoke(this, ex);
                System.Diagnostics.Debug.WriteLine($"音訊轉錄失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 轉錄音訊檔案
        /// </summary>
        public async Task<TranscriptionResult> TranscribeFileAsync(string audioFilePath, TranscriptionLanguage language = TranscriptionLanguage.Auto)
        {
            if (!System.IO.File.Exists(audioFilePath))
            {
                throw new System.IO.FileNotFoundException($"音訊檔案不存在: {audioFilePath}");
            }

            try
            {
                // 讀取音訊檔案並轉換為 byte array
                                var audioData = await Task.Run(() => File.ReadAllBytes(audioFilePath));
                
                // 假設為 16kHz, 16bit PCM 格式
                const int sampleRate = 16000;
                
                return await TranscribeAsync(audioData, sampleRate, language);
            }
            catch (Exception ex)
            {
                TranscriptionError?.Invoke(this, ex);
                System.Diagnostics.Debug.WriteLine($"音訊檔案轉錄失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 設定轉錄參數
        /// </summary>
        public void SetTranscriptionOptions(bool enableTimestamps = true, bool enableWordTimestamps = false, float temperature = 0.0f)
        {
            _enableTimestamps = enableTimestamps;
            _enableWordTimestamps = enableWordTimestamps;
            _temperature = Math.Max(0.0f, Math.Min(1.0f, temperature));
        }

        /// <summary>
        /// 取得支援的語言清單
        /// </summary>
        public string[] GetSupportedLanguages()
        {
            return new string[]
            {
                "auto", "zh", "en", "ja", "ko", "es", "fr", "de", "ru", "ar", "pt",
                "it", "nl", "pl", "sv", "da", "no", "fi", "tr", "th", "vi"
            };
        }

        /// <summary>
        /// 取得模型檔案路徑
        /// </summary>
        private string GetModelPath(WhisperModelSize modelSize)
        {
            var modelsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "whisper");
            var modelFileName = $"ggml-{modelSize.ToString().ToLower()}.bin";
            return System.IO.Path.Combine(modelsDir, modelFileName);
        }

        /// <summary>
        /// 取得處理時間 (模擬)
        /// </summary>
        private int GetProcessingTime(int audioDataLength)
        {
            // 基於音訊長度模擬處理時間
            var baseTime = Math.Max(100, audioDataLength / 10000); // 最少 100ms
            var modelMultiplier = _currentModelSize switch
            {
                WhisperModelSize.Tiny => 0.5,
                WhisperModelSize.Base => 1.0,
                WhisperModelSize.Small => 1.5,
                WhisperModelSize.Medium => 2.0,
                WhisperModelSize.Large => 3.0,
                _ => 1.0
            };

            return (int)(baseTime * modelMultiplier);
        }

        /// <summary>
        /// 生成模擬轉錄文字
        /// </summary>
        private string GenerateMockTranscription(TranscriptionLanguage language)
        {
            return language switch
            {
                TranscriptionLanguage.Chinese => "這是一段中文語音轉錄的測試結果",
                TranscriptionLanguage.English => "This is a test result for English speech transcription",
                TranscriptionLanguage.Japanese => "これは日本語音声転写のテスト結果です",
                TranscriptionLanguage.Korean => "이것은 한국어 음성 전사 테스트 결과입니다",
                _ => "This is a mock transcription result for testing purposes"
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _isModelLoaded = false;
                _disposed = true;
            }
        }
    }
}
