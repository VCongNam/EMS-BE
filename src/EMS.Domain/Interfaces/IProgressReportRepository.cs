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
        Task<ProgressReport?> GetByIdAsync(Guid reportId);
        Task<ProgressReport?> GetByIdWithDetailsAsync(Guid reportId);
        Task<IEnumerable<ProgressReport>> GetReportsByTeacherIdAsync(Guid teacherId);
        Task UpdateAsync(ProgressReport report);
        Task DeleteAsync(ProgressReport report);
    }
}
