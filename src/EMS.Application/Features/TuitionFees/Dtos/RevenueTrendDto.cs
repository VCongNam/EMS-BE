using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class RevenueTrendDto
    {
        public string MonthLabel { get; set; } = null!;
        public decimal Revenue { get; set; }
    }
}
