using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            this.accountRepository = accountRepository;
        }

        public async Task<UserProfileResponse> GetProfileAsync(Guid accountId)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            var response = new UserProfileResponse
            {
                AccountId = account.AccountId,
                Email = account.Email,
                FullName = account.FullName,
                PhoneNumber = account.PhoneNumber,
                RoleName = account.Role?.RoleName ?? "N/A",
                Status = account.Status,
                CreatedAt = (DateTime)account.CreatedAt!,
                AvatarUrl = account.AvatarUrl
            };

            // Gắn thông tin riêng tùy theo Role
            switch (response.RoleName)
            {
                case "Teacher":
                    response.RoleSpecificData = new
                    {
                        Bio = account.Teacher?.Bio,
                        Specialization = account.Teacher?.Specialization,
                        BankName = account.Teacher?.BankName,
                        BankAccount = account.Teacher?.BankAccount,
                        BankAccountName = account.Teacher?.BankAccountName
                    };
                    break;
                case "TA":
                    response.RoleSpecificData = new
                    {
                        Bio = account.TeachingAssistant?.Bio,
                        BankName = account.TeachingAssistant?.BankName,
                        BankAccount = account.TeachingAssistant?.BankAccount,
                        BankAccountName = account.TeachingAssistant?.BankAccountName
                    };
                    break;
                case "Student":
                    response.RoleSpecificData = new
                    {
                        //ParentName = account.Student?.ParentName,
                        //ParentPhone = account.Student?.ParentPhone,
                        //ParentEmail = account.Student?.ParentEmail,
                        //Address = account.Student?.Address,
                        //Dob = account.Student?.Dob
                    };
                    break;
            }

            return response;
        }

        public async Task<UserProfileResponse> UpdateTeacherProfileAsync(Guid accountId, UpdateTeacherProfileRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null || account.Role.RoleName != "Teacher")
                throw new Exception("Tài khoản không hợp lệ hoặc không phải Giáo viên!");

            // 1. Cập nhật thông tin chung ở bảng Account
            account.FullName = request.FullName;
            account.PhoneNumber = request.PhoneNumber;
            account.UpdatedAt = DateTime.UtcNow;

            // 2. Cập nhật thông tin riêng ở bảng Teacher
            if (account.Teacher != null)
            {
                account.Teacher.Bio = request.Bio;
                account.Teacher.Specialization = request.Specialization;
                account.Teacher.BankName = request.BankName;
                account.Teacher.BankAccount = request.BankAccount;
                account.Teacher.BankAccountName = request.BankAccountName;
            }

            await accountRepository.UpdateAsync(account);
            return await GetProfileAsync(accountId);
        }

        public async Task<UserProfileResponse> UpdateTAProfileAsync(Guid accountId, UpdateTAProfileRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null || account.Role.RoleName != "TA")
                throw new Exception("Tài khoản không hợp lệ hoặc không phải Trợ giảng!");

            account.FullName = request.FullName;
            account.PhoneNumber = request.PhoneNumber;
            account.UpdatedAt = DateTime.UtcNow;

            if (account.TeachingAssistant != null)
            {
                account.TeachingAssistant.Bio = request.Bio;
                account.TeachingAssistant.BankName = request.BankName;
                account.TeachingAssistant.BankAccount = request.BankAccount;
                account.TeachingAssistant.BankAccountName = request.BankAccountName;
            }

            await accountRepository.UpdateAsync(account);
            return await GetProfileAsync(accountId);
        }

        // 3. UPDATE CHO HỌC SINH (STUDENT)
        public async Task<UserProfileResponse> UpdateStudentProfileAsync(Guid accountId, UpdateStudentProfileRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null || account.Role.RoleName != "Student")
                throw new Exception("Tài khoản không hợp lệ hoặc không phải Học sinh!");

            account.FullName = request.FullName;
            account.PhoneNumber = request.PhoneNumber;
            account.UpdatedAt = DateTime.UtcNow;

            //if (account.Student != null)
            //{
            //    account.Student.ParentName = request.ParentName;
            //    account.Student.ParentPhone = request.ParentPhone;
            //    account.Student.ParentEmail = request.ParentEmail;
            //    account.Student.Address = request.Address;
            //    account.Student.Dob = request.Dob;
            //}

            await accountRepository.UpdateAsync(account);
            return await GetProfileAsync(accountId);
        }

        public async Task<bool> ChangePasswordAsync(Guid accountId, ChangePasswordRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash);
            if (!isOldPasswordValid) throw new Exception("Mật khẩu cũ không chính xác!");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await accountRepository.UpdateAsync(account);
            return true;
        }


        public async Task<(string NewUrl, string? OldUrl)> UpdateAvatarUrlAsync(Guid accountId, string avatarUrl)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            // 1. Giữ lại link cũ để tí nữa còn xóa trên Cloud
            string? oldUrl = account.AvatarUrl;

            // 2. Cập nhật link mới
            account.AvatarUrl = avatarUrl;
            account.UpdatedAt = DateTime.UtcNow;

            await accountRepository.UpdateAsync(account);

            return (avatarUrl, oldUrl);
        }

    }
}