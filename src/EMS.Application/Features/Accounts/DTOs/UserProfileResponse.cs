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
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
