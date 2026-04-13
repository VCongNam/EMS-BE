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
    public class GradeCategoryRepository : IGradeCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public GradeCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(GradeCategory gradeCategory)
        {
            await _context.GradeCategories.AddAsync(gradeCategory);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GradeCategory gradeCategory)
        {
            _context.GradeCategories.Update(gradeCategory);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(GradeCategory gradeCategory)
        {
            _context.GradeCategories.Remove(gradeCategory);
            await _context.SaveChangesAsync();
        }

        public async Task<GradeCategory?> GetByIdAsync(Guid gradeCategoryId)
        {
            return await _context.GradeCategories.FindAsync(gradeCategoryId);
        }

        public async Task<IEnumerable<GradeCategory>> GetByClassIdAsync(Guid classId)
        {
            return await _context.GradeCategories
                .Where(g => g.ClassId == classId)
                .ToListAsync();
        }

        public async Task UpdateWeightsAsync(IEnumerable<GradeCategory> categories)
        {
            _context.GradeCategories.UpdateRange(categories);
            await _context.SaveChangesAsync();
        }

        public async Task<List<GradeCategory>> GetStudentGradeDetailsAsync(Guid classId, Guid studentId)
        {
            return await _context.GradeCategories
                .Where(gc => gc.ClassId == classId)
                .Include(gc => gc.Assignments.Where(a => a.IsDeleted != true && a.Submissions.Any(s => s.StudentId == studentId)))
                    .ThenInclude(a => a.Submissions.Where(s => s.StudentId == studentId))
                        .ThenInclude(s => s.SubmissionFeedbacks)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
