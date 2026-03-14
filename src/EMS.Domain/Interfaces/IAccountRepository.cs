using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<Account?> GetByIdAsync(Guid accountId);
        Task<Account> AddAsync(Account account);
        Task UpdateAsync(Account account);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task CreateStudentAccountAsync(Account account, Student student);
    }
}
