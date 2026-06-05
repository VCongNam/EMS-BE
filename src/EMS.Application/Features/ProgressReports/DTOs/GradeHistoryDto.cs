using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.ProgressReports.DTOs
{
    public class GradeHistoryDto
    {
        public string AssignmentTitle { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal? Grade { get; set; }
        public DateTime Date { get; set; }
    }
}
