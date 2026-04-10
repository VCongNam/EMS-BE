using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository accountRepository;
        private readonly ICurrentUserService currentUserService;
        private readonly ISupabaseStorageService storageService;

        private const long MaxImageSize = 5 * 1024 * 1024;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        public AccountService(IAccountRepository accountRepository, ICurrentUserService currentUserService, ISupabaseStorageService storageService)
        {
            this.accountRepository = accountRepository;
            this.currentUserService = currentUserService;
            this.storageService = storageService;
        }

        public async Task<UserProfileResponse> GetProfileAsync(Guid accountId)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            var currentStudentId = currentUserService.StudentId;

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
                    // Trỏ vào Students (số nhiều) và lấy phần tử đầu tiên
                    var studentInfo = account.Students?.FirstOrDefault(s => s.StudentId == currentStudentId);

                    response.RoleSpecificData = new
                    {
                        StudentId = studentInfo?.StudentId, // Trả về luôn cho FE dễ dùng
                        Address = studentInfo?.Address,
                        Dob = studentInfo?.Dob
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
            var currentStudentId = currentUserService.StudentId;

            // SỬA: Tìm đúng đứa con cần update
            var studentProfile = account.Students.FirstOrDefault(s => s.StudentId == currentStudentId);
            if (studentProfile != null)
            {
                studentProfile.Address = request.Address;
                studentProfile.Dob = request.Dob;
                studentProfile.FullName = request.FullName;

                // Đồng bộ ngược lại tên Account nếu cần (tùy logic của bạn)
                account.FullName = request.FullName;
            }

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

        public async Task<string> UpdateAvatarAsync(IFormFile file)
        {
            var accountId = currentUserService.UserId;
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            // 1. Validate định dạng và dung lượng ảnh
            ValidateImage(file);

            // 2. Xóa ảnh cũ trên Supabase để tiết kiệm dung lượng
            if (!string.IsNullOrEmpty(account.AvatarUrl))
            {
                // Try-catch để nếu file cũ không tồn tại trên kho thì vẫn cho upload cái mới
                try { await storageService.DeleteFileByUrlAsync(account.AvatarUrl); } catch { }
            }

            // 3. Upload ảnh mới (Đặt vào folder avatars/{accountId})
            var newImageUrl = await storageService.UploadFileAsync(file, $"avatars/{accountId}");

            // 4. Lưu link vào Database
            account.AvatarUrl = newImageUrl;
            account.UpdatedAt = DateTime.UtcNow;
            await accountRepository.UpdateAsync(account);

            return newImageUrl;
        }

        private void ValidateImage(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024) throw new Exception("Ảnh không được quá 5MB.");

            var ext = Path.GetExtension(file.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(ext)) throw new Exception("Định dạng ảnh không hợp lệ.");
        }


    }
}