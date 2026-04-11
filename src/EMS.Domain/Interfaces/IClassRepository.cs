using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IClassRepository
    {
        Task AddAsync(Class classroom);

        Task<IEnumerable<ClassEnrollment>> GetClassMemberAsync(Guid classId);
        Task<bool> IsStudentAlreadyEnrolledAsync(Guid classId, Guid studentId);
        Task<ClassEnrollment> AddEnrollmentAsync(ClassEnrollment enrollment);
        Task<IEnumerable<Class>> GetClassesByTeacherIdAsync(Guid teacherId);
        Task<Class?> GetByIdAsync(Guid classId);
        Task UpdateAsync(Class classroom);

        Task<Subject?> GetSubjectByNameAndGradeAsync(string subjectName, short gradeLevel);
        Task AddSubjectAsync(Subject subject);

        Task DeleteSchedulesAsync(Guid classId);
        Task AddSchedulesAsync(IEnumerable<ClassSchedule> schedules);

        Task<IEnumerable<ClassTum>> GetTAsByClassIdAsync(Guid classId);
        Task<bool> IsTAAssignedAsync(Guid classId, Guid taId);
        Task<ClassTum> AddClassTAAsync(ClassTum classTa);
        Task<ClassTum> GetClassTAAsync(Guid classId, Guid taId);
        Task UpdateClassTAAsync(ClassTum classTa);
        Task<(List<ClassEnrollment> Items, int ToltalCount)> GetClassByStudentIdAsync(Guid studentId, int page, int size, string? status);
        Task<ClassEnrollment> GetClassSummaryAsync(Guid classId, Guid studentId);
        Task<(List<Post> Items, int TotalCount)> GetClassPostAsync(Guid classId, int page, int size, DateTime? fromDate, DateTime? toDate);

        Task<ClassEnrollment?> GetEnrollmentAsync(Guid classId, Guid studentId);
        Task UpdateEnrollmentAsync(ClassEnrollment enrollment);

    }

}
