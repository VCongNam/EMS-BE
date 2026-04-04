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
                r.StudentId == studentId && r.ClassId == classId && r.PeriodMonth == month && r.PeriodYear == year);
        }

        public async Task<IEnumerable<ClassEnrollment>> GetActiveStudentsInClassAsync(Guid classId)
        {
            return await context.ClassEnrollments
                .Include(e => e.Student).ThenInclude(s => s.StudentNavigation)
                .Where(e => e.ClassId == classId && e.Status == "Active")
                .ToListAsync();
        }

        // --- Lấy dữ liệu điểm số ---
        public async Task<List<Submission>> GetSubmissionsForCalcAsync(Guid classId, DateTime startDate, DateTime endDate)
        {
            return await context.Submissions
                .Include(s => s.Assignment).ThenInclude(a => a.GradeCategory)
                .Where(s => s.Assignment.ClassId == classId
                         && s.Assignment.DueDate >= startDate && s.Assignment.DueDate <= endDate
                         && s.Grade != null)
                .ToListAsync();
        }

        // --- Lấy dữ liệu điểm danh ---
        public async Task<List<Attendance>> GetAttendancesForCalcAsync(Guid classId, DateOnly startDate, DateOnly endDate)
        {
            return await context.Attendances
                .Include(a => a.Session)
                .Where(a => a.Session.ClassId == classId
                         && a.Session.Date >= startDate && a.Session.Date <= endDate)
                .ToListAsync();
        }
    }
}
