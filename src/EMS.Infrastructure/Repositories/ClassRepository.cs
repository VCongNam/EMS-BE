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
                .Include(c => c.Subject)
                .Include(c => c.ClassSchedules)
                .Include(c => c.ClassEnrollments)
                .Where(c => c.TeacherId == teacherId && (c.IsDeleted == null || c.IsDeleted == false))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Subject?> GetSubjectByNameAndGradeAsync(string subjectName, short gradeLevel)
        {
            return await _context.Subjects
                .FirstOrDefaultAsync(s => s.SubjectName == subjectName && s.GradeLevel == gradeLevel && (s.IsDeleted == null || s.IsDeleted == false));
        }

        public async Task AddSubjectAsync(Subject subject)
        {
            await _context.Subjects.AddAsync(subject);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSchedulesAsync(Guid classId)
        {
            var schedules = await _context.ClassSchedules.Where(s => s.ClassId == classId).ToListAsync();
            _context.ClassSchedules.RemoveRange(schedules);
            await _context.SaveChangesAsync();
        }

        public async Task AddSchedulesAsync(IEnumerable<ClassSchedule> schedules)
        {
            await _context.ClassSchedules.AddRangeAsync(schedules);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Class classroom)
        {
            _context.Classes.Update(classroom);
            await _context.SaveChangesAsync();
        }
    }
}
