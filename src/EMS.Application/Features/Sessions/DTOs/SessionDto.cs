using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Sessions.DTOs
{
    public class SessionDto
    {
        public Guid SessionId { get; set; }
        public Guid ClassId { get; set; }
        public string? Title { get; set; }
        public DateOnly Date { get; set; }
        public string? MeetingLink { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
