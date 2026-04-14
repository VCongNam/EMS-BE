using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IStudentRepository
    {
        Task<Student?> IsStudentExistAsync(Guid accountId, string name, DateOnly dob);
        Task<Student?> GetByIdAsync(Guid studentId);
        Task AddAsync(Student student);
        Task UpdateAsync(Student student);
        Task SaveChangesAsync();
    }
}
