using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class SystemAdminRepository : ISystemAdminRepository
    {
        private readonly ApplicationDbContext context;

        public SystemAdminRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<int> CountAccountsByRoleAsync(string roleName)
        {
            return await context.Accounts
                .CountAsync(a => a.Role.RoleName == roleName && a.Status == "Active" && a.IsDeleted == false);
        }

        public async Task<int> CountOngoingClassesAsync()
        {
            return await context.Classes
                .CountAsync(c => c.IsDeleted == false);
        }

        public async Task<IEnumerable<Account>> GetAccountsInPeriodAsync(DateTime start, DateTime end)
        {
            return await context.Accounts
                .AsNoTracking()
                .Include(a => a.Role)
                .Where(a => a.CreatedAt >= start && a.CreatedAt <= end && a.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsInPeriodAsync(DateTime start, DateTime end)
        {
            return await context.Posts.AsNoTracking().Where(p => p.CreatedAt >= start && p.CreatedAt <= end && p.IsDeleted == false).ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsInPeriodAsync(DateTime start, DateTime end)
        {
            return await context.Assignments.AsNoTracking().Where(a => a.CreatedAt >= start && a.CreatedAt <= end && a.IsDeleted == false).ToListAsync();
        }

        public async Task<IEnumerable<Session>> GetSessionsInPeriodAsync(DateTime start, DateTime end)
        {
            return await context.Sessions.AsNoTracking().Where(s => s.CreatedAt >= start && s.CreatedAt <= end && s.IsDeleted == false).ToListAsync();
        }

        public async Task<IEnumerable<Teacher>> GetAllTeachersGridAsync(string? searchTerm, string? statusFilter)
        {
            var query = context.Teachers
                .AsNoTracking()
                .Include(t => t.TeacherNavigation) // Thông tin Account
                .Include(t => t.Classes.Where(c => c.IsDeleted == false))
                    .ThenInclude(c => c.ClassEnrollments)
                .Where(t => t.TeacherNavigation.IsDeleted == false);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string searchLower = searchTerm.ToLower();
                query = query.Where(t =>
                    (t.TeacherNavigation.FullName != null && t.TeacherNavigation.FullName.ToLower().Contains(searchLower)) ||
                    (t.TeacherNavigation.PhoneNumber != null && t.TeacherNavigation.PhoneNumber.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(t => t.TeacherNavigation.Status == statusFilter);
            }

            return await query.OrderByDescending(t => t.TeacherNavigation.CreatedAt).ToListAsync();
        }

        public async Task<Teacher?> GetTeacherByIdAsync(Guid teacherId)
        {
            return await context.Teachers
                .AsNoTracking()
                .Include(t => t.TeacherNavigation)
                .Include(t => t.Classes.Where(c =>  c.IsDeleted == false))
                    .ThenInclude(c => c.ClassEnrollments)
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId && t.TeacherNavigation.IsDeleted == false);
        }
    }
}
