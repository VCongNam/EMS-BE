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
            return await context.Posts
                .Include(m => m.Author)
                .Include(m => m.PostAttachments)
                .Include(m => m.Comments.Where(c => c.IsDeleted != true))
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(m => m.PostId == postId && m.IsDeleted != true);
        }

        public async Task<IEnumerable<Post>> GetByClassIdAsync(Guid classId)
        {
            return await context.Posts
                .AsNoTracking()
                .Include(m => m.Author)
                .Include(m => m.PostAttachments)
                .Include(m => m.Comments.Where(c => c.IsDeleted != true))
                .Where(m => m.ClassId == classId && m.IsDeleted != true)
                .OrderByDescending(m => m.CreatedAt)
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
