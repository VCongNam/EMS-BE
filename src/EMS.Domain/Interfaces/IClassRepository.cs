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

    }

}
