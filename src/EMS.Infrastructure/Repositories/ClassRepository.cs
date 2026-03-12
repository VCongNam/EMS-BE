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

        public async Task<IEnumerable<ClassEnrollment>> GetClassMemberAsync(Guid classId)
        {
            return await _context.ClassEnrollments
               .Include(ce => ce.Student)
               .ThenInclude(s => s.Account)
               .Where(ce => ce.ClassID == classId && ce.Status == "Active")
               .OrderByDescending(ce => ce.EnrolledDate)
               .ToListAsync();
        }
    }

}
