using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class ProgressReport
{
    public Guid ReportId { get; set; }

    public Guid StudentId { get; set; }

    public Guid ClassId { get; set; }

    public Guid TeacherId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? PeriodMonth { get; set; }

    public int? PeriodYear { get; set; }

    public decimal? Gpa { get; set; }

    public decimal? AttendanceRate { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
