using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ISystemAdminRepository
    {
        // Nhóm đếm số liệu Dashboard
        Task<int> CountAccountsByRoleAsync(string roleName);
        Task<int> CountOngoingClassesAsync();

        Task<IEnumerable<Account>> GetAccountsInPeriodAsync(DateTime start, DateTime end);
        Task<IEnumerable<Post>> GetPostsInPeriodAsync(DateTime start, DateTime end);
        Task<IEnumerable<Assignment>> GetAssignmentsInPeriodAsync(DateTime start, DateTime end);
        Task<IEnumerable<Session>> GetSessionsInPeriodAsync(DateTime start, DateTime end);

        // Nhóm thao tác với Teacher
        Task<IEnumerable<Teacher>> GetAllTeachersGridAsync(string? searchTerm, string? statusFilter);
        Task<Teacher?> GetTeacherByIdAsync(Guid teacherId);
    }
}
