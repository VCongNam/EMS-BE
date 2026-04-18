using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ITeacherReportRepository
    {
        Task<List<Class>> GetActiveClassesAsync(Guid teacherId);
        Task<Dictionary<Guid, (int NewCount, int DropoutCount)>> GetEnrollmentStatsAsync(List<Guid> classIds, DateOnly start, DateOnly end);
        Task<Dictionary<Guid, (int TotalSlots, int PresentCount)>> GetAttendanceStatsAsync(List<Guid> classIds, DateOnly start, DateOnly end);
    }
}
