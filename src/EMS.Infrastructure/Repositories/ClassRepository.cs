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

        public async Task<ClassEnrollment> AddEnrollmentAsync(ClassEnrollment enrollment)
        {
            await _context.ClassEnrollments.AddAsync(enrollment);
            await _context.SaveChangesAsync();
            return enrollment;
        }

        public async Task<IEnumerable<ClassEnrollment>> GetClassMemberAsync(Guid classId)
        {
            return await _context.ClassEnrollments
               .Include(ce => ce.Student)
               .ThenInclude(s => s.StudentNavigation)
               .Where(ce => ce.ClassId == classId && ce.Status == "Active")
               .OrderByDescending(ce => ce.EnrolledDate)
               .ToListAsync();
        }

        public async Task<bool> IsStudentAlreadyEnrolledAsync(Guid classId, Guid studentId)
        {
            return await _context.ClassEnrollments
                .AnyAsync(ce => ce.ClassId == classId && ce.StudentId == studentId);
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
