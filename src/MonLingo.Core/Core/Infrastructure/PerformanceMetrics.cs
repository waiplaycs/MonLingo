using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using NLog;

namespace MonLingo.Core.Infrastructure
{
    /// <summary>
    /// 效能計時與指標追蹤系統
    /// </summary>
    public static class PerformanceMetrics
    {
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<long>> Metrics = new();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 測量非同步操作的執行時間
        /// </summary>
        public static async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await action();
                sw.Stop();
                LogMetric(operation, sw.ElapsedMilliseconds);
                Logger.Info($"操作 {operation} 完成，耗時: {sw.ElapsedMilliseconds}ms");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.Error(ex, $"操作 {operation} 失敗，耗時: {sw.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// 測量同步操作的執行時間
        /// </summary>
        public static T Measure<T>(string operation, Func<T> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = action();
                sw.Stop();
                LogMetric(operation, sw.ElapsedMilliseconds);
                Logger.Info($"操作 {operation} 完成，耗時: {sw.ElapsedMilliseconds}ms");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.Error(ex, $"操作 {operation} 失敗，耗時: {sw.ElapsedMilliseconds}ms");
                throw;
            }
        }

        /// <summary>
        /// 計算指定操作的 P95 延遲
        /// </summary>
        public static long GetP95Latency(string operation)
        {
            if (!Metrics.TryGetValue(operation, out var queue) || queue.IsEmpty)
                return 0;

            var array = queue.ToArray();
            Array.Sort(array);
            int p95Index = (int)(array.Length * 0.95);
            return array[p95Index];
        }

        private static void LogMetric(string operation, long elapsedMs)
        {
            var queue = Metrics.GetOrAdd(operation, _ => new ConcurrentQueue<long>());
            queue.Enqueue(elapsedMs);

            // 保持最近 1000 筆數據
            while (queue.Count > 1000)
            {
                queue.TryDequeue(out _);
            }

            // 使用 NLog 記錄性能指標
            Logger.Info($"操作 {operation} 執行時間: {elapsedMs}ms");
        }

        /// <summary>
        /// 清除所有計時數據
        /// </summary>
        public static void Reset()
        {
            Metrics.Clear();
        }
    }
}
