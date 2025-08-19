using System;

namespace MonLingo.Core.Infrastructure
{
    // 事件類型定義
    public class CaptureRequestedEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class SettingsRequestedEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class ExitRequestedEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
