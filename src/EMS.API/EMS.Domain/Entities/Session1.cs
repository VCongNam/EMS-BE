using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class Session1
{
    public Guid SessionId { get; set; }

    public Guid ClassId { get; set; }

    public string? Title { get; set; }

    public DateOnly Date { get; set; }

    public string? MeetingLink { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Topic { get; set; }

    public string? Note { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;
}
