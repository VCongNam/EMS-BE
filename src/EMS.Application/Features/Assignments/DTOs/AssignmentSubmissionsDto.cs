using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class AssignmentSubmissionsDto
    {
        public Guid AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }

        public List<SubmissionBasicDto> Submissions { get; set; } = new List<SubmissionBasicDto>();
    }

    public class SubmissionBasicDto
    {
        public Guid SubmissionId { get; set; }
        public Guid StudentId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Grade { get; set; }
    }

}
