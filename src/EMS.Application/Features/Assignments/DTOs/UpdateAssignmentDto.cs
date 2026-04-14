using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class UpdateAssignmentDto
    {
        public Guid GradeCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool AllowLateSubmission { get; set; }
        public bool Isgraded { get; set; } = true;
        public List<IFormFile>? NewAttachments { get; set; }
        public List<Guid>? RemoveAttachmentIds { get; set; }
    }
}
