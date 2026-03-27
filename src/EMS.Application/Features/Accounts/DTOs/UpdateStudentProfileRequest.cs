using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.DTOs
{
    public class UpdateStudentProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string ParentName { get; set; } = null!;
        public string ParentPhone { get; set; } = null!;
        public string? ParentEmail { get; set; }
        public string? Address { get; set; }
        public DateOnly Dob { get; set; }
    }
}
