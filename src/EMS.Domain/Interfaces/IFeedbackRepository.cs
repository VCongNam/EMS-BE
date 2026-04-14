using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IFeedbackRepository
    {
        Task AddAsync(SystemFeedback feedback);
        Task<IEnumerable<SystemFeedback>> GetAllAsync(string? type, string? status);
        Task<SystemFeedback?> GetByIdAsync(Guid id);
        Task UpdateAsync(SystemFeedback feedback);
    }
}
