using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Post
{
    public Guid PostId { get; set; }

    public Guid ClassId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = null!;

    public string? AttachmentUrl { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Author { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
