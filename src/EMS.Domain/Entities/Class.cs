using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Class
{
    public Guid ClassId { get; set; }

    public Guid TeacherId { get; set; }

    public string ClassName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TuitionFee { get; set; }

    public string? Room { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public short? MaxStudents { get; set; }

    public Guid SubjectId { get; set; }
    public string? BillingMethod { get; set; }
    public string? BillingCycle { get; set; }
    public string? TuitionNote { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();

    public virtual ICollection<ClassTum> ClassTa { get; set; } = new List<ClassTum>();

    public virtual ICollection<GradeCategory> GradeCategories { get; set; } = new List<GradeCategory>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<LearningMaterial> LearningMaterials { get; set; } = new List<LearningMaterial>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
