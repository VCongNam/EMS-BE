using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class TuitionSummaryDto
    {
        public decimal ExpectedRevenue { get; set; } // Tổng doanh thu dự kiến (Card 1)
        public decimal ActualRevenue { get; set; }   // Tổng thực thu (Card 2)
        public decimal DebtAmount { get; set; }      // Tổng công nợ (Card 3)
    }
}
