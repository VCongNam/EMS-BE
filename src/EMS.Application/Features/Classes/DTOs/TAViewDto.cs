using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class TAViewDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public Guid TAId { get; set; }
        public Guid ClassTaId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Permission { get; set; }
        public decimal? SalaryPerSession { get; set; }
    }

    public class TAProfileDto
    {
        public Guid TAId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public string? AvatarURL { get; set; }
    }
}
