using System;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class TakeAttendanceDto
    {
        public Guid StudentId { get; set; }
        public string Status { get; set; } = "Present";
        public bool? IsExcused { get; set; }
        public string? Note { get; set; }
    }
}
