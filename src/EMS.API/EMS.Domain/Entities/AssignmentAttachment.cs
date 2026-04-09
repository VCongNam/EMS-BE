using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class AssignmentAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid AssignmentId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public long? FileSize { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Assignment Assignment { get; set; } = null!;
}
