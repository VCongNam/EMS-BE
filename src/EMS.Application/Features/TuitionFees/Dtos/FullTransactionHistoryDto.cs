using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class FullTransactionHistoryDto
    {
        public Guid TransactionId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public string? ProofImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } 
        public Guid InvoiceId { get; set; }
        public decimal InvoiceTotalAmount { get; set; }
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public string? InvoiceDescription { get; set; }
        public decimal InvoiceUnitPrice { get; set; }
        public int InvoiceSessionCount { get; set; }

        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;

        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }
}
