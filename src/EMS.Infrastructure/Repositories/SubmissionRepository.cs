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
             _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }

        public async Task<Submission?> GetSubmissionWithAttachmentsAsync(Guid assignmentId, Guid studentId)
        {
            return await _context.Submissions
                .Include(s => s.SubmissionAttachments)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
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
    }

}
