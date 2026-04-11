using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassRevenueDistributionDto
    {
        public string ClassName { get; set; } = null!;
        public decimal Revenue { get; set; }
    }
}
