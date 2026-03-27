using EMS.Application.Features.Students.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public interface IStudentClassService
    {
        Task<PagedResult<EnrolledClassDto>> GetMyClassesAsync(EnrolledClassFilter filter);
        Task<EnrolledClassDetailDto> GetClassDetailAsync(Guid classId);
        Task<PagedResult<PostDto>> GetClassPostsAsync(Guid classId, PostFilter filter);
    }
}
