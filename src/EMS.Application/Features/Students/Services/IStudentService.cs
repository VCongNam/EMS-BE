using EMS.Application.Features.Students.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public interface IStudentService
    {
        Task<Guid> CreateStudentAsync(CreateStudentDto request);
        Task<PagedResult<EnrolledClassDto>> GetMyClassesAsync(EnrolledClassFilter filter);
    }
}
