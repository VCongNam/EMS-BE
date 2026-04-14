using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class UpdateClassFeeConfigDto
    {
        public string BillingMethod { get; set; } = string.Empty;
        public decimal TuitionFee { get; set; }
        public int PaymentDeadlineDays { get; set; }
    }
}
