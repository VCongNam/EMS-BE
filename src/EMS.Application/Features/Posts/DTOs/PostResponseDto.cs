using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.DTOs
{
    public class PostResponseDto
    {
        public Guid PostId { get; set; }
        public Guid ClassId { get; set; }
        public string AuthorName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PostAttachmentDto> Attachments { get; set; } = new();
        public List<CommentResponseDto> Comments { get; set; } = new();
    }
}
