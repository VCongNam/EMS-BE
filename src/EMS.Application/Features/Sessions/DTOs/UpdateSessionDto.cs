using System;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class UpdateSessionDto
    {
        public string? Title { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? MeetingLink { get; set; }
        public string? Topic { get; set; }
        public string? Note { get; set; }
    }
}
