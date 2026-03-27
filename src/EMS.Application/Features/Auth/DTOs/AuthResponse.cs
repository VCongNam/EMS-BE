using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.DTOs
{
    public class AuthResponse
    {
        public Guid AccountId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string RoleName { get; set; } = null!; // Ví dụ: "Teacher", "Student", "Admin"
        public string? AvatarUrl { get; set; } // Tuỳ chọn, rất tốt để hiện trên thanh Header của React
        public string Token { get; set; } = null!; // Chuỗi JWT
    }
}
