using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonLingo.Core.Infrastructure
{
    // 簡化版本的通知類型
    public enum SimpleNotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 簡化的事件聚合器介面 
    /// </summary>
    public interface ISimpleEventAggregator
    {
        void Publish<T>(T eventObject) where T : class;
        void Subscribe<T>(Action<T> handler) where T : class;
    }

    /// <summary>
    /// 簡化的通知服務介面
    /// </summary>
    public interface ISimpleNotificationService
    {
        void ShowNotification(string title, string message);
        void ShowError(string message);
        void ShowWarning(string message);
        void ShowInfo(string message);
    }

    /// <summary>
    /// 簡單的服務容器實現
    /// </summary>
    public class SimpleServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Initialize()
        {
            // 註冊服務
            _services[typeof(ISimpleEventAggregator)] = new SimpleEventAggregator();
            _services[typeof(ISimpleNotificationService)] = new SimpleNotificationService();
        }

        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }
            return null;
        }
    }

    /// <summary>
    /// 簡單的事件聚合器實現
    /// </summary>
    public class SimpleEventAggregator : ISimpleEventAggregator
    {
        private readonly Dictionary<Type, List<object>> _handlers = new Dictionary<Type, List<object>>();

        public void Publish<T>(T eventObject) where T : class
        {
            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out List<object> handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is Action<T> typedHandler)
                    {
                        typedHandler(eventObject);
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<object>();
            }
            _handlers[eventType].Add(handler);
        }
    }

    /// <summary>
    /// 簡單的通知服務實現
    /// </summary>
    public class SimpleNotificationService : ISimpleNotificationService
    {
        public void ShowNotification(string title, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{title}] {message}");
        }

        public void ShowError(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[Error] {message}");
        }

        public void ShowWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[Warning] {message}");
        }

        public void ShowInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[Info] {message}");
        }
    }
}
