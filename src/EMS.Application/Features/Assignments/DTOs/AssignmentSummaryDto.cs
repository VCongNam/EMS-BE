using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class AssignmentSummaryDto
    {
        public Guid AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool? Isgraded { get; set; }
        public bool? IsOffline { get; set; }
        public int TotalSubmissions { get; set; }
        public int TotalStudents { get; set; }
        public string? GradeCategoryName { get; set; }
    }

}
