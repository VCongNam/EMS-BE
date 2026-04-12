using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly ApplicationDbContext _context;

        public SubmissionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByAssignmentIdAsync(Guid assignmentId)
        {
            return await _context.Set<Submission>()
                .AsNoTracking()
                .Where(s => s.AssignmentId == assignmentId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Submission submission)
        {
            await _context.Submissions.AddAsync(submission);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Submission submission)
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubmissionAsync(Submission submission)
        {
            _context.Submissions.Remove(submission);
            await _context.SaveChangesAsync();
        }

        public async Task<Submission?> GetSubmissionWithAttachmentsAsync(Guid assignmentId, Guid studentId)
        {
            return await _context.Submissions
                .Include(s => s.SubmissionAttachments)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
        }

        public async Task<Submission?> GetByIdAsync(Guid submissionId)
        {
            return await _context.Submissions.FindAsync(submissionId);
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsForClassAsync(Guid classId)
        {
            return await _context.Submissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.GradeCategory)
                .Where(s => s.Assignment.ClassId == classId)
                .ToListAsync();
        }

        public async Task AddFeedbackAsync(SubmissionFeedback feedback)
        {
            await _context.SubmissionFeedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();
        }

        //Attachment
        public async Task AddAttachmentAsync(SubmissionAttachment attachment)
        {
            await _context.SubmissionAttachments.AddAsync(attachment);
            await _context.SaveChangesAsync();  
        }

        public async Task<SubmissionAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
        {
            return await _context.SubmissionAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);
        }

        public async Task RemoveAttachmentAsync(SubmissionAttachment attachment)
        {
            _context.SubmissionAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubmissionAttachmentsAsync(IEnumerable<SubmissionAttachment> attachments)
        {
            _context.SubmissionAttachments.RemoveRange(attachments);
        }

        public async Task AddAttachmentsAsync(IEnumerable<SubmissionAttachment> attachments)
        {
            await _context.SubmissionAttachments.AddRangeAsync(attachments);
        }

        public async Task<IEnumerable<Submission>> GetByAssignmentIdsAsync(List<Guid> assignmentIds)
        {
            return await _context.Submissions
                .Where(s => assignmentIds.Contains(s.AssignmentId))
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Submission> submissions)
        {
            await _context.Submissions.AddRangeAsync(submissions);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<Submission> submissions)
        {
            _context.Submissions.UpdateRange(submissions);
            await _context.SaveChangesAsync();
        }
    }

}
