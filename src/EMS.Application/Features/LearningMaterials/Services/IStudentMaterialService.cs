using EMS.Application.Features.LearningMaterials.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.LearningMaterials.Services
{
    public interface IStudentMaterialService
    {
        Task<List<MaterialDto>> GetClassMaterialsAsync(Guid classId);
    }
}
