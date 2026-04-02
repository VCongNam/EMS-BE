using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Dtos
{
    public class AccountDetailDto
    {
        public Guid AccountId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string RoleName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public int TotalOwnedClasses { get; set; }
    }
}
