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
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string? Status { get; set; }

 
        public int StudentCount { get; set; }           
        public List<string> Schedules { get; set; } = new();
        public DateTime CreatedAt { get; set; }        

        public string? Permission { get; set; }
        public decimal? SalaryPerSession { get; set; }
    }
}
