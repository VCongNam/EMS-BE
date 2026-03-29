using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class AssignmentDetailDto
    {
        public Guid AssignmentID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AttachmentPath { get; set; } // Link file đề bài gốc
        public DateTime DueDate { get; set; }

        public SubmissionDetailDto? MySubmission { get; set; }
    }
}
