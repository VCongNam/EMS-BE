using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.DTOs
{
    public class CreateOfflineTestDto
    {
        public Guid ClassId { get; set; }
        public Guid GradeCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? MaxScore { get; set; }
        public DateTime TestDate { get; set; }          // → ghi vào CreatedAt
        public List<IFormFile>? Attachments { get; set; }
    }
    public class UploadOfflineSubmissionDto
    {
        public Guid StudentId { get; set; }
        public List<IFormFile> Files { get; set; } = new();
    }
}
