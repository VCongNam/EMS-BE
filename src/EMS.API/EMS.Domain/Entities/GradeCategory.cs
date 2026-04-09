using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class GradeCategory
{
    public Guid GradeCategoryId { get; set; }

    public Guid ClassId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Weight { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual Class Class { get; set; } = null!;
}
