using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class AssignmentItemDto
    {
        public Guid AssignmentID { get; set; }
        public string Title { get; set; }
        public DateTime DueDate { get; set; }

        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal? Grade { get; set; }

        // Trạng thái hiển thị cho học sinh ("Pending", "Submitted", "Overdue", "Graded")
        public string StudentStatus { get; set; }
    }

    public class AssignmentFilter
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }
}
