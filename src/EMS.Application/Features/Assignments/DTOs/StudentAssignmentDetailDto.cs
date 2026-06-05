using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class StudentAssignmentDetailDto
    {
        public Guid AssignmentID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<StudentAttachmentDto> Attachments { get; set; } = new List<StudentAttachmentDto>();
        public DateTime DueDate { get; set; }

        public SubmissionDetailDto? MySubmission { get; set; }
    }

    public class StudentAttachmentDto
    {
        public Guid AttachmentID { get; set; }
        public string FileName { get; set; }
        public string FileURL { get; set; }
        public string FileType { get; set; }
        public long? FileSize { get; set; }
        public string? FileRole { get; set; }
    }
}
