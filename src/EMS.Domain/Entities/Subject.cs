using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Subject
{
    public Guid SubjectId { get; set; }

    public string SubjectName { get; set; } = null!;

    public short GradeLevel { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
