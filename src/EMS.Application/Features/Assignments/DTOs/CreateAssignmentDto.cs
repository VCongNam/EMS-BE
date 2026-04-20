using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class CreateAssignmentDto
    {
        public Guid ClassId { get; set; }
        public Guid? GradeCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public bool AllowLateSubmission { get; set; }
        public bool Isgraded { get; set; } = true;
        public string Status { get; set; } = "Draft"; 
        public List<IFormFile>? Attachments { get; set; }
    }
}
