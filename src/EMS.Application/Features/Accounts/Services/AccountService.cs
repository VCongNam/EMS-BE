using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AccountService> logger;

        private const long MaxImageSize = 5 * 1024 * 1024;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        public AccountService(IAccountRepository accountRepository, ICurrentUserService currentUserService, ISupabaseStorageService storageService, ILogger<AccountService> logger )
        {
            this.accountRepository = accountRepository;
            this.currentUserService = currentUserService;
            this.storageService = storageService;
            this.logger = logger;
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
                    if (studentInfo == null) throw new Exception("Hồ sơ học sinh không tồn tại");
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

        public async Task<UserProfileResponse> UpdateStudentProfileAsync(Guid accountId, Guid studentId, UpdateStudentProfileRequest request)
        {
            var account = await accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");

            var studentProfile = account.Students.FirstOrDefault(s => s.StudentId == studentId);

            if (studentProfile == null)
                throw new Exception("Hồ sơ học sinh không tồn tại hoặc bạn không có quyền chỉnh sửa hồ sơ này!");

            if (string.IsNullOrWhiteSpace(request.StudentFullName))
                throw new Exception("Tên học sinh không được để trống");

            studentProfile.FullName = request.StudentFullName.Trim();
            studentProfile.Address = request.Address;
            studentProfile.Dob = request.Dob;

            if (!string.IsNullOrWhiteSpace(request.ParentFullName))
            {
                account.FullName = request.ParentFullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                account.PhoneNumber = request.PhoneNumber.Trim();
            }

            account.UpdatedAt = DateTime.UtcNow;

            await accountRepository.UpdateAsync(account);
            return await GetProfileAsync(accountId); // Có thể bạn cũng cần update lại GetProfileAsync để trả về đúng profile vừa sửa
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

        public async Task<string> UpdateAvatarAsync(UploadAvatarDto request)
        {
            var userId = currentUserService.UserId;
            var account = await accountRepository.GetByIdAsync(userId);

            if (account == null)
                throw new Exception("Không tìm thấy tài khoản người dùng.");

            if (!string.IsNullOrEmpty(account.AvatarUrl))
            {
                try
                {
                    await storageService.DeleteFileByUrlAsync(account.AvatarUrl);
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Không thể xóa avatar cũ của user {userId}: {ex.Message}");
                }
            }
            ValidateImage(request.AvatarFile.FileName, request.AvatarFile.Length, request.AvatarFile.ContentType);

            var avatarUrl = await storageService.UploadFileAsync(request.AvatarFile, $"avatars/{userId}");

            account.AvatarUrl = avatarUrl;
            account.UpdatedAt = DateTime.UtcNow;

            await accountRepository.UpdateAsync(account);

            return avatarUrl;
        }

        private void ValidateImage(string fileName, long fileSize, string contentType)
        {
            if (fileSize > MaxImageSize)
                throw new Exception($"Ảnh '{fileName}' vượt quá dung lượng cho phép (5MB).");

            if (!AllowedImageExtensions.Contains(contentType.ToLower()))
                throw new Exception($"Định dạng file '{contentType}' không được hỗ trợ. Chỉ nhận PNG, JPEG, JPG, WEBP.");
        }
    }
}