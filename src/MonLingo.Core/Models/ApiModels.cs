using System;

namespace MonLingo.Core.Models
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    // 相容舊版呼叫：UserService 會傳入 DeviceId
    public string DeviceId { get; set; }
    }

    public class SmsLoginRequest
    {
        public string PhoneNumber { get; set; }
        public string Phone { get; set; }
        public string VerificationCode { get; set; }
    // 相容舊版呼叫：UserService 會傳入 DeviceId
    public string DeviceId { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string SessionToken { get; set; }
        public User User { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class LicenseValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public AccountLevel AccountLevel { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime ExpireTime { get; set; }
        public string LicenseKey { get; set; }
    }
}
