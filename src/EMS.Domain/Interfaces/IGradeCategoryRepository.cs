using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IGradeCategoryRepository
    {
        Task AddAsync(GradeCategory gradeCategory);
        Task UpdateAsync(GradeCategory gradeCategory);
        Task DeleteAsync(GradeCategory gradeCategory);
        Task<GradeCategory?> GetByIdAsync(Guid gradeCategoryId);
        Task<IEnumerable<GradeCategory>> GetByClassIdAsync(Guid classId);
        Task UpdateWeightsAsync(IEnumerable<GradeCategory> categories);
    }
}
