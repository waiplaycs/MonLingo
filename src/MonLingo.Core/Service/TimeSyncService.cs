using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GuerrillaNtp;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 時間同步結果
    /// </summary>
    public class TimeSyncResult
    {
        public bool IsSuccess { get; set; }
        public DateTime NetworkTime { get; set; }
        public TimeSpan Offset { get; set; }
        public double AccuracyMs { get; set; }
        public string ServerHost { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 時間同步服務介面
    /// </summary>
    public interface ITimeSyncService
    {
        Task<TimeSyncResult> SyncTimeAsync(string ntpServer = null);
        bool IsTimeSyncEnabled { get; set; }
        TimeSpan LastOffset { get; }
    }

    /// <summary>
    /// 基於 GuerrillaNtp 2.0.1 的時間同步服務實現
    /// </summary>
    public class TimeSyncService : ITimeSyncService
    {
        private readonly string[] _defaultNtpServers = {
            "pool.ntp.org",
            "time.windows.com", 
            "time.nist.gov",
            "time.google.com",
            "ntp.ubuntu.com"
        };

        public bool IsTimeSyncEnabled { get; set; } = true;
        public TimeSpan LastOffset { get; private set; } = TimeSpan.Zero;

        /// <summary>
        /// 執行時間同步
        /// </summary>
        /// <param name="ntpServer">NTP 伺服器地址，若為 null 則使用預設伺服器</param>
        /// <returns>時間同步結果</returns>
        public async Task<TimeSyncResult> SyncTimeAsync(string ntpServer = null)
        {
            if (!IsTimeSyncEnabled)
            {
                return new TimeSyncResult
                {
                    IsSuccess = false,
                    ErrorMessage = "時間同步功能已停用"
                };
            }

            var serversToTry = string.IsNullOrEmpty(ntpServer) 
                ? _defaultNtpServers 
                : new[] { ntpServer };

            foreach (var server in serversToTry)
            {
                try
                {
                    var result = await QueryNtpServerAsync(server);
                    if (result.IsSuccess)
                    {
                        LastOffset = result.Offset;
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    // 如果不是最後一個伺服器，繼續嘗試下一個
                    if (server != serversToTry.Last())
                    {
                        continue;
                    }

                    // 最後一個伺服器也失敗了，回傳錯誤
                    return new TimeSyncResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"時間同步失敗: {ex.Message}",
                        ServerHost = server
                    };
                }
            }

            return new TimeSyncResult
            {
                IsSuccess = false,
                ErrorMessage = "所有 NTP 伺服器都無法連接"
            };
        }

        /// <summary>
        /// 查詢 NTP 伺服器獲取網路時間 - 使用 GuerrillaNtp 2.0.1 API
        /// </summary>
        private async Task<TimeSyncResult> QueryNtpServerAsync(string server)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                // 解析伺服器地址
                var hostEntry = await Dns.GetHostEntryAsync(server);
                var serverEndpoint = new IPEndPoint(hostEntry.AddressList.First(), 123);
                
                // 使用 GuerrillaNtp 2.0.1 的正確 API
                using (var client = new NtpClient(serverEndpoint))
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    // 執行 NTP 查詢 - 使用同步方法包裝為非同步
                    var ntpPacket = await Task.Run(() => client.Query());
                    
                    var endTime = DateTime.UtcNow;
                    var roundTripTime = (endTime - startTime).TotalMilliseconds;
                    
                    // 從 NTP 包提取網路時間並處理 nullable 類型
                    var networkTime = ntpPacket.ReceiveTimestamp ?? DateTime.UtcNow;
                    
                    // 計算時間偏移
                    var localTime = DateTime.UtcNow;
                    var offset = networkTime - localTime;
                    
                    return new TimeSyncResult
                    {
                        IsSuccess = true,
                        NetworkTime = networkTime,
                        Offset = offset,
                        AccuracyMs = roundTripTime / 2, // 估算精度
                        ServerHost = server
                    };
                }
            }
            catch (Exception ex)
            {
                return new TimeSyncResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ServerHost = server
                };
            }
        }
    }

    /// <summary>
    /// 日期時間擴展方法
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// 將 DateTime 轉換為 NTP 時間戳
        /// </summary>
        public static ulong ToNtpTimestamp(this DateTime dateTime)
        {
            var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var totalSeconds = (dateTime.ToUniversalTime() - ntpEpoch).TotalSeconds;
            var seconds = (uint)totalSeconds;
            var fraction = (uint)((totalSeconds - seconds) * 0x100000000);
            return ((ulong)seconds << 32) | fraction;
        }

        /// <summary>
        /// 從 NTP 時間戳轉換為 DateTime
        /// </summary>
        public static DateTime FromNtpTimestamp(ulong ntpTimestamp)
        {
            var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var seconds = (uint)(ntpTimestamp >> 32);
            var fraction = (uint)(ntpTimestamp & 0xFFFFFFFF);
            var totalSeconds = seconds + (fraction / (double)0x100000000);
            return ntpEpoch.AddSeconds(totalSeconds);
        }
    }
}
