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
    public class ClassRepository : IClassRepository
    {
        private readonly ApplicationDbContext _context;

        public ClassRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Class classroom)
        {
            await _context.Classes.AddAsync(classroom);
            await _context.SaveChangesAsync();
        }

        public async Task<Class?> GetByIdAsync(Guid classId)
        {
            return await _context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClassId == classId && (c.IsDeleted == null || c.IsDeleted == false));
        }

        public async Task<IEnumerable<Class>> GetClassesByTeacherIdAsync(Guid teacherId)
        {
            return await _context.Classes
                .AsNoTracking()
                .Where(c => c.TeacherId == teacherId && (c.IsDeleted == null || c.IsDeleted == false))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Class classroom)
        {
            _context.Classes.Update(classroom);
            await _context.SaveChangesAsync();
        }
    }
}
