using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class CreateStudentRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }

        public string ParentName { get; set; }
        public string ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public string? Address { get; set; }
        public DateTime DOB { get; set; }

        public Guid RoleID { get; set; }
    }
}
