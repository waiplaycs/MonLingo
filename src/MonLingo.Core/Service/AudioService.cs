using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Linq;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 音訊服務實現
    /// 依據 PRD §8.4 實現 WASAPI 音訊擷取，支援麥克風和系統音訊 (Loopback)
    /// </summary>
    public class AudioService : IAudioService, IDisposable
    {
        private WasapiCapture _capture;
        private WasapiLoopbackCapture _loopbackCapture;
        private AudioCaptureState _state;
        private readonly object _stateLock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;

        // 音訊品質參數
        private int _sampleRate = 16000;
        private int _bitsPerSample = 16;
        private int _channels = 1;

        // 緩衝區設定
        private readonly int _bufferDurationMs = 100;
        private readonly List<byte> _audioBuffer = new List<byte>();
        private readonly object _bufferLock = new object();

        public event EventHandler<AudioDataEventArgs> AudioDataReceived;
        public event EventHandler<AudioCaptureState> StateChanged;
        public event EventHandler<Exception> AudioError;

        public AudioCaptureState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
            private set
            {
                lock (_stateLock)
                {
                    if (_state != value)
                    {
                        _state = value;
                        StateChanged?.Invoke(this, value);
                    }
                }
            }
        }

        public AudioService()
        {
            State = AudioCaptureState.Stopped;
        }

        /// <summary>
        /// 開始音訊擷取
        /// </summary>
        public async Task<bool> StartCaptureAsync(AudioDeviceInfo device = null)
        {
            try
            {
                if (State != AudioCaptureState.Stopped)
                {
                    await StopCaptureAsync();
                }

                State = AudioCaptureState.Starting;
                _cancellationTokenSource = new CancellationTokenSource();

                // 決定使用的設備
                if (device == null)
                {
                    device = await GetDefaultMicrophoneAsync();
                }

                await Task.Run(() =>
                {
                    if (device.Type == AudioDeviceType.SystemAudio || device.Type == AudioDeviceType.Loopback)
                    {
                        StartLoopbackCapture();
                    }
                    else
                    {
                        StartMicrophoneCapture(device);
                    }
                });

                State = AudioCaptureState.Recording;
                return true;
            }
            catch (Exception ex)
            {
                State = AudioCaptureState.Error;
                AudioError?.Invoke(this, ex);
                System.Diagnostics.Debug.WriteLine($"音訊擷取啟動失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止音訊擷取
        /// </summary>
        public async Task StopCaptureAsync()
        {
            try
            {
                if (State == AudioCaptureState.Stopped)
                    return;

                State = AudioCaptureState.Stopping;

                _cancellationTokenSource?.Cancel();

                await Task.Run(() =>
                {
                    try
                    {
                        _capture?.StopRecording();
                        _capture?.Dispose();
                        _capture = null;

                        _loopbackCapture?.StopRecording();
                        _loopbackCapture?.Dispose();
                        _loopbackCapture = null;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"停止音訊擷取時發生錯誤: {ex.Message}");
                    }
                });

                lock (_bufferLock)
                {
                    _audioBuffer.Clear();
                }

                State = AudioCaptureState.Stopped;
            }
            catch (Exception ex)
            {
                State = AudioCaptureState.Error;
                AudioError?.Invoke(this, ex);
                System.Diagnostics.Debug.WriteLine($"停止音訊擷取失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得可用音訊設備清單
        /// </summary>
        public async Task<AudioDeviceInfo[]> GetAudioDevicesAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<AudioDeviceInfo>();

                try
                {
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        // 取得麥克風設備
                        var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                        foreach (var device in captureDevices)
                        {
                            devices.Add(new AudioDeviceInfo
                            {
                                Id = device.ID,
                                Name = device.FriendlyName,
                                IsDefault = device.ID == enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console).ID,
                                IsEnabled = device.State == DeviceState.Active,
                                Type = AudioDeviceType.Microphone
                            });
                        }

                        // 添加系統音訊 (Loopback)
                        var renderDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        foreach (var device in renderDevices)
                        {
                            devices.Add(new AudioDeviceInfo
                            {
                                Id = device.ID,
                                Name = $"{device.FriendlyName} (系統音訊)",
                                IsDefault = device.ID == enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console).ID,
                                IsEnabled = device.State == DeviceState.Active,
                                Type = AudioDeviceType.SystemAudio
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"取得音訊設備清單失敗: {ex.Message}");
                }

                return devices.ToArray();
            });
        }

        /// <summary>
        /// 取得預設麥克風設備
        /// </summary>
        public async Task<AudioDeviceInfo> GetDefaultMicrophoneAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                        return new AudioDeviceInfo
                        {
                            Id = device.ID,
                            Name = device.FriendlyName,
                            IsDefault = true,
                            IsEnabled = device.State == DeviceState.Active,
                            Type = AudioDeviceType.Microphone
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"取得預設麥克風失敗: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// 取得預設系統音訊設備 (Loopback)
        /// </summary>
        public async Task<AudioDeviceInfo> GetDefaultSystemAudioAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                        return new AudioDeviceInfo
                        {
                            Id = device.ID,
                            Name = $"{device.FriendlyName} (系統音訊)",
                            IsDefault = true,
                            IsEnabled = device.State == DeviceState.Active,
                            Type = AudioDeviceType.SystemAudio
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"取得預設系統音訊設備失敗: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// 設定音訊品質參數
        /// </summary>
        public void SetAudioQuality(int sampleRate = 16000, int bitsPerSample = 16, int channels = 1)
        {
            _sampleRate = sampleRate;
            _bitsPerSample = bitsPerSample;
            _channels = channels;
        }

        /// <summary>
        /// 啟動麥克風擷取
        /// </summary>
        private void StartMicrophoneCapture(AudioDeviceInfo device)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var mmDevice = enumerator.GetDevice(device.Id);
                _capture = new WasapiCapture(mmDevice);
                
                _capture.DataAvailable += OnAudioDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;

                _capture.StartRecording();
            }
        }

        /// <summary>
        /// 啟動系統音訊 Loopback 擷取
        /// </summary>
        private void StartLoopbackCapture()
        {
            _loopbackCapture = new WasapiLoopbackCapture();
            
            _loopbackCapture.DataAvailable += OnAudioDataAvailable;
            _loopbackCapture.RecordingStopped += OnRecordingStopped;

            _loopbackCapture.StartRecording();
        }

        /// <summary>
        /// 處理音訊數據
        /// </summary>
        private void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                    return;

                lock (_bufferLock)
                {
                    // 添加新數據到緩衝區
                    for (int i = 0; i < e.BytesRecorded; i++)
                    {
                        _audioBuffer.Add(e.Buffer[i]);
                    }

                    // 檢查是否有足夠數據進行處理
                    var format = _capture?.WaveFormat ?? _loopbackCapture?.WaveFormat;
                    if (format != null)
                    {
                        var bytesPerMs = format.AverageBytesPerSecond / 1000;
                        var targetBufferSize = bytesPerMs * _bufferDurationMs;

                        if (_audioBuffer.Count >= targetBufferSize)
                        {
                            // 取出處理用的數據
                            var audioData = _audioBuffer.Take((int)targetBufferSize).ToArray();
                            _audioBuffer.RemoveRange(0, (int)targetBufferSize);

                            // 觸發事件
                            AudioDataReceived?.Invoke(this, new AudioDataEventArgs
                            {
                                AudioData = audioData,
                                SampleRate = format.SampleRate,
                                Channels = format.Channels,
                                BitsPerSample = format.BitsPerSample,
                                Timestamp = DateTime.Now
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AudioError?.Invoke(this, ex);
                System.Diagnostics.Debug.WriteLine($"處理音訊數據時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理錄音停止事件
        /// </summary>
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                State = AudioCaptureState.Error;
                AudioError?.Invoke(this, e.Exception);
                System.Diagnostics.Debug.WriteLine($"錄音意外停止: {e.Exception.Message}");
            }
            else if (State == AudioCaptureState.Recording)
            {
                // 非預期停止
                State = AudioCaptureState.Stopped;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopCaptureAsync().Wait(5000); // 最多等待 5 秒
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }
    }
}
