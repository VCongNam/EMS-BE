using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.LearningMaterials.DTOs
{
    public class MaterialDto
    {
        public Guid MaterialID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<StudentMaterialAttachmentDto> Attachments { get; set; } = new List<StudentMaterialAttachmentDto>();
    }

    public class StudentMaterialAttachmentDto
    {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; }
        public long? FileSize { get; set; }
    }
}
