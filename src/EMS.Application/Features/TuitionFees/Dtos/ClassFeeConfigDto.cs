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
        public string BillingMethod { get; set; } = "Postpaid"; // Prepaid hoặc Postpaid
        public decimal TuitionFee { get; set; } // Đơn giá 1 buổi
        public int PaymentDeadlineDays { get; set; } // Hạn nộp (VD: 5 ngày)
    }
}
