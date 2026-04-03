using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Submission
{
    public Guid SubmissionId { get; set; }

    public Guid AssignmentId { get; set; }

    public Guid StudentId { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? Status { get; set; }

    public decimal? Grade { get; set; }

    public virtual Assignment Assignment { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual ICollection<SubmissionAttachment> SubmissionAttachments { get; set; } = new List<SubmissionAttachment>();

    public virtual ICollection<SubmissionFeedback> SubmissionFeedbacks { get; set; } = new List<SubmissionFeedback>();
}
