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

        public async Task AddCommentAsync(Comment comment)
        {
            await context.Comments.AddAsync(comment);
            await context.SaveChangesAsync();
        }

        public async Task<Post?> GetByIdAsync(Guid postId)
        {
            return await context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<Post?> GetByIdWithDetailsAsync(Guid postId)
        {
            return await context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task UpdateAsync(Post post)
        {
            context.Posts.Update(post);
            await context.SaveChangesAsync();
        }
    }
}
