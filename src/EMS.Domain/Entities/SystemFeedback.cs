using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class SystemFeedback
{
    public Guid FeedbackId { get; set; }

    public Guid SenderId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Type { get; set; } 

    public string? Status { get; set; }

    public string? AdminReply { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Sender { get; set; } = null!;
}
