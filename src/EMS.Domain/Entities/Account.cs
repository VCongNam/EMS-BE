using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Entities
{
    public class Account
    {
        public Guid AccountId { get; set; }
        public Guid RoleId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public Role Role { get; set; } = null!;
    }
}
