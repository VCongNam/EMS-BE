using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IProgressReportRepository
    {
        Task AddAsync(ProgressReport report);
        Task UpdateAsync(ProgressReport report);
        Task DeleteAsync(ProgressReport report);
        Task<ProgressReport?> GetByIdAsync(Guid reportId);
        Task<IEnumerable<ProgressReport>> GetReportsByClassAndPeriodAsync(Guid classId, int month, int year);
        Task<bool> IsReportExistAsync(Guid studentId, Guid classId, int month, int year);
        Task<IEnumerable<ClassEnrollment>> GetActiveStudentsInClassAsync(Guid classId);
    }
}
