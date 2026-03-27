using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IAssignmentRepository
    {
        Task AddAsync(Assignment assignment);
        Task UpdateAsync(Assignment assignment);
        Task<Assignment?> GetByIdAsync(Guid assignmentId);
        Task<IEnumerable<Assignment>> GetByClassIdAsync(Guid classId);
        //Task<IEnumerable<Assignment>> GetByClassIdAndStudentIdAsync(Guid classId, Guid studentId);
        Task<int> CountPendingAssignmentAsync(Guid classId, Guid studentId);

    }
}
