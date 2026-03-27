using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.DTOs
{
    public class UserProfileResponse
    {
        public Guid AccountId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public object? RoleSpecificData { get; set; }

    }
}
