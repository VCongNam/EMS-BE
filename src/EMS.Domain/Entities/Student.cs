using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Student
{
    public Guid StudentId { get; set; }

    public string? Address { get; set; }

    public DateOnly Dob { get; set; }

    public Guid AccountId { get; set; }

    public string? FullName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
