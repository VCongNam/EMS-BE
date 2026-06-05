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
        Task<IEnumerable<Class>> GetClassesByTeacherIdAsync(Guid teacherId);
        Task<Class?> GetByIdAsync(Guid classId);
        Task<Class?> GetClassDetailByIdAsync(Guid classId);
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

        Task<ClassEnrollment> GetClassSummaryAsync(Guid classId, Guid studentId);

        Task<IEnumerable<ClassTum>> GetClassesByTAIdAsync(Guid taId);
        Task<IEnumerable<Student>> GetStudentsByClassIdAsync(Guid classId);
        Task<Class?> GetClassStaffAsync(Guid classId);

        //Student Management
        void UpdateEnrollment(ClassEnrollment enrollment);
        Task<bool> IsStudentAlreadyEnrolledAsync(Guid classId, Guid studentId);
        Task AddEnrollmentAsync(ClassEnrollment enrollment);
        Task SaveChangesAsync();
        Task<(List<Post> Items, int TotalCount)> GetClassPostAsync(Guid classId, int page, int size, DateTime? fromDate, DateTime? toDate);
        Task<(List<ClassEnrollment> Items, int ToltalCount)> GetClassByStudentIdAsync(Guid studentId, int page, int size);
        Task<ClassEnrollment?> GetEnrollmentAsync(Guid classId, Guid studentId);
        Task<int> GetActiveStudentCountAsync(Guid classId);
        Task<List<ClassEnrollment>> GetEnrollmentsByStudentIdsAsync(Guid classId, List<Guid> studentIds);
    }

}
