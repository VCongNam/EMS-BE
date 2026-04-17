using EMS.Application.Features.Assignments.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.Services
{
    public interface IStudentAssignmentService
    {
        Task<PagedResult<AssignmentItemDto>> GetClassAssignmentsAsync(Guid classId, AssignmentFilter filter);
        Task<StudentAssignmentDetailDto> GetClassAssignmentsDetailAsync(Guid assignmentId);
        Task<bool> SubmitAssignmentAsync(Guid assignmentId, SubmitAssignmentRequest request);
        Task<bool> UnsubmitAssignmentAsync(Guid assignmentId);
    }
}
