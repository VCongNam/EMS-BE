using System;
using System.Collections.Generic;

namespace EMS.Application.Features.LearningMaterials.DTOs
{
    public class LearningMaterialResponseDto
    {
        public Guid MaterialId { get; set; }
        public Guid ClassId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<MaterialAttachmentDto> Attachments { get; set; } = new();
    }

    public class LearningMaterialSummaryDto
    {
        public Guid MaterialId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }

    public class MaterialAttachmentDto
    {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
