using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Services
{
    public interface IAccountService
    {

        Task<UserProfileResponse> GetProfileAsync(Guid accountId);
        //Task<UserProfileResponse> UpdateProfileAsync(Guid accountId, UpdateProfileRequest reguest);
        Task<bool> ChangePasswordAsync(Guid accountId, ChangePasswordRequest request);
        Task<UserProfileResponse> UpdateTeacherProfileAsync(Guid accountId, UpdateTeacherProfileRequest request);
        Task<UserProfileResponse> UpdateTAProfileAsync(Guid accountId, UpdateTAProfileRequest request);
        Task<UserProfileResponse> UpdateStudentProfileAsync(Guid accountId, UpdateStudentProfileRequest request);
    }
}
