using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDbContext context;

        public PostRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task AddAsync(Post post)
        {
            await context.Posts.AddAsync(post);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Post post)
        {
            context.Posts.Update(post);
            await context.SaveChangesAsync();
        }

        public async Task<Post?> GetByIdAsync(Guid postId)
        {
            return await context.Posts
                .FirstOrDefaultAsync(m => m.PostId == postId && m.IsDeleted != true);
        }

        public async Task<Post?> GetByIdWithDetailsAsync(Guid postId)
        {
            // Sử dụng Queryable để có thể Select linh hoạt
            var query = context.Posts
                .Where(p => p.PostId == postId && p.IsDeleted != true)
                .Select(p => new Post
                {
                    PostId = p.PostId,
                    ClassId = p.ClassId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    Author = p.Author,
                    PostAttachments = p.PostAttachments.ToList(),
                    Comments = p.Comments
                        .Where(c => c.IsDeleted != true)
                        .Select(c => new Comment
                        {
                            CommentId = c.CommentId,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt,
                            AuthorId = c.AuthorId,
                            // LOGIC QUAN TRỌNG: 
                            // Thử tìm trong bảng Students xem AuthorId có khớp với StudentID nào không
                            // Nếu có (Học sinh) thì bốc FullName của Student
                            // Nếu không (Giáo viên) thì bốc FullName của Account
                            Author = new Account
                            {
                                FullName = context.Students
                                    .Where(s => s.StudentId == c.AuthorId)
                                    .Select(s => s.FullName)
                                    .FirstOrDefault() ?? c.Author.FullName
                            }
                        }).ToList()
                });

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Post>> GetByClassIdAsync(Guid classId)
        {
            return await context.Posts
                .AsNoTracking()
                .Where(m => m.ClassId == classId && m.IsDeleted != true)
                .OrderByDescending(m => m.CreatedAt)
                .Select(p => new Post
                {
                    PostId = p.PostId,
                    ClassId = p.ClassId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    // Lấy thông tin tác giả bài viết (Giáo viên)
                    Author = p.Author,
                    PostAttachments = p.PostAttachments.ToList(),
                    Comments = p.Comments
                        .Where(c => c.IsDeleted != true)
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new Comment
                        {
                            CommentId = c.CommentId,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt,
                            AuthorId = c.AuthorId,
                            // LOGIC "ĂN TIỀN": Ép lấy tên từ bảng Student trước
                            Author = new Account
                            {
                                FullName = context.Students
                                    .Where(s => s.StudentId == c.AuthorId)
                                    .Select(s => s.FullName)
                                    .FirstOrDefault() ?? c.Author.FullName
                            }
                        }).ToList()
                })
                .ToListAsync();
        }

        public async Task AddAttachmentAsync(PostAttachment attachment)
        {
            await context.PostAttachments.AddAsync(attachment);
            await context.SaveChangesAsync();
        }

        public async Task<PostAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
        {
            return await context.PostAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);
        }

        public async Task RemoveAttachmentAsync(PostAttachment attachment)
        {
            context.PostAttachments.Remove(attachment);
            await context.SaveChangesAsync();
        }

        public async Task AddCommentAsync(Comment comment)
        {
            await context.Comments.AddAsync(comment);
            await context.SaveChangesAsync();
        }

        public async Task<Comment?> GetCommentByIdAsync(Guid commentId)
        {
            return await context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.IsDeleted != true);
        }

        public async Task UpdateCommentAsync(Comment comment)
        {
            context.Comments.Update(comment);
            await context.SaveChangesAsync();
        }
    }
}
