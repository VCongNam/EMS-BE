using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class ClassFinancialDetailDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string BillingMethod { get; set; } = null!;
        public List<StudentInvoiceItemDto> Students { get; set; } = new();
    }
}
