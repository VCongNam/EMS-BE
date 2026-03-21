using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ITARepository
    {
        Task<TeachingAssistantTask> CreateTaskAsync(TeachingAssistantTask task);
        Task<IEnumerable<TeachingAssistantTask>> GetTasksByClassTAIdAsync(Guid classTaId);
    }
}
