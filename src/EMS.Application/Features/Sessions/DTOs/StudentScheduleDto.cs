using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class StudentScheduleDto
    {
        public Guid SessionID { get; set; }
        public Guid ClassID { get; set; }
        public string ClassName { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly? StartTime { get; set; } 
        public TimeOnly? EndTime { get; set; }
        public string? MeetingLink { get; set; } 
        public string Status { get; set; }

        public string AttendanceStatus { get; set; }
    }

    public class ScheduleFilter
    {
        public DateTime FromDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime ToDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);
        public Guid? ClassID { get; set; }
    }
}
