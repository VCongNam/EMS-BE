using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.DTOs
{
    public class StudentPostDto
    {
        public Guid PostID { get; set; }
        public string Content { get; set; }
        public string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PostFilter
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
