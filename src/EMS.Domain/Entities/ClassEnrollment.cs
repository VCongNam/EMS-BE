using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class ClassEnrollment
{
    public Guid EnrollmentId { get; set; }

    public Guid ClassId { get; set; }

    public Guid StudentId { get; set; }

    public DateOnly? EnrolledDate { get; set; }

    public DateOnly? DroppedDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal? CreditBalance { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
