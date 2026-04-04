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

        public async Task UpdateAsync(ProgressReport report)
        {
            context.ProgressReports.Update(report);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ProgressReport report)
        {
            context.ProgressReports.Remove(report);
            await context.SaveChangesAsync();
        }

        public async Task<ProgressReport?> GetByIdAsync(Guid reportId)
        {
            return await context.ProgressReports
                .Include(r => r.Student).ThenInclude(s => s.StudentNavigation)
                .Include(r => r.Teacher).ThenInclude(t => t.TeacherNavigation)
                .Include(r => r.Class)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<IEnumerable<ProgressReport>> GetReportsByClassAndPeriodAsync(Guid classId, int month, int year)
        {
            return await context.ProgressReports
                .AsNoTracking()
                .Where(r => r.ClassId == classId && r.PeriodMonth == month && r.PeriodYear == year)
                .ToListAsync();
        }
        public async Task<bool> IsReportExistAsync(Guid studentId, Guid classId, int month, int year)
        {
            return await context.ProgressReports.AnyAsync(r =>
                r.StudentId == studentId &&
                r.ClassId == classId &&
                r.PeriodMonth == month &&
                r.PeriodYear == year);
        }

        public async Task<IEnumerable<ClassEnrollment>> GetActiveStudentsInClassAsync(Guid classId)
        {
            return await context.ClassEnrollments
                .Include(e => e.Student).ThenInclude(s => s.StudentNavigation)
                .Where(e => e.ClassId == classId && e.Status == "Active")
                .ToListAsync();
        }
    }
}
