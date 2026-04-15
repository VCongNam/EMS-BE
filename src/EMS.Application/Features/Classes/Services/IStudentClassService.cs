using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Classes.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public interface IStudentClassService
    {
        Task<PagedResult<EnrolledClassDto>> GetMyClassesAsync(EnrolledClassFilter filter);
        Task<EnrolledClassDetailDto> GetClassDetailAsync(Guid classId);
        
    }
}
