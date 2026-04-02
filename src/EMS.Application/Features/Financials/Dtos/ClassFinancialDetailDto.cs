using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Financials.Dtos
{
    public class ClassFinancialDetailDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public decimal TuitionFeePerStudent { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal CollectedRevenue { get; set; }
        public List<StudentInvoiceDto> StudentInvoices { get; set; } = new();
    }
}
