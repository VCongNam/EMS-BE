using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class AssignmentDetailDto
    {
        public Guid AssignmentId { get; set; }
        public Guid ClassId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public Guid? GradeCategoryId { get; set; }
        public string? GradeCategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public string? Status { get; set; }
        public bool? AllowLateSubmission { get; set; }
        public bool? IsOffline { get; set; }
        public bool? Isgraded { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class AttachmentDto
    {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
