using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly ApplicationDbContext _context;

        public AssignmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Assignment assignment)
        {
            await _context.Assignments.AddAsync(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Assignment assignment)
        {
            _context.Assignments.Update(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task<Assignment?> GetByIdAsync(Guid assignmentId)
        {
            return await _context.Assignments
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.IsDeleted != true);
        }
        public async Task<Assignment?> GetWithClassByIdAsync(Guid assignmentId)
        {
            return await _context.Assignments
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.IsDeleted != true);
        }

        public async Task<Assignment?> GetByIdWithDetailsAsync(Guid assignmentId)
        {
            return await _context.Assignments
                .Include(a => a.Author)
                .Include(a => a.GradeCategory)
                .Include(a => a.AssignmentAttachments)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.IsDeleted != true);
        }

        public async Task<IEnumerable<Assignment>> GetByClassIdAsync(Guid classId)
        {
            return await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Author)
                .Include(a => a.GradeCategory)
                .Include(a => a.AssignmentAttachments)
                .Where(a => a.ClassId == classId && a.IsDeleted != true)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        //Student Learning Portal
        public async Task<int> CountPendingAssignmentAsync(Guid classId, Guid studentId)
        {
            int count = await _context.Assignments
                .Where(a => a.ClassId == classId && a.IsDeleted != true
                    && a.DueDate >= DateTime.UtcNow)
                .Where(a => a.Submissions.Any(
                    s => s.AssignmentId == a.AssignmentId
                    && s.StudentId == studentId))
                .CountAsync();
            return count;
        }

        public async Task<(IEnumerable<Assignment> Items, int TotalCount)> GetStudentAssignmentsAsync(
            Guid classId, Guid studentId, int page, int size)
        {
            var query = _context.Assignments
                .Where(a => a.ClassId == classId && a.IsDeleted == false)
                .AsNoTracking();

            int totalCount = await query.CountAsync();

            var items = await query
                .Include(a => a.Submissions
                    .Where(s => s.StudentId == studentId))
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task<(Assignment? Assignment, Submission? Submission)> GetAssignmentDetailAsync(Guid assignmentId, Guid studentId)
        {
            var dbResult = await _context.Assignments
                .Include(a => a.AssignmentAttachments)
                .Where(a =>a.AssignmentId == assignmentId && a.IsDeleted != true)
                .Select(a => new
                {
                    Assignment = a,
                    Submission = _context.Submissions
                    .Include(s => s.SubmissionAttachments)
                    .Include(s => s.SubmissionFeedbacks)
                    .FirstOrDefault(s => s.AssignmentId == a.AssignmentId && s.StudentId == studentId)
                }).AsNoTracking().FirstOrDefaultAsync();
            if (dbResult == null) return (null, null);
            return (dbResult.Assignment, dbResult.Submission);
        }

        public async Task AddAttachmentAsync(AssignmentAttachment attachment)
        {
            await _context.AssignmentAttachments.AddAsync(attachment);
            await _context.SaveChangesAsync();
        }

        public async Task<AssignmentAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
        {
            return await _context.AssignmentAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);
        }

        public async Task RemoveAttachmentAsync(AssignmentAttachment attachment)
        {
            _context.AssignmentAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }

    }
}
