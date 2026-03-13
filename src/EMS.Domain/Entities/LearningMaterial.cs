using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class LearningMaterial
{
    public Guid MaterialId { get; set; }

    public Guid ClassId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string FileUrl { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;
}
