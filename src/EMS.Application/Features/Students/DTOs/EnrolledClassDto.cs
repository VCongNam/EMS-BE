using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class EnrolledClassDto
    {
        public Guid ClassID { get; set; }
        public string ClassName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string TeacherName { get; set; }
        public string EnrollmentStatus { get; set; }
        public DateOnly EnrolledDate { get; set; }
        public string ClassStatus { get; set; }
    }

    public class EnrolledClassFilter
    {
        public int Page { get; set; }
        public int Size { get; set; }
    }
}
