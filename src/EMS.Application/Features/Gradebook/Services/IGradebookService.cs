using EMS.Application.Features.Gradebook.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.Gradebook.Services
{
    public interface IGradebookService
    {
        // Grade Categories
        Task<IEnumerable<GradeCategoryDto>> GetGradeCategoriesByClassAsync(Guid classId);
        Task<Guid> AddGradeCategoryAsync(Guid classId, CreateGradeCategoryDto request);
        Task UpdateGradeCategoryAsync(Guid classId, UpdateGradeCategoryDto request);
        Task BulkUpdateCategoriesAsync(Guid classId, BulkUpdateGradeCategoryDto request);
        Task DeleteGradeCategoryAsync(Guid classId, Guid categoryId);

        // Class Gradebook matrix
        Task<GradebookResponseDto> GetClassGradebookAsync(Guid classId);

        // Export
        Task<byte[]> ExportClassGradebookToExcelAsync(Guid classId);
        Task<byte[]> ExportClassGradebookToPdfAsync(Guid classId);
    }
}
