using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class TransactionHistoryDto
    {
        public Guid TransactionId { get; set; }
        public Guid InvoiceId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ProofImageUrl { get; set; }
        public string Status { get; set; } = string.Empty; // Approved hoặc Rejected
        public string? ReviewerNote { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
