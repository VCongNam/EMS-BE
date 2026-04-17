using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class EnrolledClassDetailDto
    {
        public Guid ClassID { get; set; }
        public string ClassName { get; set; }
        public string TeacherName { get; set; }
        public int PendingAssignmentsCount { get; set; }
        public double AttendanceRate { get; set; }
    }
}
