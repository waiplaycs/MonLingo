using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 熱鍵服務介面
    /// </summary>
    public interface IHotkeyService
    {
        /// <summary>
        /// 註冊熱鍵
        /// </summary>
        Task<bool> RegisterHotkeyAsync(string hotkeyString, Action callback);

        /// <summary>
        /// 取消註冊熱鍵
        /// </summary>
        void UnregisterHotkey(string hotkeyString);

        /// <summary>
        /// 取消註冊所有熱鍵
        /// </summary>
        void UnregisterAll();

        /// <summary>
        /// 檢查熱鍵是否可用
        /// </summary>
        bool IsHotkeyAvailable(string hotkeyString);

        /// <summary>
        /// 熱鍵觸發事件
        /// </summary>
        event EventHandler<HotkeyEventArgs> HotkeyPressed;

        /// <summary>
        /// 初始化窗口句柄
        /// </summary>
        void Initialize(Window window);
    }

    /// <summary>
    /// 熱鍵事件參數
    /// </summary>
    public class HotkeyEventArgs : EventArgs
    {
        public string HotkeyString { get; set; }
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
    }

    /// <summary>
    /// 熱鍵資訊
    /// </summary>
    public class HotkeyInfo
    {
        public int Id { get; set; }
        public string HotkeyString { get; set; }
        public Key Key { get; set; }
        public uint Modifiers { get; set; }
        public Action Callback { get; set; }
    }

    /// <summary>
    /// 熱鍵服務實現
    /// 依據 PRD 規範實現 RegisterHotKey 整合、衝突檢測、降級策略
    /// </summary>
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly Dictionary<string, HotkeyInfo> _registeredHotkeys;
        private readonly Dictionary<int, HotkeyInfo> _hotkeyIds;
        private int _nextHotkeyId = 1;
        private bool _disposed = false;
        private IntPtr _windowHandle = IntPtr.Zero;

        // Windows API 常數
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        public event EventHandler<HotkeyEventArgs> HotkeyPressed;

        public HotkeyService()
        {
            _registeredHotkeys = new Dictionary<string, HotkeyInfo>();
            _hotkeyIds = new Dictionary<int, HotkeyInfo>();
        }

        /// <summary>
        /// 初始化窗口句柄 (需要在主線程調用)
        /// </summary>
        public void Initialize(Window window)
        {
            if (window != null)
            {
                var helper = new WindowInteropHelper(window);
                _windowHandle = helper.Handle;
                
                if (_windowHandle == IntPtr.Zero)
                {
                    helper.EnsureHandle();
                    _windowHandle = helper.Handle;
                }

                // 註冊消息處理
                var source = HwndSource.FromHwnd(_windowHandle);
                source?.AddHook(WndProc);
            }
        }

        /// <summary>
        /// 註冊熱鍵
        /// </summary>
        public async Task<bool> RegisterHotkeyAsync(string hotkeyString, Action callback)
        {
            return await Task.Run(() => RegisterHotkey(hotkeyString, callback));
        }

        /// <summary>
        /// 註冊熱鍵 (同步版本)
        /// </summary>
        private bool RegisterHotkey(string hotkeyString, Action callback)
        {
            if (string.IsNullOrEmpty(hotkeyString) || callback == null || _windowHandle == IntPtr.Zero)
                return false;

            try
            {
                // 解析熱鍵字串
                if (!ParseHotkeyString(hotkeyString, out var key, out var modifiers))
                    return false;

                // 檢查是否已註冊
                if (_registeredHotkeys.ContainsKey(hotkeyString))
                {
                    UnregisterHotkey(hotkeyString);
                }

                // 檢查熱鍵是否可用
                if (!IsHotkeyAvailable(hotkeyString))
                {
                    // 嘗試降級策略
                    var fallbackHotkey = GetFallbackHotkey(hotkeyString);
                    if (!string.IsNullOrEmpty(fallbackHotkey) && IsHotkeyAvailable(fallbackHotkey))
                    {
                        hotkeyString = fallbackHotkey;
                        ParseHotkeyString(hotkeyString, out key, out modifiers);
                    }
                    else
                    {
                        return false;
                    }
                }

                var hotkeyId = _nextHotkeyId++;
                var hotkeyInfo = new HotkeyInfo
                {
                    Id = hotkeyId,
                    HotkeyString = hotkeyString,
                    Key = key,
                    Modifiers = modifiers,
                    Callback = callback
                };

                // 註冊系統熱鍵
                if (RegisterHotKey(_windowHandle, hotkeyId, modifiers, (uint)KeyInterop.VirtualKeyFromKey(key)))
                {
                    _registeredHotkeys[hotkeyString] = hotkeyInfo;
                    _hotkeyIds[hotkeyId] = hotkeyInfo;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"註冊熱鍵失敗 [{hotkeyString}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取消註冊熱鍵
        /// </summary>
        public void UnregisterHotkey(string hotkeyString)
        {
            if (string.IsNullOrEmpty(hotkeyString) || !_registeredHotkeys.TryGetValue(hotkeyString, out var hotkeyInfo))
                return;

            try
            {
                UnregisterHotKey(_windowHandle, hotkeyInfo.Id);
                _registeredHotkeys.Remove(hotkeyString);
                _hotkeyIds.Remove(hotkeyInfo.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消註冊熱鍵失敗 [{hotkeyString}]: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消註冊所有熱鍵
        /// </summary>
        public void UnregisterAll()
        {
            try
            {
                foreach (var hotkeyInfo in _hotkeyIds.Values)
                {
                    UnregisterHotKey(_windowHandle, hotkeyInfo.Id);
                }

                _registeredHotkeys.Clear();
                _hotkeyIds.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消註冊所有熱鍵失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 檢查熱鍵是否可用
        /// </summary>
        public bool IsHotkeyAvailable(string hotkeyString)
        {
            if (_windowHandle == IntPtr.Zero || !ParseHotkeyString(hotkeyString, out var key, out var modifiers))
                return false;

            var testId = 0xBEEF; // 測試用 ID
            var available = RegisterHotKey(_windowHandle, testId, modifiers, (uint)KeyInterop.VirtualKeyFromKey(key));
            
            if (available)
            {
                UnregisterHotKey(_windowHandle, testId);
            }

            return available;
        }

        /// <summary>
        /// 解析熱鍵字串
        /// </summary>
        private bool ParseHotkeyString(string hotkeyString, out Key key, out uint modifiers)
        {
            key = Key.None;
            modifiers = 0;

            try
            {
                var parts = hotkeyString.Split('+');
                if (parts.Length == 0)
                    return false;

                // 解析修飾鍵
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var modifier = parts[i].Trim().ToLower();
                    switch (modifier)
                    {
                        case "ctrl":
                        case "control":
                            modifiers |= MOD_CONTROL;
                            break;
                        case "alt":
                            modifiers |= MOD_ALT;
                            break;
                        case "shift":
                            modifiers |= MOD_SHIFT;
                            break;
                        case "win":
                        case "windows":
                            modifiers |= MOD_WIN;
                            break;
                        default:
                            return false;
                    }
                }

                // 解析主鍵
                var keyString = parts[parts.Length - 1].Trim().ToUpper();
                if (!Enum.TryParse<Key>(keyString, out key))
                {
                    // 嘗試特殊鍵名對應
                    switch (keyString)
                    {
                        case "SPACE":
                            key = Key.Space;
                            break;
                        case "ESC":
                        case "ESCAPE":
                            key = Key.Escape;
                            break;
                        case "ENTER":
                        case "RETURN":
                            key = Key.Return;
                            break;
                        default:
                            return false;
                    }
                }

                return key != Key.None;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 獲取降級熱鍵
        /// </summary>
        private string GetFallbackHotkey(string originalHotkey)
        {
            // 降級策略：依序嘗試不同修飾鍵組合
            var fallbackStrategies = new[]
            {
                "Ctrl+Alt+T",
                "Ctrl+Shift+M",
                "Alt+Shift+T",
                "Ctrl+Win+T",
                "F12",
                "F11",
                "F10"
            };

            foreach (var fallback in fallbackStrategies)
            {
                if (fallback != originalHotkey && IsHotkeyAvailable(fallback))
                {
                    return fallback;
                }
            }

            return null;
        }

        /// <summary>
        /// 處理 Windows 消息
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                var hotkeyId = wParam.ToInt32();
                OnHotkeyPressed(hotkeyId);
                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 處理熱鍵觸發
        /// </summary>
        private void OnHotkeyPressed(int hotkeyId)
        {
            if (_hotkeyIds.TryGetValue(hotkeyId, out var hotkeyInfo))
            {
                try
                {
                    hotkeyInfo.Callback?.Invoke();

                    HotkeyPressed?.Invoke(this, new HotkeyEventArgs
                    {
                        HotkeyString = hotkeyInfo.HotkeyString,
                        Key = hotkeyInfo.Key,
                        Modifiers = (ModifierKeys)hotkeyInfo.Modifiers
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"執行熱鍵回調失敗 [{hotkeyInfo.HotkeyString}]: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAll();
                _disposed = true;
            }
        }

        // Windows API
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
