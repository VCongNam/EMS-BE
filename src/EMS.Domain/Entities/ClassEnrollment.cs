using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Entities
{
    public class ClassEnrollment
    {
        public Guid EnrollmentID { get; set; } 
        public Guid ClassID { get; set; } 
        public Guid StudentID { get; set; } 
        public DateTime? EnrolledDate { get; set; }
        public DateTime? DroppedDate { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Class Class { get; set; }
        public virtual Student Student { get; set; }
    }
}
