using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class AssignedClassDto
    {
        public Guid ClassID { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string? Permission { get; set; }
        public decimal? SalaryPerSession { get; set; }
        public string TeacherName { get; set; } = string.Empty; 
    }
}
