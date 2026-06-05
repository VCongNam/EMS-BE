using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IPostRepository
    {
        Task AddAsync(Post post);
        Task UpdateAsync(Post post);
        Task<Post?> GetByIdAsync(Guid postId);
        Task<Post?> GetByIdWithDetailsAsync(Guid postId);
        Task<IEnumerable<Post>> GetByClassIdAsync(Guid classId);

        Task AddAttachmentAsync(PostAttachment attachment);
        Task<PostAttachment?> GetAttachmentByIdAsync(Guid attachmentId);
        Task RemoveAttachmentAsync(PostAttachment attachment);

        Task AddCommentAsync(Comment comment);
        Task<Comment?> GetCommentByIdAsync(Guid commentId);
        Task UpdateCommentAsync(Comment comment);
    }
}
