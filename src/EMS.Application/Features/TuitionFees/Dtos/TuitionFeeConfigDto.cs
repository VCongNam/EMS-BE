using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class TuitionFeeConfigDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public bool IsInvoiceGeneratedThisMonth { get; set; } // Cờ hiệu kiểm tra
        public string BillingMethod { get; set; } = null!; // Prepaid / Postpaid
        public decimal PricePerSession { get; set; }
        public int StudentCount { get; set; }
    }
}
