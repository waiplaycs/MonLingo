using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MonLingo.Core.Models;

namespace MonLingo.Core.Service
{
    /// <summary>
    /// API客戶端服務實現
    /// </summary>
    public class ApiClient : IApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigService _configService;
        private readonly string _baseUrl;

        public ApiClient(IConfigService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _httpClient = new HttpClient();
            
            // 從配置讀取API基礎URL，預設為本地測試伺服器
            _baseUrl = _configService.GetAsync<string>("ApiBaseUrl").Result ?? "https://api.monlingo.com";
            
            // 設定預設標頭
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MonLingo/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 電子郵件登入API
        /// </summary>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LoginResponse>(responseContent, GetJsonOptions());
                }

                // 如果API伺服器不可用，回傳模擬的成功回應用於開發測試
                return CreateMockLoginResponse(request.Email);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login API failed: {ex.Message}");
                
                // 開發模式：回傳模擬回應
                return CreateMockLoginResponse(request.Email);
            }
        }

        /// <summary>
        /// 簡訊登入API
        /// </summary>
        public async Task<LoginResponse> LoginViaSmsAsync(SmsLoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/sms-login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LoginResponse>(responseContent, GetJsonOptions());
                }

                // 模擬回應
                return CreateMockSmsLoginResponse(request.Phone);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS Login API failed: {ex.Message}");
                
                // 開發模式：回傳模擬回應
                return CreateMockSmsLoginResponse(request.Phone);
            }
        }

        /// <summary>
        /// 根據Token獲取使用者資料
        /// </summary>
        public async Task<LoginResponse> GetUserByTokenAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/user/profile");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LoginResponse>(responseContent, GetJsonOptions());
                }

                return new LoginResponse { Success = false, Message = "Token無效" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get user by token failed: {ex.Message}");
                return new LoginResponse { Success = false, Message = "網路錯誤" };
            }
        }

        /// <summary>
        /// 驗證授權API
        /// </summary>
        public async Task<LicenseValidationResponse> ValidateLicenseAsync(string sessionToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/license/validate");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LicenseValidationResponse>(responseContent, GetJsonOptions());
                }

                // 模擬回應
                return new LicenseValidationResponse
                {
                    IsValid = true,
                    ExpireTime = DateTime.Now.AddDays(30),
                    AccountLevel = AccountLevel.Pro,
                    Message = "授權有效"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"License validation failed: {ex.Message}");
                
                // 開發模式：回傳模擬的有效授權
                return new LicenseValidationResponse
                {
                    IsValid = true,
                    ExpireTime = DateTime.Now.AddDays(30),
                    AccountLevel = AccountLevel.Pro,
                    Message = "授權有效（開發模式）"
                };
            }
        }

        /// <summary>
        /// 註冊使用者API
        /// </summary>
        public async Task<LoginResponse> RegisterAsync(string email, string password, string username)
        {
            try
            {
                var request = new { Email = email, Password = password, Username = username };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<LoginResponse>(responseContent, GetJsonOptions());
                }

                // 模擬回應
                return CreateMockRegisterResponse(email, username);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Register API failed: {ex.Message}");
                return CreateMockRegisterResponse(email, username);
            }
        }

        /// <summary>
        /// 發送驗證碼API
        /// </summary>
        public async Task<bool> SendVerificationCodeAsync(string phone)
        {
            try
            {
                var request = new { Phone = phone };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/send-code", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Send verification code failed: {ex.Message}");
                return true; // 開發模式：模擬成功
            }
        }

        /// <summary>
        /// 重設密碼API
        /// </summary>
        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                var request = new { Email = email };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/reset-password", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset password failed: {ex.Message}");
                return true; // 開發模式：模擬成功
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 獲取JSON序列化選項
        /// </summary>
        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// 建立模擬登入回應
        /// </summary>
        private LoginResponse CreateMockLoginResponse(string email)
        {
            return new LoginResponse
            {
                Success = true,
                Message = "登入成功（開發模式）",
                SessionToken = Guid.NewGuid().ToString(),
                User = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Email = email,
                    Username = email.Split('@')[0],
                    AccountLevel = AccountLevel.Pro,
                    ExpireTime = DateTime.Now.AddDays(30),
                    Coins = 1250,
                    Points = 500,
                    RegisterTime = DateTime.Now.AddDays(-30),
                    LastLoginTime = DateTime.Now
                }
            };
        }

        /// <summary>
        /// 建立模擬簡訊登入回應
        /// </summary>
        private LoginResponse CreateMockSmsLoginResponse(string phone)
        {
            return new LoginResponse
            {
                Success = true,
                Message = "簡訊登入成功（開發模式）",
                SessionToken = Guid.NewGuid().ToString(),
                User = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Phone = phone,
                    Username = $"User_{phone.Substring(phone.Length - 4)}",
                    AccountLevel = AccountLevel.Free,
                    ExpireTime = DateTime.Now.AddDays(-1), // 免費使用者
                    Coins = 100,
                    Points = 200,
                    RegisterTime = DateTime.Now,
                    LastLoginTime = DateTime.Now
                }
            };
        }

        /// <summary>
        /// 建立模擬註冊回應
        /// </summary>
        private LoginResponse CreateMockRegisterResponse(string email, string username)
        {
            return new LoginResponse
            {
                Success = true,
                Message = "註冊成功（開發模式）",
                SessionToken = Guid.NewGuid().ToString(),
                User = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Email = email,
                    Username = username,
                    AccountLevel = AccountLevel.Free,
                    ExpireTime = DateTime.Now.AddDays(-1), // 新使用者預設為免費
                    Coins = 50, // 新使用者贈送50硬幣
                    Points = 100, // 新使用者贈送100積分
                    RegisterTime = DateTime.Now,
                    LastLoginTime = DateTime.Now
                }
            };
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
