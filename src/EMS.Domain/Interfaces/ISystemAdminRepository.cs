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
        Task<int> CountAccountsByRoleAsync(string roleName);
        Task<int> CountActiveClassesAsync();
        Task<int> CountNewRegistrationsThisMonthAsync();

        Task<IEnumerable<Account>> GetAllAccountsAsync(string? role, string? status);
        Task<Account?> GetAccountByIdAsync(Guid accountId);
        Task UpdateAccountAsync(Account account);
        Task<int> CountClassesByTeacherAsync(Guid teacherId);

    }
}
