using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.LearningMaterials.DTOs
{
    public class CreateLearningMaterialDto
    {
        public Guid ClassId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IFormFile File { get; set; } = null!;
        public List<IFormFile>? Attachments { get; set; }
    }
}
