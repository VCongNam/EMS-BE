using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class TeachingAssistantTask
{
    public Guid TataskId { get; set; }

    public Guid ClassTaid { get; set; }

    public string Title { get; set; } = null!;

    public DateTime DueDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Type { get; set; }

    public virtual ClassTum ClassTa { get; set; } = null!;
}
