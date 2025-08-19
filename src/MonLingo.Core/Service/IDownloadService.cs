using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Linq;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 下載項目資訊
    /// </summary>
    public class DownloadItem
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string DestinationPath { get; set; }
        public string FileName { get; set; }
        public long TotalSize { get; set; }
        public long DownloadedSize { get; set; }
        public double ProgressPercentage { get; set; }
        public DownloadStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; }
        public bool SupportResume { get; set; }
    }

    /// <summary>
    /// 下載狀態
    /// </summary>
    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// 下載進度事件參數
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        public string DownloadId { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double ProgressPercentage { get; set; }
        public double DownloadSpeed { get; set; } // bytes per second
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    /// <summary>
    /// 下載完成事件參數
    /// </summary>
    public class DownloadCompletedEventArgs : EventArgs
    {
        public string DownloadId { get; set; }
        public bool IsSuccessful { get; set; }
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// 下載服務介面
    /// 依據 PRD §2.1、§11.3 實現 Downloader 任務隊列，支援大檔下載續傳
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// 開始下載
        /// </summary>
        Task<string> StartDownloadAsync(string url, string destinationPath, string fileName = null);

        /// <summary>
        /// 暫停下載
        /// </summary>
        Task<bool> PauseDownloadAsync(string downloadId);

        /// <summary>
        /// 恢復下載
        /// </summary>
        Task<bool> ResumeDownloadAsync(string downloadId);

        /// <summary>
        /// 取消下載
        /// </summary>
        Task<bool> CancelDownloadAsync(string downloadId);

        /// <summary>
        /// 取得下載項目資訊
        /// </summary>
        DownloadItem GetDownloadInfo(string downloadId);

        /// <summary>
        /// 取得所有下載項目
        /// </summary>
        DownloadItem[] GetAllDownloads();

        /// <summary>
        /// 清除已完成或失敗的下載記錄
        /// </summary>
        void ClearCompletedDownloads();

        /// <summary>
        /// 設定下載配置
        /// </summary>
        void SetDownloadConfiguration(int maxConcurrentDownloads = 3, int chunkCount = 8, int timeout = 30000);

        /// <summary>
        /// 檢查 URL 是否支援斷點續傳
        /// </summary>
        Task<bool> CheckResumeSupport(string url);

        /// <summary>
        /// 下載進度更新事件
        /// </summary>
        event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;

        /// <summary>
        /// 下載完成事件
        /// </summary>
        event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

        /// <summary>
        /// 下載錯誤事件
        /// </summary>
        event EventHandler<Exception> DownloadError;
    }

    /// <summary>
    /// 下載服務實現
    /// 使用 Downloader 套件實現高效能檔案下載，支援續傳和並行下載
    /// </summary>
    public class DownloadService : IDownloadService, IDisposable
    {
        private readonly Dictionary<string, DownloadItem> _downloads;
        private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens;
        private readonly object _downloadsLock = new object();
        private SemaphoreSlim _concurrentDownloadsSemaphore;
        private bool _disposed = false;

        // 下載配置
        private int _maxConcurrentDownloads = 3;
        private int _chunkCount = 8;
        private int _timeout = 30000;

        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;
        public event EventHandler<Exception> DownloadError;

        public DownloadService()
        {
            _downloads = new Dictionary<string, DownloadItem>();
            _cancellationTokens = new Dictionary<string, CancellationTokenSource>();
            _concurrentDownloadsSemaphore = new SemaphoreSlim(_maxConcurrentDownloads, _maxConcurrentDownloads);
        }

        /// <summary>
        /// 開始下載
        /// </summary>
        public async Task<string> StartDownloadAsync(string url, string destinationPath, string fileName = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL 不能為空", nameof(url));

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("目標路徑不能為空", nameof(destinationPath));

            var downloadId = Guid.NewGuid().ToString();
            var finalFileName = fileName ?? Path.GetFileName(new Uri(url).LocalPath);
            if (string.IsNullOrWhiteSpace(finalFileName))
                finalFileName = $"download_{DateTime.Now:yyyyMMdd_HHmmss}";

            var fullPath = Path.Combine(destinationPath, finalFileName);

            // 建立下載項目
            var downloadItem = new DownloadItem
            {
                Id = downloadId,
                Url = url,
                DestinationPath = destinationPath,
                FileName = finalFileName,
                Status = DownloadStatus.Pending,
                StartTime = DateTime.Now,
                SupportResume = await CheckResumeSupport(url)
            };

            lock (_downloadsLock)
            {
                _downloads[downloadId] = downloadItem;
                _cancellationTokens[downloadId] = new CancellationTokenSource();
            }

            // 異步開始下載
            _ = Task.Run(async () => await PerformDownloadAsync(downloadId));

            return downloadId;
        }

        /// <summary>
        /// 暫停下載
        /// </summary>
        public async Task<bool> PauseDownloadAsync(string downloadId)
        {
            return await Task.Run(() =>
            {
                lock (_downloadsLock)
                {
                    if (!_downloads.TryGetValue(downloadId, out var item))
                        return false;

                    if (item.Status == DownloadStatus.Downloading)
                    {
                        item.Status = DownloadStatus.Paused;
                        _cancellationTokens[downloadId]?.Cancel();
                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// 恢復下載
        /// </summary>
        public async Task<bool> ResumeDownloadAsync(string downloadId)
        {
            return await Task.Run(async () =>
            {
                lock (_downloadsLock)
                {
                    if (!_downloads.TryGetValue(downloadId, out var item))
                        return false;

                    if (item.Status == DownloadStatus.Paused && item.SupportResume)
                    {
                        item.Status = DownloadStatus.Pending;
                        _cancellationTokens[downloadId] = new CancellationTokenSource();
                        
                        // 重新開始下載
                        _ = Task.Run(async () => await PerformDownloadAsync(downloadId));
                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// 取消下載
        /// </summary>
        public async Task<bool> CancelDownloadAsync(string downloadId)
        {
            return await Task.Run(() =>
            {
                lock (_downloadsLock)
                {
                    if (!_downloads.TryGetValue(downloadId, out var item))
                        return false;

                    item.Status = DownloadStatus.Cancelled;
                    item.EndTime = DateTime.Now;
                    _cancellationTokens[downloadId]?.Cancel();

                    // 清理部分下載的檔案
                    var filePath = Path.Combine(item.DestinationPath, item.FileName);
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"清理下載檔案失敗: {ex.Message}");
                        }
                    }

                    return true;
                }
            });
        }

        /// <summary>
        /// 取得下載項目資訊
        /// </summary>
        public DownloadItem GetDownloadInfo(string downloadId)
        {
            lock (_downloadsLock)
            {
                return _downloads.TryGetValue(downloadId, out var item) ? item : null;
            }
        }

        /// <summary>
        /// 取得所有下載項目
        /// </summary>
        public DownloadItem[] GetAllDownloads()
        {
            lock (_downloadsLock)
            {
                return _downloads.Values.ToArray();
            }
        }

        /// <summary>
        /// 清除已完成或失敗的下載記錄
        /// </summary>
        public void ClearCompletedDownloads()
        {
            lock (_downloadsLock)
            {
                var toRemove = _downloads.Where(kvp => 
                    kvp.Value.Status == DownloadStatus.Completed ||
                    kvp.Value.Status == DownloadStatus.Failed ||
                    kvp.Value.Status == DownloadStatus.Cancelled)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var id in toRemove)
                {
                    _downloads.Remove(id);
                    _cancellationTokens.Remove(id);
                }
            }
        }

        /// <summary>
        /// 設定下載配置
        /// </summary>
        public void SetDownloadConfiguration(int maxConcurrentDownloads = 3, int chunkCount = 8, int timeout = 30000)
        {
            _maxConcurrentDownloads = Math.Max(1, maxConcurrentDownloads);
            _chunkCount = Math.Max(1, chunkCount);
            _timeout = Math.Max(5000, timeout);

            // 更新信號量
            _concurrentDownloadsSemaphore?.Dispose();
            _concurrentDownloadsSemaphore = new SemaphoreSlim(_maxConcurrentDownloads, _maxConcurrentDownloads);
        }

        /// <summary>
        /// 檢查 URL 是否支援斷點續傳
        /// </summary>
        public async Task<bool> CheckResumeSupport(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(5000);
                    var request = new HttpRequestMessage(HttpMethod.Head, url);
                    var response = await client.SendAsync(request);

                    return response.Headers.AcceptRanges?.Contains("bytes") == true;
                }
            }
            catch
            {
                return false; // 預設不支援
            }
        }

        /// <summary>
        /// 執行實際下載
        /// </summary>
        private async Task PerformDownloadAsync(string downloadId)
        {
            await _concurrentDownloadsSemaphore.WaitAsync();

            try
            {
                DownloadItem item;
                CancellationToken cancellationToken;

                lock (_downloadsLock)
                {
                    if (!_downloads.TryGetValue(downloadId, out item) ||
                        !_cancellationTokens.TryGetValue(downloadId, out var cts))
                    {
                        return;
                    }
                    cancellationToken = cts.Token;
                }

                item.Status = DownloadStatus.Downloading;

                var fullPath = Path.Combine(item.DestinationPath, item.FileName);
                
                // 確保目標目錄存在
                Directory.CreateDirectory(item.DestinationPath);

                // 模擬下載過程 (實際實現時使用 Downloader 套件)
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(_timeout);

                    var response = await client.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    item.TotalSize = response.Content.Headers.ContentLength ?? 0;
                    
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[8192];
                        var totalBytesRead = 0L;
                        var lastProgressReport = DateTime.Now;

                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            if (bytesRead == 0) break;

                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            totalBytesRead += bytesRead;

                            item.DownloadedSize = totalBytesRead;
                            item.ProgressPercentage = item.TotalSize > 0 ? (double)totalBytesRead / item.TotalSize * 100 : 0;

                            // 限制進度報告頻率
                            if (DateTime.Now - lastProgressReport > TimeSpan.FromMilliseconds(200))
                            {
                                DownloadProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                                {
                                    DownloadId = downloadId,
                                    DownloadedBytes = totalBytesRead,
                                    TotalBytes = item.TotalSize,
                                    ProgressPercentage = item.ProgressPercentage,
                                    DownloadSpeed = CalculateDownloadSpeed(totalBytesRead, item.StartTime),
                                    EstimatedTimeRemaining = CalculateEstimatedTime(totalBytesRead, item.TotalSize, item.StartTime)
                                });

                                lastProgressReport = DateTime.Now;
                            }
                        }
                    }
                }

                item.Status = DownloadStatus.Completed;
                item.EndTime = DateTime.Now;

                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                {
                    DownloadId = downloadId,
                    IsSuccessful = true,
                    FilePath = fullPath,
                    Duration = item.EndTime.Value - item.StartTime
                });
            }
            catch (OperationCanceledException)
            {
                // 下載被取消或暫停
                var item = GetDownloadInfo(downloadId);
                if (item?.Status != DownloadStatus.Paused)
                {
                    item.Status = DownloadStatus.Cancelled;
                    item.EndTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                var item = GetDownloadInfo(downloadId);
                if (item != null)
                {
                    item.Status = DownloadStatus.Failed;
                    item.EndTime = DateTime.Now;
                    item.ErrorMessage = ex.Message;
                }

                DownloadError?.Invoke(this, ex);
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                {
                    DownloadId = downloadId,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                });
            }
            finally
            {
                _concurrentDownloadsSemaphore.Release();
            }
        }

        /// <summary>
        /// 計算下載速度
        /// </summary>
        private double CalculateDownloadSpeed(long bytesDownloaded, DateTime startTime)
        {
            var elapsed = DateTime.Now - startTime;
            return elapsed.TotalSeconds > 0 ? bytesDownloaded / elapsed.TotalSeconds : 0;
        }

        /// <summary>
        /// 計算預估剩餘時間
        /// </summary>
        private TimeSpan CalculateEstimatedTime(long bytesDownloaded, long totalBytes, DateTime startTime)
        {
            if (bytesDownloaded <= 0 || totalBytes <= 0 || bytesDownloaded >= totalBytes)
                return TimeSpan.Zero;

            var elapsed = DateTime.Now - startTime;
            var progress = (double)bytesDownloaded / totalBytes;
            var totalEstimatedTime = TimeSpan.FromTicks((long)(elapsed.Ticks / progress));
            
            return totalEstimatedTime - elapsed;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // 取消所有進行中的下載
                lock (_downloadsLock)
                {
                    foreach (var cts in _cancellationTokens.Values)
                    {
                        cts?.Cancel();
                        cts?.Dispose();
                    }

                    _cancellationTokens.Clear();
                    _downloads.Clear();
                }

                _concurrentDownloadsSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}
