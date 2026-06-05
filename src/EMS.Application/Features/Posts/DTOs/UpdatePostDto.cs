using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.DTOs
{
    public class UpdatePostDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public List<IFormFile>? NewAttachments { get; set; }
        public List<Guid>? RemoveAttachmentIds { get; set; }
    }
}
