using EMS.Application.Features.Classes.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public interface IClassService
    {
        Task<Guid> CreateClassAsync(CreateClassDto request);
        Task<IEnumerable<ClassSummaryDto>> GetTeacherDashboardAsync(Guid teacherId);
        Task<ClassDetailDto> GetClassDetailAsync(Guid classId);
        Task UpdateClassAsync(Guid classId, UpdateClassDto request);
        Task ArchiveClassAsync(Guid classId);

    }

}
