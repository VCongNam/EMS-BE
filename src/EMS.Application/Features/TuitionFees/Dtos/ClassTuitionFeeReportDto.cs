using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassTuitionReportDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string BillingMethod { get; set; } = string.Empty; // Thu trước / Thu sau
        public decimal TuitionFee { get; set; } // Đơn giá buổi học
        public int StudentCount { get; set; }   // Sĩ số (Số học sinh đang Active)
        public double CollectionRate { get; set; } // Tỉ lệ thu (%) của kỳ này
    }
}
