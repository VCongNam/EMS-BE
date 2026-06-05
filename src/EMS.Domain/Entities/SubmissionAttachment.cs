using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class SubmissionAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid SubmissionId { get; set; }

    public string? FileUrl { get; set; }

    public string? FileType { get; set; }

    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string FileRole { get; set; } = null!;

    public virtual Submission Submission { get; set; } = null!;
}
