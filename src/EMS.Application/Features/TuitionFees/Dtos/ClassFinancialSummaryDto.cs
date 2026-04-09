using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassFinancialSummaryDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public int StudentCount { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal DebtAmount { get; set; }
        public double CollectionRate { get; set; }
    }
}
