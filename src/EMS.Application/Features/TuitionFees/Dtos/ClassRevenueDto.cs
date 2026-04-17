using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassRevenueDto
    {
        public string ClassName { get; set; } = string.Empty;
        public decimal Revenue { get; set; } // Tổng doanh thu thực tế (Paid)
    }
}
