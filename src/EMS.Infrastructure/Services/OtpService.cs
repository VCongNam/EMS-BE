using EMS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        public string GenerateOtp()
        {
            // Tạo mã 6 số bảo mật
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        public string HashOtp(string otp)
        {
            // Hash mã OTP bằng BCrypt
            return BCrypt.Net.BCrypt.HashPassword(otp);
        }

        public bool VerifyOtp(string plainOtp, string hashedOtp)
        {
            // So khớp mã người dùng nhập với mã đã Hash trong DB
            return BCrypt.Net.BCrypt.Verify(plainOtp, hashedOtp);
        }
    }
}
