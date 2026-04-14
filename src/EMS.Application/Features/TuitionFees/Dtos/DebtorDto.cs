using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class DebtorDto
    {
        public string StudentName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }
        public int OverdueDays => DueDate < DateTime.UtcNow ? (DateTime.UtcNow - DueDate).Days : 0;
    }
}
