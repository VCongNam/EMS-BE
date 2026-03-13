using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Assignment
{
    public Guid AssignmentId { get; set; }

    public Guid ClassId { get; set; }

    public Guid AuthorId { get; set; }

    public Guid GradeCategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? AttachmentPath { get; set; }

    public DateTime DueDate { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Author { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual GradeCategory GradeCategory { get; set; } = null!;

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
