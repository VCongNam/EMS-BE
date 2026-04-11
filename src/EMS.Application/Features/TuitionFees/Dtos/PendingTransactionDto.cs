using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class PendingTransactionDto
    {
        public Guid TransactionId { get; set; }
        public string StudentName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public decimal AmountPaid { get; set; }
        public string? ProofImageURL { get; set; }
        public DateTime PaidDate { get; set; }
    }
}
