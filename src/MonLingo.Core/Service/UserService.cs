using System;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// 使用者服務實現（PRD §11.1）
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IConfigService _configService;
        private readonly IApiClient _apiClient;
        private User _currentUser;

        public UserService(IConfigService configService, IApiClient apiClient)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// 電子郵件登入（PRD §11.1）
        /// </summary>
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Email = email,
                    Password = password,
                    DeviceId = GetDeviceId()
                };

                var response = await _apiClient.LoginAsync(loginRequest);

                if (response?.Success == true && response.User != null)
                {
                    _currentUser = response.User;
                    _currentUser.SessionToken = response.SessionToken;
                    _currentUser.LastLoginTime = DateTime.Now;

                    await SaveUserTokenAsync(response.SessionToken);
                    await SaveCurrentUserAsync(_currentUser);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // 登入錯誤處理 - 記錄錯誤但不拋出例外
                System.Diagnostics.Debug.WriteLine($"Login failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 手機簡訊登入（PRD §11.1）
        /// </summary>
        public async Task<bool> LoginViaSmsAsync(string phone, string code)
        {
            try
            {
                var smsLoginRequest = new SmsLoginRequest
                {
                    Phone = phone,
                    VerificationCode = code,
                    DeviceId = GetDeviceId()
                };

                var response = await _apiClient.LoginViaSmsAsync(smsLoginRequest);

                if (response?.Success == true && response.User != null)
                {
                    _currentUser = response.User;
                    _currentUser.SessionToken = response.SessionToken;
                    _currentUser.LastLoginTime = DateTime.Now;

                    await SaveUserTokenAsync(response.SessionToken);
                    await SaveCurrentUserAsync(_currentUser);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS login failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 獲取當前使用者資料（同步）
        /// </summary>
        public User GetCurrentUser()
        {
            return _currentUser;
        }

        /// <summary>
        /// 獲取當前使用者資料（非同步）
        /// </summary>
        public async Task<User> GetCurrentUserAsync()
        {
            if (_currentUser == null)
            {
                await LoadUserFromStorageAsync();
            }
            return _currentUser;
        }

        /// <summary>
        /// 更新使用者資料
        /// </summary>
        public async Task UpdateUserAsync(User user)
        {
            _currentUser = user;
            await SaveCurrentUserAsync(user);
        }

        /// <summary>
        /// 登出
        /// </summary>
        public async Task LogoutAsync()
        {
            _currentUser = null;
            await _configService.RemoveAsync("UserToken");
            await _configService.RemoveAsync("CurrentUser");
        }

        /// <summary>
        /// 註冊新使用者
        /// </summary>
        public async Task<bool> RegisterAsync(string email, string password, string username)
        {
            try
            {
                var response = await _apiClient.RegisterAsync(email, password, username);

                if (response?.Success == true && response.User != null)
                {
                    // 註冊成功後自動登入
                    _currentUser = response.User;
                    _currentUser.SessionToken = response.SessionToken;
                    _currentUser.RegisterTime = DateTime.Now;
                    _currentUser.LastLoginTime = DateTime.Now;

                    await SaveUserTokenAsync(response.SessionToken);
                    await SaveCurrentUserAsync(_currentUser);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registration failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 發送驗證碼
        /// </summary>
        public async Task<bool> SendVerificationCodeAsync(string phone)
        {
            try
            {
                return await _apiClient.SendVerificationCodeAsync(phone);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Send verification code failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重設密碼
        /// </summary>
        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                return await _apiClient.ResetPasswordAsync(email);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset password failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 檢查登入狀態
        /// </summary>
        public bool IsLoggedIn()
        {
            return _currentUser != null && !string.IsNullOrEmpty(_currentUser.SessionToken);
        }

        #region 私有輔助方法

        /// <summary>
        /// 儲存使用者 Token
        /// </summary>
        private async Task SaveUserTokenAsync(string token)
        {
            await _configService.SaveAsync("UserToken", token);
        }

        /// <summary>
        /// 儲存當前使用者資料
        /// </summary>
        private async Task SaveCurrentUserAsync(User user)
        {
            await _configService.SaveAsync("CurrentUser", user);
        }

        /// <summary>
        /// 從儲存載入使用者資料
        /// </summary>
        private async Task LoadUserFromStorageAsync()
        {
            try
            {
                // 先嘗試從本地配置載入
                _currentUser = await _configService.GetAsync<User>("CurrentUser");

                // 如果本地沒有，嘗試從Token載入
                if (_currentUser == null)
                {
                    await LoadUserFromTokenAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load user from storage failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 從 Token 載入使用者資料
        /// </summary>
        private async Task LoadUserFromTokenAsync()
        {
            var token = await _configService.GetAsync<string>("UserToken");
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var response = await _apiClient.GetUserByTokenAsync(token);
                    if (response?.Success == true && response.User != null)
                    {
                        _currentUser = response.User;
                        _currentUser.SessionToken = token;
                        await SaveCurrentUserAsync(_currentUser);
                    }
                }
                catch
                {
                    // Token 無效，需要重新登入
                    await _configService.RemoveAsync("UserToken");
                }
            }
        }

        /// <summary>
        /// 獲取裝置 ID
        /// </summary>
        private string GetDeviceId()
        {
            // 基於硬體資訊生成唯一裝置 ID
            var machineName = Environment.MachineName ?? "Unknown";
            var userName = Environment.UserName ?? "Unknown";
            return $"{machineName}_{userName}_{Environment.OSVersion.Platform}";
        }

        #endregion
    }
}
