using EMS.Application.Features.SystemAdmin.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Services
{
    public interface ISystemAdminService
    {
        Task<AdminDashboardDto> GetSystemDashboardAsync(DashboardFilterDto filter);
        Task<IEnumerable<TeacherGridDto>> GetTeachersGridAsync(string? searchTerm, string? statusFilter);
        Task<TeacherDetailDto> GetTeacherDetailAsync(Guid teacherId);
    }
}
