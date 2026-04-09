using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class LearningMaterial
{
    public Guid MaterialId { get; set; }

    public Guid ClassId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid AuthorId { get; set; }

    public virtual Account Author { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<MaterialAttachment> MaterialAttachments { get; set; } = new List<MaterialAttachment>();
}
