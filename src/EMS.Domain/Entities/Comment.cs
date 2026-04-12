using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Comment
{
    public Guid CommentId { get; set; }

    public Guid PostId { get; set; }

    public Guid AuthorId { get; set; }

        public Guid? StudentId { get; set; }

    public string Content { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Author { get; set; } = null!;

    public virtual Student? Student { get; set; }

    public virtual Post Post { get; set; } = null!;
}
