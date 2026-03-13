using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class ClassTum
{
    public Guid ClassTaid { get; set; }

    public Guid ClassId { get; set; }

    public Guid Taid { get; set; }

    public string Permission { get; set; } = null!;

    public decimal? SalaryPerSession { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual TeachingAssistant Ta { get; set; } = null!;
}
