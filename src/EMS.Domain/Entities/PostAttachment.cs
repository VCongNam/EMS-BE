using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Entities
{
    public class PostAttachment
    {
        public Guid AttachmentId { get; set; }

        public Guid PostId { get; set; }

        public string FileUrl { get; set; } = null!;

        public string FileType { get; set; } = null!;

        public string FileName { get; set; } = null!;

        public long? FileSize { get; set; }

        public DateTime? CreatedAt { get; set; }

        public virtual Post Post { get; set; } = null!;
    }
}
