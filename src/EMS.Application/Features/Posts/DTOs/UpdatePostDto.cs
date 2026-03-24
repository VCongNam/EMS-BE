using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.DTOs
{
    public class UpdatePostDto
    {
        public string Content { get; set; } = null!;

        // TODO: Mở comment khi làm chức năng Upload File
        // public IFormFile? Attachment { get; set; }
    }
}
