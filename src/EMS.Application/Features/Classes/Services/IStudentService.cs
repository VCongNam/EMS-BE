using EMS.Application.Features.Classes.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public interface IStudentService
    {
        Task<Guid> CreateStudentAsync(CreateStudentRequest request);
    }
}
