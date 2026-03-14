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
                .Include(a => a.Role) // Load kèm thông tin Role
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await context.Accounts.Include(a => a.Role).FirstOrDefaultAsync(a => a.AccountId == id);

        }

        public async Task<Role> GetRoleByNameAsync(string roleName)
        {
            return await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        }

        public async Task UpdateAsync(Account account)
        {
            context.Accounts.Update(account);
            await context.SaveChangesAsync();
        }

    }
}
