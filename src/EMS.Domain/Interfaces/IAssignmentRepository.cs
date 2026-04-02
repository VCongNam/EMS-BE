using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IAssignmentRepository
    {
        Task AddAsync(Assignment assignment);
        Task UpdateAsync(Assignment assignment);
        Task<Assignment?> GetByIdAsync(Guid assignmentId);
        Task<Assignment?> GetByIdWithDetailsAsync(Guid assignmentId);
        //Student Learning Portal
        Task<IEnumerable<Assignment>> GetByClassIdAsync(Guid classId);
        Task<int> CountPendingAssignmentAsync(Guid classId, Guid studentId);
        Task<(List<(Assignment Assignment, Submission? Submission)> Items, int TotalCount)> GetStudentAssignmentsAsync(
            Guid classId, Guid studentId, int page, int size);
        Task<(Assignment? Assignment, Submission? Submission)> GetAssignmentDetailAsync(Guid assignmentId, Guid studentId);

        // Attachment management
        Task AddAttachmentAsync(AssignmentAttachment attachment);
        Task<AssignmentAttachment?> GetAttachmentByIdAsync(Guid attachmentId);
        Task RemoveAttachmentAsync(AssignmentAttachment attachment);
    }
}
