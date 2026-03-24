using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.DTOs
{
    public class CreatePostDto
    {
        public Guid ClassId { get; set; }
        public string Content { get; set; } = null!;
        //public IFormFile? Attachment { get; set; } // Nhận file upload
    }
}
