using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class TransactionDetailDto
    {
        public Guid TransactionId { get; set; }
        public Guid InvoiceId { get; set; }

        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public decimal AmountPaid { get; set; }      
        public decimal InvoiceTotalAmount { get; set; } 
        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime PaidDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ProofImageUrl { get; set; }
        public string? Note { get; set; } 

        public string InvoicePeriod { get; set; } = string.Empty; 
    }
}
