using EMS.Application.Common.Interfaces;  
using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
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

        public AccountService(IAccountRepository accountRepository, IJwtTokenGenerator jwtTokenGenerator)
        {
            this.accountRepository = accountRepository;
            this.jwtTokenGenerator = jwtTokenGenerator;
        }

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

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingAccount = await accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null) throw new Exception("Email đã được sử dụng!");

            // 2. Tự động lấy RoleID của "Teacher" từ Database
            var teacherRole = await accountRepository.GetRoleByNameAsync("Teacher");
            if (teacherRole == null) throw new Exception("Lỗi hệ thống: Không tìm thấy Role 'Teacher' trong CSDL!");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newAccount = new Account
            {
                AccountId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                RoleId = teacherRole.RoleId, // Tự động gán quyền Teacher
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            var saveAccount = await accountRepository.AddAsync(newAccount);
            return new AuthResponse
            {
                AccountId = saveAccount.AccountId,
                Email = saveAccount.Email,
                FullName = saveAccount.FullName,
                Token = ""
            };
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

            await accountRepository.UpdateAsync(account);

            return await GetProfileAsync(accountId);
        }


        public async Task<bool> ResetPasswordAsync(Guid accountId, ResetPasswordRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash);
            if (!isPasswordValid) throw new Exception("Mật khẩu cũ không chính xác!");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await accountRepository.UpdateAsync(account);

            return true;
        }

    }
}
