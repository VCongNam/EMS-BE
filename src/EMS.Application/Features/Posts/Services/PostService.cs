using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Posts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository postRepository;
        private readonly ICurrentUserService currentUserService;

        // TODO: Mở comment khi làm chức năng Upload File
        // private readonly ISupabaseStorageService _storageService;

        public PostService(
            IPostRepository postRepository,
            ICurrentUserService currentUserService
            /* TODO: Mở comment -> , ISupabaseStorageService storageService */)
        {
            this.postRepository = postRepository;
            this.currentUserService = currentUserService;
            // _storageService = storageService;
        }

        public async Task<Guid> CreatePostAsync(CreatePostDto request)
        {
            string? attachmentUrl = null;

            // TODO: Mở comment khi làm chức năng Upload File
            /*
            if (request.Attachment != null)
            {
                attachmentUrl = await _storageService.UploadFileAsync(request.Attachment, "post-attachments");
            }
            */

            var newPost = new Post
            {
                PostId = Guid.NewGuid(),
                ClassId = request.ClassId,
                AuthorId = currentUserService.UserId,
                Content = request.Content,
                AttachmentUrl = attachmentUrl,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await postRepository.AddAsync(newPost);
            return newPost.PostId;
        }

        public async Task<PostResponseDto> GetPostByIdAsync(Guid postId)
        {
            var post = await postRepository.GetByIdWithDetailsAsync(postId);

            if (post == null || post.IsDeleted == true)
                throw new Exception("Post not found or has been deleted!");

            return new PostResponseDto
            {
                PostId = post.PostId,
                ClassId = post.ClassId,
                AuthorName = post.Author?.FullName ?? "Unknown",
                Content = post.Content,
                AttachmentUrl = post.AttachmentUrl,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                // Lọc bỏ những Comment đã bị xóa mềm (Soft Delete)
                Comments = post.Comments.Where(c => c.IsDeleted != true).Select(c => new CommentResponseDto
                {
                    CommentId = c.CommentId,
                    Content = c.Content,
                    AuthorName = c.Author?.FullName ?? "Unknown",
                    CreatedAt = c.CreatedAt
                }).OrderBy(c => c.CreatedAt).ToList()
            };
        }

        public async Task UpdatePostAsync(Guid postId, UpdatePostDto request)
        {
            var post = await postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted == true)
                throw new Exception("Post not found!");

            if (post.AuthorId != currentUserService.UserId)
                throw new Exception("Access Denied: You are not the author of this post!");

            // TODO: Mở comment khi làm chức năng Upload File
            /*
            if (request.Attachment != null)
            {
                post.AttachmentUrl = await _storageService.UploadFileAsync(request.Attachment, "post-attachments");
            }
            */

            post.Content = request.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await postRepository.UpdateAsync(post);
        }

        public async Task DeletePostAsync(Guid postId)
        {
            var post = await postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted == true)
                throw new Exception("Post not found!");

            if (post.AuthorId != currentUserService.UserId)
                throw new Exception("Access Denied: You are not the author of this post!");

            post.IsDeleted = true; // Xóa mềm
            post.UpdatedAt = DateTime.UtcNow;

            await postRepository.UpdateAsync(post);
        }

        public async Task<Guid> AddCommentAsync(Guid postId, CreateCommentDto request)
        {
            var post = await postRepository.GetByIdAsync(postId);

            if (post == null || post.IsDeleted == true)
                throw new Exception("Cannot comment: Post not found!");

            var comment = new Comment
            {
                CommentId = Guid.NewGuid(),
                PostId = postId,
                AuthorId = currentUserService.UserId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await postRepository.AddCommentAsync(comment);
            return comment.CommentId;
        }
        
    }
}
