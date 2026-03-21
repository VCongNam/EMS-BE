using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class ClassTADto
    {
        public Guid TAID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Permission { get; set; }
        public decimal? SalaryPerSession { get; set; }
    }
}
