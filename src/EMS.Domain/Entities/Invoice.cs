using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities;

public partial class Invoice
{
    public Guid InvoiceId { get; set; }

    public Guid StudentId { get; set; }

    public Guid ClassId { get; set; }

    public short PeriodMonth { get; set; }

    public int PeriodYear { get; set; }

    public decimal Amount { get; set; }

    public DateTime DueDate { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }
    public int? SessionCount { get; set; }
    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
