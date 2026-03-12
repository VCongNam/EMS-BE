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
    }

}
