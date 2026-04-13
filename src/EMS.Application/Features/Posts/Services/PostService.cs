using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.Posts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ISupabaseStorageService storageService;
        private readonly ICurrentUserService currentUserService;
        private readonly INotificationService _notificationService; 
        private readonly ILogger<PostService> _logger; 

        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
        private static readonly string[] AllowedMimeTypes =
        {
            "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/svg+xml", "image/bmp",
            "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/zip", "application/x-rar-compressed"
        };

        public PostService(
            IPostRepository postRepository,
            ISupabaseStorageService storageService,
            ICurrentUserService currentUserService,
            INotificationService notificationService, 
            ILogger<PostService> logger)
        {
            this.postRepository = postRepository;
            this.storageService = storageService;
            this.currentUserService = currentUserService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Guid> CreatePostAsync(CreatePostDto request)
        {
            var postId = Guid.NewGuid();
            var post = new Post
            {
                PostId = postId,
                ClassId = request.ClassId,
                AuthorId = currentUserService.UserId,
                Title = request.Title,
                Content = request.Content,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await postRepository.AddAsync(post);

            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var file in request.Attachments)
                {
                    ValidateFile(file.FileName, file.Length, file.ContentType);
                    var attachmentUrl = await storageService.UploadFileAsync(file, $"posts/{postId}");

                    var attachment = new PostAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        PostId = postId,
                        FileName = file.FileName,
                        FileUrl = attachmentUrl,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    };
                    await postRepository.AddAttachmentAsync(attachment);
                }
            }

            //Notification
            await SendPostNotificationAsync(request.ClassId, "Bài đăng mới",
                $"Giáo viên đã đăng một bài viết mới: {request.Title}", postId);

            return postId;
        }

        public async Task UpdatePostAsync(Guid id, UpdatePostDto request)
        {
            var post = await postRepository.GetByIdAsync(id);
            if (post == null) throw new Exception($"Post with ID {id} not found.");

            if (post.AuthorId != currentUserService.UserId)
                throw new Exception("Bạn không có quyền chỉnh sửa bài đăng này.");

            post.Title = request.Title;
            post.Content = request.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await postRepository.UpdateAsync(post);

            if (request.RemoveAttachmentIds != null && request.RemoveAttachmentIds.Count > 0)
            {
                foreach (var attachmentId in request.RemoveAttachmentIds)
                {
                    var attachment = await postRepository.GetAttachmentByIdAsync(attachmentId);
                    if (attachment != null)
                    {
                        await storageService.DeleteFileByUrlAsync(attachment.FileUrl);
                        await postRepository.RemoveAttachmentAsync(attachment);
                    }
                }
            }

            if (request.NewAttachments != null && request.NewAttachments.Count > 0)
            {
                foreach (var file in request.NewAttachments)
                {
                    ValidateFile(file.FileName, file.Length, file.ContentType);
                    var attachmentUrl = await storageService.UploadFileAsync(file, $"posts/{id}");

                    var attachment = new PostAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        PostId = id,
                        FileName = file.FileName,
                        FileUrl = attachmentUrl,
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    };
                    await postRepository.AddAttachmentAsync(attachment);
                }
            }

            //Notification
            await SendPostNotificationAsync(post.ClassId, "Bài đăng cập nhật",
                $"Bài viết '{post.Title}' vừa được giáo viên cập nhật nội dung.", id);
        }

        private async Task SendPostNotificationAsync(Guid classId, string title, string content, Guid postId)
        {
            try
            {
                var targets = await _notificationService.GetAllClassTargetsAsync(classId);

                if (targets.Any())
                {
                    await _notificationService.SendBulkNotificationWithStudentAsync(
                        targets: targets,
                        title: title,
                        content: content,
                        actionUrl: $"/student/classes/{classId}",
                        type: "Post"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo Post: {ex.Message}");
            }
        }

        public async Task DeletePostAsync(Guid id)
        {
            var post = await postRepository.GetByIdAsync(id);
            if (post == null) throw new Exception("Post not found.");

            if (post.AuthorId != currentUserService.UserId)
                throw new Exception("Bạn không có quyền xóa bài đăng này.");

            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;
            await postRepository.UpdateAsync(post);
        }

        public async Task<PostResponseDto> GetPostDetailAsync(Guid postId)
        {
            var post = await postRepository.GetByIdWithDetailsAsync(postId);
            if (post == null) throw new Exception("Post not found or has been deleted.");

            return new PostResponseDto
            {
                PostId = post.PostId,
                ClassId = post.ClassId,
                AuthorName = post.Author?.FullName ?? "Unknown",
                Title = post.Title ?? null!,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Attachments = post.PostAttachments.Select(a => new PostAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                }).ToList(),
                Comments = post.Comments.Select(c => new CommentResponseDto
                {
                    CommentId = c.CommentId,
                    AuthorId = c.AuthorId,

                    AuthorName = c.Author?.FullName ?? "Người dùng ẩn danh",

                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).OrderBy(c => c.CreatedAt).ToList()
            };
        }

        public async Task<IEnumerable<PostSummaryDto>> GetPostsByClassIdAsync(Guid classId)
        {
            var posts = await postRepository.GetByClassIdAsync(classId);

            return posts.Select(p => new PostSummaryDto
            {
                PostId = p.PostId,
                Title = p.Title ?? null!,
                Content = p.Content,
                AuthorName = p.Author?.FullName ?? "Unknown",
                CreatedAt = p.CreatedAt,

                Attachments = p.PostAttachments.Select(a => new PostAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                }).ToList(),

                Comments = p.Comments.Select(c => new CommentResponseDto
                {
                    CommentId = c.CommentId,
                    AuthorId = c.AuthorId,

                    AuthorName = c.Author?.FullName ?? "Người dùng ẩn danh",

                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).OrderBy(c => c.CreatedAt).ToList()
            });
        }

        public async Task<Guid> CreateCommentAsync(Guid postId, CreateCommentDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new Exception("Nội dung bình luận không được để trống.");

            var post = await postRepository.GetByIdAsync(postId);
            if (post == null) throw new Exception("Không tìm thấy bài viết.");

            Guid authorId = currentUserService.Role == "Student"
                ? (currentUserService.StudentId ?? throw new Exception("Không tìm thấy ID học sinh đang được chọn."))
                : currentUserService.UserId;

            var comment = new Comment
            {
                CommentId = Guid.NewGuid(),
                PostId = postId,
                Content = request.Content,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                AuthorId = authorId
            };

            await postRepository.AddCommentAsync(comment);
            return comment.CommentId;
        }

        public async Task DeleteCommentAsync(Guid commentId)
        {
            var comment = await postRepository.GetCommentByIdAsync(commentId);
            if (comment == null) throw new Exception("Không tìm thấy bình luận.");

            // Xác định ID đang thao tác hiện tại
            Guid currentActingId = currentUserService.Role == "Student"
                ? (currentUserService.StudentId ?? currentUserService.UserId)
                : currentUserService.UserId;

            if (comment.AuthorId != currentActingId)
                throw new Exception("Bạn không có quyền xóa bình luận này.");

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;

            await postRepository.UpdateCommentAsync(comment);
        }

        private void ValidateFile(string fileName, long fileSize, string contentType)
        {
            if (fileSize > MaxFileSize)
                throw new Exception($"File '{fileName}' exceeds maximum size of 10MB.");

            if (contentType.StartsWith("image/")) return;

            if (!AllowedMimeTypes.Contains(contentType))
                throw new Exception($"File type '{contentType}' is not allowed.");
        }

    }
}
