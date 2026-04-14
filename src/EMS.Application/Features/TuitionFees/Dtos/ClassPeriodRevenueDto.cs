using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassPeriodRevenueDto
    {
        public decimal ExpectedRevenue { get; set; } 
        public decimal ActualRevenue { get; set; }   
        public decimal DebtAmount { get; set; }    
    }
}
