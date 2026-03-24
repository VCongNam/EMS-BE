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
        Task<Post?> GetByIdAsync(Guid postId);
        Task<Post?> GetByIdWithDetailsAsync(Guid postId);
        Task UpdateAsync(Post post);
        Task AddCommentAsync(Comment comment);
    }
}
