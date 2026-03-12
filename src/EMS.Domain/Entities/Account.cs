using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Entities
{
    public class Account
    {
        public Guid AccountID { get; set; }
        public Guid RoleID { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarURL { get; set; }
        public string? Status { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Student Student { get; set; }
    }
}
