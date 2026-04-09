using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class Transaction
{
    public Guid TransactionId { get; set; }

    public Guid InvoiceId { get; set; }

    public decimal AmountPaid { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? ProofImageUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? PaidDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? ApprovedBy { get; set; }

    public string? Note { get; set; }

    public virtual Account? ApprovedByNavigation { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
