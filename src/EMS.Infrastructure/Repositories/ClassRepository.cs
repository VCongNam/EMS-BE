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

        public async Task<Class?> GetClassDetailByIdAsync(Guid classId)
        {
            return await _context.Classes
                .AsNoTracking()
                .Include(c => c.Subject)
                .Include(c => c.ClassSchedules)
                .Include(c => c.ClassEnrollments)
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

        //Student Management
        public async Task AddEnrollmentAsync(ClassEnrollment enrollment)
        {
            await _context.ClassEnrollments.AddAsync(enrollment);
        }

        public void UpdateEnrollment(ClassEnrollment enrollment)
        {
            _context.ClassEnrollments.Update(enrollment);
        }

        public async Task<List<ClassEnrollment>> GetEnrollmentsByStudentIdsAsync(Guid classId, List<Guid> studentIds)
        {
            return await _context.ClassEnrollments
                .Where(ce => ce.ClassId == classId && studentIds.Contains(ce.StudentId))
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetActiveStudentCountAsync(Guid classId)
        {
            return await _context.ClassEnrollments
                .CountAsync(ce => ce.ClassId == classId && ce.Status == "Active");
        }

        public async Task<IEnumerable<ClassEnrollment>> GetClassMemberAsync(Guid classId)
        {
            return await _context.ClassEnrollments
               .Include(ce => ce.Student)
               .ThenInclude(s => s.Account)
               .Where(ce => ce.ClassId == classId)
               .OrderByDescending(ce => ce.EnrolledDate)
               .ToListAsync();
        }

        public async Task<Class?> GetClassStaffAsync(Guid classId)
        {
            return await _context.Classes
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.TeacherNavigation)
                .Include(c => c.ClassTa)
                    .ThenInclude(cta => cta.Ta)
                        .ThenInclude(ta => ta.Ta)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClassId == classId && (c.IsDeleted == null || c.IsDeleted == false));
        }

        public async Task<bool> IsStudentAlreadyEnrolledAsync(Guid classId, Guid studentId)
        {
            return await _context.ClassEnrollments
                .AnyAsync(ce => ce.ClassId == classId && ce.StudentId == studentId);
        }

        public async Task<(List<ClassEnrollment> Items, int ToltalCount)> GetClassByStudentIdAsync(Guid studentId, int page, int size)
        {
            var query = _context.ClassEnrollments
                .Include(ce => ce.Class)
                .ThenInclude(c => c.Teacher.TeacherNavigation)
                .Where(ce => ce.StudentId == studentId)
                .AsNoTracking();
                query = query.Where(ce => ce.Status != "Archive");
            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(ce => ce.EnrolledDate)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task<ClassEnrollment?> GetClassSummaryAsync(Guid classId, Guid studentId)
        {
            var result = await _context.ClassEnrollments
                .Include(ce => ce.Class)
                    .ThenInclude(c => c.Teacher.TeacherNavigation)
                .Where(ce => ce.ClassId == classId && ce.StudentId == studentId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return result;
        }

        public async Task<(List<Post> Items, int TotalCount)> GetClassPostAsync(Guid classId, int page, int size, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Posts
                .Where(p => p.ClassId == classId)
                .AsNoTracking();
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= fromDate);
            }
            int totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(p =>  p.CreatedAt)
                            .Skip((page-1) * size)
                            .Take(size)
                            .ToListAsync();
            return (items, totalCount);
        }

        //Teaching Assistant Management

        public async Task<IEnumerable<ClassTum>> GetTAsByClassIdAsync(Guid classId)
        {
            return await _context.ClassTa
                .Include(cta => cta.Ta)
                    .ThenInclude(ta => ta.Ta) 
                .Where(cta => cta.ClassId == classId)
                .ToListAsync();
        }

        public async Task<bool> IsTAAssignedAsync(Guid classId, Guid taId)
        {
            return await _context.ClassTa.AnyAsync(cta => 
                    cta.ClassId == classId &&
                    cta.Taid == taId &&
                    cta.Status != "Deactive");
        }

        public async Task<ClassTum> AddClassTAAsync(ClassTum classTa)
        {
            await _context.ClassTa.AddAsync(classTa);
            await _context.SaveChangesAsync();
            return classTa;
        }

        public async Task<ClassTum> GetClassTAAsync(Guid classId, Guid taId)
        {
            return await _context.ClassTa.FirstOrDefaultAsync(cta => cta.ClassId == classId && cta.Taid == taId);
        }

        public async Task UpdateClassTAAsync(ClassTum classTa)
        {
            _context.ClassTa.Update(classTa);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassEnrollment?> GetEnrollmentAsync(Guid classId, Guid studentId)
        {
            return await _context.ClassEnrollments
                .FirstOrDefaultAsync(ce => ce.ClassId == classId && ce.StudentId == studentId);
        }

        public async Task<IEnumerable<ClassTum>> GetClassesByTAIdAsync(Guid taId)
        {
            return await _context.ClassTa
                .Where(ct => ct.Taid == taId && ct.Class.Status != "Archived")
                .Include(ct => ct.Class)
                    .ThenInclude(cls => cls.Subject)
                .Include(ct => ct.Class)
                    .ThenInclude(cls => cls.Teacher)
                        .ThenInclude(t => t.TeacherNavigation)
                .Include(ct => ct.Class)
                    .ThenInclude(cls => cls.ClassEnrollments)
                .Include(ct => ct.Class)
                    .ThenInclude(cls => cls.ClassSchedules)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsByClassIdAsync(Guid classId)
        {
            var students = await _context.ClassEnrollments
                .AsNoTracking()
                .Where(cm => cm.ClassId == classId
                          && cm.Status == "Active")   
                .Include(cm => cm.Student)          
                .Select(cm => cm.Student)             
                .ToListAsync();

            return students;
        }
    }
}
