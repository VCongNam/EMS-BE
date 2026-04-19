using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class ClassAttendanceHistoryDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public List<StudentAttendanceRecordDto> Attendances { get; set; } = new();
    }
}
