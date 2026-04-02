using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class SubmissionDetailDto
    {
        public Guid SubmissionID { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
        public DateTime SubmittedAt { get; set; }
        public decimal? Grade { get; set; }
        public string Status { get; set; }
        public List<string> Feedbacks { get; set; } = new List<string>();
    }

    public class SubmitAssignmentRequest
    {
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }
}
