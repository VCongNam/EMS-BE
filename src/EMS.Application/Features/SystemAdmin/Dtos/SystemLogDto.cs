using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Dtos
{
    public class SystemLogDto
    {
        public Guid LogId { get; set; }
        public Guid? AccountId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string ActionType { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
