using EMS.Application.Features.Classes.DTOs;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public interface IClassTAService
    {
        Task<IEnumerable<ClassTADto>> GetClassTAsAsync(Guid classId);
        Task<Guid> AssignTAAsync(Guid classId, AssignTADto request);
        Task UpdateTAPermissionAsync(Guid classId, Guid taId, UpdateTAPermissionDto request);
        Task<Guid> CreateTaskAsync(CreateTaskDto request);
        Task<IEnumerable<TaskDto>> GetTasksAsync(Guid classTaId);
        Task<IEnumerable<TAViewDto>> GetTAsByTeacherIdAsync();
        Task<TAProfileDto?> FindTAByEmailAsync(string email);
    }
}
