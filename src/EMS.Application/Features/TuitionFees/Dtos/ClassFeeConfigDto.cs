using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassFeeConfigDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string BillingMethod { get; set; } = "Postpaid"; 
        public decimal TuitionFee { get; set; } 
        public int PaymentDeadlineDays { get; set; } 
    }
}
