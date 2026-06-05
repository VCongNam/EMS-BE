using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMS.Application.Features.Auth.DTOs;

namespace EMS.Application.Features.Auth.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<bool> VerifyEmailAsync(VerifyEmailRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<AuthResponse> SelectProfileAsync(Guid studentId);
        Task<bool> VerifyOnboardingAsync(OnboardingRequest request);
        Task<bool> ResendOtpAsync(ResendOtpRequest request);
    }
}
