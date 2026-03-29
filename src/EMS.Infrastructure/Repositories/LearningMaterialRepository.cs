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
    public class LearningMaterialRepository : ILearningMaterialRepository
    {
        private readonly ApplicationDbContext _context;

        public LearningMaterialRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(LearningMaterial material)
        {
            await _context.LearningMaterials.AddAsync(material);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LearningMaterial material)
        {
            _context.LearningMaterials.Update(material);
            await _context.SaveChangesAsync();
        }

        public async Task<LearningMaterial?> GetByIdAsync(Guid materialId)
        {
            return await _context.LearningMaterials
                .FirstOrDefaultAsync(m => m.MaterialId == materialId && m.IsDeleted != true);
        }

        public async Task<LearningMaterial?> GetByIdWithDetailsAsync(Guid materialId)
        {
            return await _context.LearningMaterials
                .Include(m => m.Author)
                .Include(m => m.MaterialAttachments)
                .FirstOrDefaultAsync(m => m.MaterialId == materialId && m.IsDeleted != true);
        }

        public async Task<IEnumerable<LearningMaterial>> GetByClassIdAsync(Guid classId)
        {
            return await _context.LearningMaterials
                .AsNoTracking()
                .Include(m => m.Author)
                .Where(m => m.ClassId == classId && m.IsDeleted != true)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        // Attachment management
        public async Task AddAttachmentAsync(MaterialAttachment attachment)
        {
            await _context.MaterialAttachments.AddAsync(attachment);
            await _context.SaveChangesAsync();
        }

        public async Task<MaterialAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
        {
            return await _context.MaterialAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);
        }

        public async Task RemoveAttachmentAsync(MaterialAttachment attachment)
        {
            _context.MaterialAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }
    }
}
