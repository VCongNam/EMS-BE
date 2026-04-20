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
        public string BillingMethod { get; set; } = string.Empty; 
        public decimal TuitionFee { get; set; } 
        public int StudentCount { get; set; }   
        public double CollectionRate { get; set; }

        public string ConditionCode { get; set; } 
        public string StatusMessage { get; set; }
        public bool IsIssuable { get; set; }
    }
}
