using EMS.Application.Features.Students.DTOs;
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

        public async Task<(List<ClassEnrollment> Items, int ToltalCount)> GetClassByStudentIdAsync(Guid studentId, int page, int size, string? status)
        {
            var query = _context.ClassEnrollments
                .Include(ce => ce.Class)
                .ThenInclude(c => c.Teacher.TeacherNavigation)
                .Where(ce => ce.StudentId == studentId)
                .AsNoTracking();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(ce => ce.Status == status);
            }
            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(ce => ce.EnrolledDate)
                .Skip((page - 1) * size)
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
            return await _context.ClassTa.AnyAsync(cta => cta.ClassId == classId && cta.Taid == taId);
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

        

    }
}
