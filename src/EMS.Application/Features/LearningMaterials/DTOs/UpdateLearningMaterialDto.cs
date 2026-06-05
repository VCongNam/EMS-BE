using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.LearningMaterials.DTOs
{
    public class UpdateLearningMaterialDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<IFormFile>? NewAttachments { get; set; }
        public List<Guid>? RemoveAttachmentIds { get; set; }
    }
}
