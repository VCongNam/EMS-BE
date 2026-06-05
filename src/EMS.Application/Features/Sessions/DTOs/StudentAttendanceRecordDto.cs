using System;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class StudentAttendanceRecordDto
    {
        public Guid SessionId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid? AttendanceId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool? IsExcused { get; set; }
        public string? Note { get; set; }
    }
}
