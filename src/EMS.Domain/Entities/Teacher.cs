using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Teacher
{
    public Guid TeacherId { get; set; }

    public string? Bio { get; set; }

    public string? Specialization { get; set; }

    public string? BankName { get; set; }

    public string? BankAccount { get; set; }

    public string? BankAccountName { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();

    public virtual Account TeacherNavigation { get; set; } = null!;

    public virtual ICollection<TeachingAssistantTask> TeachingAssistantTasks { get; set; } = new List<TeachingAssistantTask>();
}
