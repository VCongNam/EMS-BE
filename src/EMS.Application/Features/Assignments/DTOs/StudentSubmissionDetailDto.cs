using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Assignments.DTOs
{
 
    public class StudentSubmissionDetailDto
    {
        public Guid SubmissionId { get; set; }
        public Guid AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;

        public Guid StudentId { get; set; }
        public string StudentFullName { get; set; } = string.Empty;

        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Grade { get; set; }

        public List<SubmissionAttachmentDto> Attachments { get; set; } = new();

        public List<SubmissionFeedbackDto> Feedbacks { get; set; } = new();
    }

    public class SubmissionAttachmentDto
    {
        public Guid AttachmentId { get; set; }
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class SubmissionFeedbackDto
    {
        public Guid FeedbackId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
