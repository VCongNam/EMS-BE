using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EMS.Infrastructure.Data;

namespace EMS.Infrastructure.Repositories
{
    public class ProgressReportRepository : IProgressReportRepository
    {
        private readonly ApplicationDbContext context;

        public ProgressReportRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task AddAsync(ProgressReport report)
        {
            await context.ProgressReports.AddAsync(report);
            await context.SaveChangesAsync();
        }

        public async Task<ProgressReport?> GetByIdAsync(Guid reportId)
        {
            return await context.ProgressReports.FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<ProgressReport?> GetByIdWithDetailsAsync(Guid reportId)
        {
            // Include các bảng liên quan để lấy tên Student và Class
            return await context.ProgressReports
                .Include(r => r.Class)
                .Include(r => r.Student)
                    .ThenInclude(s => s.StudentNavigation) // Link tới Account để lấy FullName
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<IEnumerable<ProgressReport>> GetReportsByTeacherIdAsync(Guid teacherId)
        {
            return await context.ProgressReports
                .Include(r => r.Class)
                .Include(r => r.Student)
                    .ThenInclude(s => s.StudentNavigation)
                .Where(r => r.TeacherId == teacherId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(ProgressReport report)
        {
            context.ProgressReports.Update(report);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ProgressReport report)
        {
            context.ProgressReports.Remove(report); // Hard Delete
            await context.SaveChangesAsync();
        }
    }
}
