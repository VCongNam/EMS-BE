using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class ClassMemberResponse
    {
        public Guid StudentID { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }    
        public string Email { get; set; }    
        public DateOnly? EnrolledDate { get; set; } 
        public string Status { get; set; }
    }
}
