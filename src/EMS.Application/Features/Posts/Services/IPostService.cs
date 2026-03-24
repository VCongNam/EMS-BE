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
        Task<PostResponseDto> GetPostByIdAsync(Guid postId);
        Task UpdatePostAsync(Guid postId, UpdatePostDto request);
        Task DeletePostAsync(Guid postId);
        Task<Guid> AddCommentAsync(Guid postId, CreateCommentDto request);
    }
}
