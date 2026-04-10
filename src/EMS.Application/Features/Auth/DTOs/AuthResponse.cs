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
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool RequiresProfileSelection { get; set; }
        public string? Token { get; set; } // Main Token
        public string? TempToken { get; set; } // Dùng cho bước chọn Profile
        public string? Status { get; set; }
        public List<StudentProfileDto>? AvailableProfiles { get; set; }
    }
}
