using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class PostAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid PostId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public long? FileSize { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Post Post { get; set; } = null!;
}
