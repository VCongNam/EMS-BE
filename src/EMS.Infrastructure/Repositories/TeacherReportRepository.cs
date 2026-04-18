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

        public async Task<List<Class>> GetActiveClassesAsync(Guid teacherId)
        {
            return await context.Classes
                .Include(c => c.Subject)
                .Include(c => c.ClassEnrollments)
                .Where(c => c.TeacherId == teacherId
                         && c.IsDeleted != true) // Xử lý bool? an toàn
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, (int NewCount, int DropoutCount)>> GetEnrollmentStatsAsync(List<Guid> classIds, DateOnly start, DateOnly end)
        {
            var groupedData = await context.ClassEnrollments
                .Where(ce => classIds.Contains(ce.ClassId))
                .GroupBy(ce => ce.ClassId)
                .Select(g => new
                {
                    ClassId = g.Key,
                    // Xử lý DateOnly? an toàn (is not null)
                    NewCount = g.Count(x => x.EnrolledDate != null && x.EnrolledDate >= start && x.EnrolledDate <= end),
                    DropoutCount = g.Count(x => x.DroppedDate != null && x.DroppedDate >= start && x.DroppedDate <= end)
                })
                .ToListAsync();

            return groupedData.ToDictionary(
                x => x.ClassId,
                x => (x.NewCount, x.DropoutCount)
            );
        }

        public async Task<Dictionary<Guid, (int TotalSlots, int PresentCount)>> GetAttendanceStatsAsync(List<Guid> classIds, DateOnly start, DateOnly end)
        {
            var groupedData = await context.Attendances
                .Where(a => classIds.Contains(a.Session.ClassId)
                         && a.Session.Date >= start
                         && a.Session.Date <= end
                         && a.Session.Status == "Completed")
                .GroupBy(a => a.Session.ClassId)
                .Select(g => new
                {
                    ClassId = g.Key,
                    TotalSlots = g.Count(),
                    PresentCount = g.Count(x => x.Status == "Present")
                })
                .ToListAsync();

            return groupedData.ToDictionary(
                x => x.ClassId,
                x => (x.TotalSlots, x.PresentCount)
            );
        }
    }
}
