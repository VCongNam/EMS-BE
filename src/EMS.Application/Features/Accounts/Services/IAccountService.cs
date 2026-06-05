using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
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
        Task<bool> ChangePasswordAsync(Guid accountId, ChangePasswordRequest request);
        Task<UserProfileResponse> UpdateTeacherProfileAsync(Guid accountId, UpdateTeacherProfileRequest request);
        Task<UserProfileResponse> UpdateTAProfileAsync(Guid accountId, UpdateTAProfileRequest request);
        Task<UserProfileResponse> UpdateStudentProfileAsync(Guid accountId, Guid studentId, UpdateStudentProfileRequest request);

        Task<string> UpdateAvatarAsync(UploadAvatarDto request);
    }
}
