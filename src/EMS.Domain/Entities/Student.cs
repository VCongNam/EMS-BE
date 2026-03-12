using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Entities
{
    public class Student
    {
        public Guid StudentID { get; set; }
        public string ParentName { get; set; }
        public string ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public string? Address { get; set; }
        public DateTime DOB { get; set; }

        public virtual Account Account { get; set; }

        public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; }
    }
}
