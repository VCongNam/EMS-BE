using EMS.Application.Features.Assignments.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.Services
{
    public interface IAssignmentService
    {
        Task<Guid> CreateAssignmentAsync(CreateAssignmentDto request);
        Task UpdateAssignmentAsync(Guid id, UpdateAssignmentDto request);
        Task DeleteAssignmentAsync(Guid id);
        Task<IEnumerable<AssignmentSummaryDto>> GetAssignmentsByClassIdAsync(Guid classId);
        Task<AssignmentSubmissionsDto> GetAssignmentSubmissionsAsync(Guid assignmentId);


    }
}
