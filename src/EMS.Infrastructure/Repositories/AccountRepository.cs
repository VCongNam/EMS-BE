using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext context;

        public AccountRepository(ApplicationDbContext context)
        {
            this.context = context;
        }
        
        public async Task<Account> AddAsync(Account account)
        {
            await context.Accounts.AddAsync(account);
            await context.SaveChangesAsync();
            return account;
        }
        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Email == email && a.IsDeleted != true);
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await context.Accounts
        .Include(a => a.Role)
        .Include(a => a.Teacher)             // Lấy thêm thông tin Teacher
        .Include(a => a.Students)             // Lấy thêm thông tin Student
        .Include(a => a.TeachingAssistant)   // Lấy thêm thông tin TA
        .FirstOrDefaultAsync(a => a.AccountId == id);

        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        }

        public async Task UpdateAsync(Account account)
        {
            context.Accounts.Update(account);
            await context.SaveChangesAsync();
        }

        public async Task<Account?> GetByPhoneAsync(string phone)
        {
            return await context.Accounts
                .Include(a => a.Role)
                .Include(a => a.Students) // Cần Students để chọn Profile
                .FirstOrDefaultAsync(a => a.PhoneNumber == phone && a.IsDeleted != true);
        }
    }
}
