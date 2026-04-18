using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.DTOs
{
    public class AttendanceHistoryDto
    {
        public DateOnly Date { get; set; }
        public string Status { get; set; } = string.Empty; 
        public string? Note { get; set; }
        public string? Topic { get; set; } 
    }
}
