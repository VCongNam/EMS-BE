using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Attendance
{
    public Guid AttendanceId { get; set; }

    public Guid SessionId { get; set; }

    public Guid StudentId { get; set; }

    public string Status { get; set; } = null!;

    public bool? IsExcused { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? InvoiceId { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual Session Session { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
