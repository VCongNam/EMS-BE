using System;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class UpdateAttendanceDto
    {
        public string Status { get; set; } = string.Empty;
        public bool? IsExcused { get; set; }
        public string? Note { get; set; }
    }
}
