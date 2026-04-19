using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EMS.Infrastructure.Data;


namespace EMS.Infrastructure.Repositories
{
    public class TeacherReportRepository : ITeacherReportRepository
    {
        private readonly ApplicationDbContext context;
        public TeacherReportRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Class>> GetFilteredClassesAsync(Guid teacherId, DateOnly start, DateOnly end, Guid? subjectId, string? status)
        {
            var query = context.Classes
                .Include(c => c.Subject)
                .Include(c => c.ClassEnrollments)
                .Where(c => c.TeacherId == teacherId && c.IsDeleted != true);

            query = query.Where(c => c.StartDate <= end && (c.EndDate == null || c.EndDate >= start));

            if (subjectId.HasValue) query = query.Where(c => c.SubjectId == subjectId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);

            return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<Class?> GetClassByIdAsync(Guid classId)
        {
            return await context.Classes
                .Include(c => c.Subject)
                .Include(c => c.ClassEnrollments)
                    .ThenInclude(e => e.Student) 
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.IsDeleted != true);
        }

        public async Task<List<Submission>> GetSubmissionsForClassesAsync(List<Guid> classIds, DateTime start, DateTime end)
        {
            return await context.Submissions
                .Include(s => s.Assignment).ThenInclude(a => a.GradeCategory)
                .Where(s => classIds.Contains(s.Assignment.ClassId) && s.Assignment.DueDate >= start && s.Assignment.DueDate <= end && s.Grade != null)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, (int NewCount, int DropoutCount)>> GetEnrollmentStatsAsync(List<Guid> classIds, DateOnly start, DateOnly end)
        {
            var data = await context.ClassEnrollments
                .Where(ce => classIds.Contains(ce.ClassId))
                .GroupBy(ce => ce.ClassId)
                .Select(g => new {
                    ClassId = g.Key,
                    New = g.Count(x => x.EnrolledDate >= start && x.EnrolledDate <= end),
                    Drop = g.Count(x => x.DroppedDate >= start && x.DroppedDate <= end)
                }).ToListAsync();

            return data.ToDictionary(x => x.ClassId, x => (NewCount: x.New, DropoutCount: x.Drop));
        }

        public async Task<Dictionary<Guid, (int TotalSlots, int PresentCount)>> GetAttendanceStatsAsync(List<Guid> classIds, DateOnly start, DateOnly end)
        {
            var data = await context.Attendances
                .Where(a => classIds.Contains(a.Session.ClassId) && a.Session.Date >= start && a.Session.Date <= end && a.Session.Status == "Completed")
                .GroupBy(a => a.Session.ClassId)
                .Select(g => new {
                    ClassId = g.Key,
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == "Present")
                }).ToListAsync();

            return data.ToDictionary(x => x.ClassId, x => (TotalSlots: x.Total, PresentCount: x.Present));
        }
    }
}
