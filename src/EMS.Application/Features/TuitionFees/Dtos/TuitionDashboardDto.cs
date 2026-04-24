using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class TuitionDashboardDto
    {
        public decimal TotalExpected { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDebt { get; set; }

        public List<ClassRevenueDto> ProportionByClass { get; set; } = new();

        public List<DailyRevenueDto> DailyTrend { get; set; } = new();
    }
}
