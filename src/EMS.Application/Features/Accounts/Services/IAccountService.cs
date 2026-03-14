using EMS.Application.Features.Accounts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Services
{
    public interface IAccountService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<bool> VerifyEmailAsync(VerifyEmailRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);


        Task<UserProfileResponse> GetProfileAsync(Guid accountId);
        Task<UserProfileResponse> UpdateProfileAsync(Guid accountId, UpdateProfileRequest reguest);
        Task<bool> ChangePassewordAsync(Guid accountId, ChangePasswordRequest request);

    }
}
