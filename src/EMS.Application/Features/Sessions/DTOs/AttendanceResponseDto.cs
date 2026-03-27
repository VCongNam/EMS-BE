using System;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class AttendanceResponseDto
    {
        public Guid AttendanceId { get; set; }
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool? IsExcused { get; set; }
        public string? Note { get; set; }
    }
}
