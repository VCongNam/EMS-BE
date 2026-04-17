using EMS.Application.Features.Assignments.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.Services
{
    public interface IAssignmentService
    {
        Task<Guid> CreateAssignmentAsync(CreateAssignmentDto request);
        Task UpdateAssignmentAsync(Guid id, UpdateAssignmentDto request);
        Task DeleteAssignmentAsync(Guid id);
        Task<AssignmentDetailDto> GetAssignmentDetailAsync(Guid assignmentId);
        Task<IEnumerable<AssignmentSummaryDto>> GetAssignmentsByClassIdAsync(Guid classId);
        Task<AssignmentSubmissionsDto> GetAssignmentSubmissionsAsync(Guid assignmentId);

        // Grading functionalities moved from Gradebook
        Task GradeSubmissionAsync( Guid submissionId, GradeSubmissionDto request);
        Task GiveFeedbackAsync( Guid submissionId, FeedbackSubmissionDto request);
        Task<Guid> OfflineGradeAsync( Guid assignmentId, OfflineGradeDto request);
        Task<AssignmentSubmissionsListDto> GetSubmissionsForAssignmentAsync(Guid assignmentId);

        Task<StudentSubmissionDetailDto> GetStudentSubmissionDetailAsync(Guid assignmentId, Guid studentId);

        Task<bool> HasStudentSubmittedAsync(Guid assignmentId, Guid studentId);
    }
}
