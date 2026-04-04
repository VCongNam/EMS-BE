using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ISubmissionRepository
    {
        Task<IEnumerable<Submission>> GetSubmissionsByAssignmentIdAsync(Guid assignmentId);
        Task AddAsync(Submission submission);
        Task UpdateAsync(Submission submission);
        Task<Submission?> GetSubmissionWithAttachmentsAsync(Guid assignmentId, Guid studentId);
        Task DeleteSubmissionAttachmentsAsync(IEnumerable<SubmissionAttachment> attachments);

        //Attachment
        Task AddAttachmentAsync(SubmissionAttachment attachment);
        Task<SubmissionAttachment?> GetAttachmentByIdAsync(Guid attachmentId);
        Task RemoveAttachmentAsync(SubmissionAttachment attachment);
        Task AddAttachmentsAsync(IEnumerable<SubmissionAttachment> attachments);
    }
}
