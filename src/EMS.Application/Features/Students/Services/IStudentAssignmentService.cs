using EMS.Application.Features.Students.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public interface IStudentAssignmentService
    {
        Task<PagedResult<AssignmentItemDto>> GetClassAssignmentsAsync(Guid classId, AssignmentFilter filter);
        Task<AssignmentDetailDto> GetClassAssignmentsDetailAsync(Guid assignmentId);
        Task<bool> SubmitAssignmentAsync(Guid assignmentId, SubmitAssignmentRequest request);
        Task<bool> UnsubmitAssignmentAsync(Guid assignmentId);
    }
}
