using EMS.Application.Common.Interfaces;  
using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;


namespace EMS.Application.Features.Accounts.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IJwtTokenGenerator jwtTokenGenerator;
        private readonly IEmailService emailService;

        public AccountService(IAccountRepository accountRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService)
        {
            this.accountRepository = accountRepository;
            this.jwtTokenGenerator = jwtTokenGenerator;
            this.emailService = emailService;
        }

        
        //Đăng ký
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingAccount = await accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null) throw new Exception("Email đã được sử dụng!");

            // 2. Tự động lấy RoleID của "Teacher" từ Database
            var teacherRole = await accountRepository.GetRoleByNameAsync("Teacher");
            if (teacherRole == null) throw new Exception("Lỗi hệ thống: Không tìm thấy Role 'Teacher' trong CSDL!");

            string otp = new Random().Next(100000, 999999).ToString();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newAccount = new Account
            {
                AccountId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                RoleId = teacherRole.RoleId, // Tự động gán quyền Teacher
                Status = "Unverified",
                VerificationToken = otp,
                VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };
            var saveAccount = await accountRepository.AddAsync(newAccount);
            await emailService.SendEmailAsync(request.Email, "EMS-OTP", $"Mã OTP của bạn là: {otp}");
            return new AuthResponse
            {
                AccountId = saveAccount.AccountId,
                Email = saveAccount.Email,
                FullName = saveAccount.FullName
            };
        }
        public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");
            if (account.Status == "Active") throw new Exception("Tài khoản đã được xác thực!");

            if (account.VerificationToken != request.OtpCode) throw new Exception("Mã OTP không chính xác!");
            if (account.VerificationTokenExpiresAt < DateTime.UtcNow) throw new Exception("Mã OTP đã hết hạn!");

            account.Status = "Active";
            account.VerificationToken = null;
            account.VerificationTokenExpiresAt = null;

            await accountRepository.UpdateAsync(account);
            return true;
        }

        //Đăng nhập
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash);

            if (!isPasswordValid) throw new Exception("Sai mật khẩu!");
            var roleName = account.Role?.RoleName ?? throw new Exception("Tài khoản bị lỗi dữ liệu phân quyền!");
            var token = jwtTokenGenerator.GenerateToken(account, roleName);
            return new AuthResponse
            {
                AccountId = account.AccountId,
                Email = account.Email,
                FullName = account.FullName,
                Token = token
            };
        }

        //Quên mật khẩu 
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) return true; // Trả về true để bảo mật tránh lộ email tồn tại

            string resetOtp = new Random().Next(100000, 999999).ToString();
            account.ResetPasswordToken = resetOtp;
            account.ResetPasswordTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

            await accountRepository.UpdateAsync(account);
            await emailService.SendEmailAsync(request.Email, "EMS - Khôi phục mật khẩu", $"Mã khôi phục mật khẩu của bạn là: <b>{resetOtp}</b>");

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) throw new Exception("Yêu cầu không hợp lệ!");

            if (account.ResetPasswordToken != request.OtpCode) throw new Exception("Mã OTP không chính xác!");
            if (account.ResetPasswordTokenExpiresAt < DateTime.UtcNow) throw new Exception("Mã OTP đã hết hạn!");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.ResetPasswordToken = null;
            account.ResetPasswordTokenExpiresAt = null;

            await accountRepository.UpdateAsync(account);
            return true;
        }

        public async Task<UserProfileResponse> GetProfileAsync(Guid accountId)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            return new UserProfileResponse
            {
                AccountId = account.AccountId,
                Email = account.Email,
                FullName = account.FullName,
                PhoneNumber = account.PhoneNumber,
                RoleName = account.Role?.RoleName ?? "N/A",
                Status = account.Status,
                CreatedAt = (DateTime)account.CreatedAt
            };
        }

        public async Task<UserProfileResponse> UpdateProfileAsync(Guid accountId, UpdateProfileRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            account.FullName = request.FullName;
            account.PhoneNumber = request.PhoneNumber;
            account.UpdatedAt = DateTime.UtcNow;

            await accountRepository.UpdateAsync(account);

            return await GetProfileAsync(accountId);
        }

        public async Task<bool> ChangePassewordAsync(Guid accountId, ChangePasswordRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash);
            if (!isOldPasswordValid) throw new Exception("Mật khẩu cũ không chính xác!");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await accountRepository.UpdateAsync(account);
            return true;
        }

        public async Task<AuthResponse> RegisterTAAsync(TARegisterDto request)
        {
            var existingAccount = await accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null) throw new Exception("Email đã được sử dụng!");
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var role = await accountRepository.GetRoleByNameAsync("TA");
            var newAccount = new Account
            {
                AccountId = Guid.NewGuid(),
                FullName = request.FullName,
                RoleId = role.RoleId,
                Email = request.Email,
                PasswordHash = hashedPassword,
                PhoneNumber = request.PhoneNumber,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                TeachingAssistant = new TeachingAssistant
                {
                    Bio = request.Bio,
                }
            };
            var saveAccount = await accountRepository.AddAsync(newAccount);
            return new AuthResponse
            {
                AccountId = saveAccount.AccountId,
                Email = saveAccount.Email,
                FullName = saveAccount.FullName,
            };
        }
    }
}
