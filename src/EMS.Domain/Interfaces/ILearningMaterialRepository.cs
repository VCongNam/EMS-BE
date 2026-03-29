using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ILearningMaterialRepository
    {
        Task AddAsync(LearningMaterial material);
        Task UpdateAsync(LearningMaterial material);
        Task<LearningMaterial?> GetByIdAsync(Guid materialId);
        Task<LearningMaterial?> GetByIdWithDetailsAsync(Guid materialId);
        Task<IEnumerable<LearningMaterial>> GetByClassIdAsync(Guid classId);

        // Attachment management
        Task AddAttachmentAsync(MaterialAttachment attachment);
        Task<MaterialAttachment?> GetAttachmentByIdAsync(Guid attachmentId);
        Task RemoveAttachmentAsync(MaterialAttachment attachment);
    }
}
