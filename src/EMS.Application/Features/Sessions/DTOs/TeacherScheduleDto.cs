using System;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class TeacherScheduleDto
    {
        public Guid SessionId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string? Title { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Room { get; set; }
        public string? MeetingLink { get; set; }
        public string? Status { get; set; }
    }
}
