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
        Task<AdminDashboardDto> GetSystemDashboardAsync();
        Task<IEnumerable<AccountSummaryDto>> GetAllAccountsAsync(string? role, string? status);
        Task<AccountDetailDto> GetAccountDetailAsync(Guid accountId);
        Task ChangeAccountStatusAsync(Guid accountId, ChangeAccountStatusDto request);
        Task<IEnumerable<SystemLogDto>> GetSuspiciousActivitiesAsync(int limit = 50);
    }
}
