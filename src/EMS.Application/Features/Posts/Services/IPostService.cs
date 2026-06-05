using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Posts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.Services
{
    public interface IPostService
    {
        Task<Guid> CreatePostAsync(CreatePostDto request);
        Task UpdatePostAsync(Guid id, UpdatePostDto request);
        Task DeletePostAsync(Guid id);
        Task<PostResponseDto> GetPostDetailAsync(Guid postId);
        Task<IEnumerable<PostSummaryDto>> GetPostsByClassIdAsync(Guid classId);

        Task<Guid> CreateCommentAsync(Guid postId, CreateCommentDto request);
        Task DeleteCommentAsync(Guid commentId);
        Task<PagedResult<StudentPostDto>> GetClassPostsAsync(Guid classId, PostFilter filter);
    }
}
