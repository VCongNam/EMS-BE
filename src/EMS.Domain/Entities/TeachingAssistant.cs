using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class TeachingAssistant
{
    public Guid Taid { get; set; }

    public string? Bio { get; set; }

    public string? BankName { get; set; }

    public string? BankAccount { get; set; }

    public string? BankAccountName { get; set; }

    public virtual ICollection<ClassTum> ClassTa { get; set; } = new List<ClassTum>();

    public virtual Account Ta { get; set; } = null!;

    public virtual ICollection<TeachingAssistantTask> TeachingAssistantTasks { get; set; } = new List<TeachingAssistantTask>();
}
