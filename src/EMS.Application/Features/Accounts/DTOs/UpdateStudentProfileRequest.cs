using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.DTOs
{
    public class UpdateStudentProfileRequest
    {
        public string StudentFullName { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly Dob { get; set; }

        public string ParentFullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string ParentEmail { get; set; } = null!;
    }
}
