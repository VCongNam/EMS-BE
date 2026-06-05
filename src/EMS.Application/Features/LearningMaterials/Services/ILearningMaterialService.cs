using EMS.Application.Features.LearningMaterials.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.LearningMaterials.Services
{
    public interface ILearningMaterialService
    {
        Task<Guid> CreateLearningMaterialAsync(CreateLearningMaterialDto request);
        Task UpdateLearningMaterialAsync(Guid id, UpdateLearningMaterialDto request);
        Task DeleteLearningMaterialAsync(Guid id);
        Task<LearningMaterialResponseDto> GetLearningMaterialDetailAsync(Guid materialId);
        Task<IEnumerable<LearningMaterialSummaryDto>> GetLearningMaterialsByClassIdAsync(Guid classId);
    }
}
