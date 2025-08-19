using System;
using System.Threading.Tasks;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 音訊數據事件參數
    /// </summary>
    public class AudioDataEventArgs : EventArgs
    {
        public byte[] AudioData { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 音訊處理狀態
    /// </summary>
    public enum AudioCaptureState
    {
        Stopped,
        Starting,
        Recording,
        Stopping,
        Error
    }

    /// <summary>
    /// 音訊設備資訊
    /// </summary>
    public class AudioDeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; }
        public AudioDeviceType Type { get; set; }
    }

    /// <summary>
    /// 音訊設備類型
    /// </summary>
    public enum AudioDeviceType
    {
        Microphone,
        SystemAudio,
        Loopback
    }

    /// <summary>
    /// 音訊服務介面
    /// 依據 PRD §8.4、§3 規範實現 WASAPI 音訊擷取與 Whisper 轉錄整合
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// 開始音訊擷取
        /// </summary>
        Task<bool> StartCaptureAsync(AudioDeviceInfo device = null);

        /// <summary>
        /// 停止音訊擷取
        /// </summary>
        Task StopCaptureAsync();

        /// <summary>
        /// 取得可用音訊設備清單
        /// </summary>
        Task<AudioDeviceInfo[]> GetAudioDevicesAsync();

        /// <summary>
        /// 取得預設麥克風設備
        /// </summary>
        Task<AudioDeviceInfo> GetDefaultMicrophoneAsync();

        /// <summary>
        /// 取得預設系統音訊設備 (Loopback)
        /// </summary>
        Task<AudioDeviceInfo> GetDefaultSystemAudioAsync();

        /// <summary>
        /// 設定音訊品質參數
        /// </summary>
        void SetAudioQuality(int sampleRate = 16000, int bitsPerSample = 16, int channels = 1);

        /// <summary>
        /// 取得當前擷取狀態
        /// </summary>
        AudioCaptureState State { get; }

        /// <summary>
        /// 音訊數據接收事件 (即時串流)
        /// </summary>
        event EventHandler<AudioDataEventArgs> AudioDataReceived;

        /// <summary>
        /// 音訊擷取狀態變更事件
        /// </summary>
        event EventHandler<AudioCaptureState> StateChanged;

        /// <summary>
        /// 音訊擷取錯誤事件
        /// </summary>
        event EventHandler<Exception> AudioError;
    }
}
