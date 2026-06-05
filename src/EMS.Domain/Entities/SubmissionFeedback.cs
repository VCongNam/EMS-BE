using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class SubmissionFeedback
{
    public Guid FeedbackId { get; set; }

    public Guid SubmissionId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Author { get; set; } = null!;

    public virtual Submission Submission { get; set; } = null!;
}
