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

        Task<List<Submission>> GetSubmissionsForCalcAsync(Guid classId, DateTime startDate, DateTime endDate);
        Task<List<Attendance>> GetAttendancesForCalcAsync(Guid classId, DateOnly startDate, DateOnly endDate);
        Task<List<Class>> GetClassesByTeacherAndPeriodAsync(Guid teacherId, int month, int year, string? searchTerm);

        Task<Dictionary<Guid, int>> GetActiveStudentCountsByClassesAsync(List<Guid> classIds);
        Task<List<ProgressReport>> GetReportsByClassesAndPeriodAsync(List<Guid> classIds, int month, int year);
        Task<Class?> GetClassByIdAsync(Guid classId);
        // Thêm vào IProgressReportRepository.cs
        Task<int> GetTotalSessionsInPeriodAsync(Guid classId, DateOnly startDate, DateOnly endDate);
    }
}
