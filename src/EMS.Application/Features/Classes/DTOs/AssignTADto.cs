using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class AssignTADto
    {
        public Guid TAID { get; set; }
        public string Permission { get; set; }
        public decimal? SalaryPerSession { get; set; }
    }
}
