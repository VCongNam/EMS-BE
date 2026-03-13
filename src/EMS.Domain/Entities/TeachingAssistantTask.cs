using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class TeachingAssistantTask
{
    public Guid TataskId { get; set; }

    public Guid Taid { get; set; }

    public Guid TeacherId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime DueDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual TeachingAssistant Ta { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
