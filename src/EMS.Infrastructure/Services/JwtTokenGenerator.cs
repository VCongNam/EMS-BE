using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(Account account, string roleName, bool isTempToken = false, Guid? studentId = null)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));

            // 1. Khởi tạo danh sách Claims cơ bản
            var claims = new List<Claim>
            {
                // Standard and framework-friendly claims
                new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString()),
                // Also add ClaimTypes.NameIdentifier so CurrentUserService can read UserId reliably
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                // Add both registered email claim and ClaimTypes.Email for compatibility
                new Claim(JwtRegisteredClaimNames.Email, account.Email ?? ""),
                new Claim(ClaimTypes.Email, account.Email ?? string.Empty),
                new Claim("FullName", account.FullName),
                new Claim(ClaimTypes.Role, roleName),
                
                // TokenType: "Temp" (chỉ để chọn profile) hoặc "Main" (để dùng mọi API)
                new Claim("TokenType", isTempToken ? "Temp" : "Main")
            };

            // 2. Nếu là MainToken của Student, nhét StudentId vào để Authorization
            if (!isTempToken && studentId.HasValue)
            {
                claims.Add(new Claim("StudentId", studentId.Value.ToString()));
            }

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Thiết lập thời gian hết hạn (Token tạm nên hết hạn nhanh hơn, ví dụ 5-10 phút)
            int expiryMinutes = isTempToken ? 10 : int.Parse(jwtSettings["ExpiryMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
