using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.SystemAdmin.Dtos;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Services
{
    public class SystemAdminService : ISystemAdminService
    {
        private readonly ISystemAdminRepository adminRepository;
        private readonly IEmailService emailService;

        public SystemAdminService(ISystemAdminRepository adminRepository, IEmailService emailService)
        {
            this.adminRepository = adminRepository;
            this.emailService = emailService;
        }

        public async Task<AdminDashboardDto> GetSystemDashboardAsync()
        {
            var teachers = await adminRepository.CountAccountsByRoleAsync("Teacher");
            var students = await adminRepository.CountAccountsByRoleAsync("Student");
            var tas = await adminRepository.CountAccountsByRoleAsync("TA");

            return new AdminDashboardDto
            {
                TotalUsers = teachers + students + tas,
                TotalTeachers = teachers,
                TotalStudents = students,
                TotalTAs = tas,
                TotalActiveClasses = await adminRepository.CountActiveClassesAsync(),
                NewRegistrationsThisMonth = await adminRepository.CountNewRegistrationsThisMonthAsync()
            };
        }

        public async Task<IEnumerable<AccountSummaryDto>> GetAllAccountsAsync(string? role, string? status)
        {
            var accounts = await adminRepository.GetAllAccountsAsync(role, status);

            return accounts.Select(a => new AccountSummaryDto
            {
                AccountId = a.AccountId,
                Email = a.Email ?? null!,
                FullName = a.FullName ?? null!,
                RoleName = a.Role?.RoleName ?? "Unknown",
                Status = a.Status ?? "Active",
                CreatedAt = a.CreatedAt
            });
        }

        public async Task<AccountDetailDto> GetAccountDetailAsync(Guid accountId)
        {
            var account = await adminRepository.GetAccountByIdAsync(accountId);
            if (account == null) throw new Exception("Account not found.");

            var detail = new AccountDetailDto
            {
                AccountId = account.AccountId,
                Email = account.Email ?? null!,
                FullName = account.FullName ?? null!,
                PhoneNumber = account.PhoneNumber,
                AvatarUrl = account.AvatarUrl,
                RoleName = account.Role?.RoleName ?? "Unknown",
                Status = account.Status ?? "Active",
                CreatedAt = account.CreatedAt
            };

            if (detail.RoleName == "Teacher")
            {
                detail.TotalOwnedClasses = await adminRepository.CountClassesByTeacherAsync(accountId);
            }

            return detail;
        }

        public async Task ChangeAccountStatusAsync(Guid accountId, ChangeAccountStatusDto request)
        {
            var account = await adminRepository.GetAccountByIdAsync(accountId);
            if (account == null) throw new Exception("Account not found.");

            if (account.Role?.RoleName == "Admin")
                throw new Exception("Cannot change status of another Admin account.");

            var validStatuses = new[] { "Active", "Banned", "Unverified"};
            if (!validStatuses.Contains(request.NewStatus))
                throw new Exception("Invalid status value.");

            account.Status = request.NewStatus;
            account.UpdatedAt = DateTime.UtcNow;

            await adminRepository.UpdateAccountAsync(account);
            // Kiểm tra xem email dưới DB có trống không
            if (string.IsNullOrEmpty(account.Email))
            {
                throw new Exception($"Tài khoản {account.FullName} không có địa chỉ Email trong hệ thống để gửi!");
            }

            // 1. Chuẩn bị nội dung
            var subject = $"[EMS Platform] Thông báo trạng thái tài khoản";
            var body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <h3>Chào {account.FullName},</h3>
                    <p>Tài khoản của bạn trên hệ thống EMS đã được chuyển sang trạng thái: <strong style='color: #d9534f;'>{request.NewStatus}</strong>.</p>
                    <p><strong>Thông điệp từ Ban quản trị:</strong> {request.Reason}</p>
                    <p>Nếu cần hỗ trợ thêm, vui lòng phản hồi lại email này.</p>
                    <br/>
                    <p>Trân trọng,<br/><strong>Đội ngũ EMS Admin</strong></p>
                </div>";

            try
            {
                if (!string.IsNullOrEmpty(account.Email))
                {
                    var emailMessage = new EmailMessage
                    {
                        To = account.Email,    
                        Subject = subject,
                        Body = body
                    };

                    await emailService.SendEmailAsync(emailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== LỖI GỬI EMAIL ==========");
                Console.WriteLine($"Tài khoản nhận: {account.Email}");
                Console.WriteLine($"Thông báo lỗi: {ex.Message}");
                Console.WriteLine("=====================================");
            }
        }

        public async Task<IEnumerable<SystemLogDto>> GetSuspiciousActivitiesAsync(int limit = 50)
        {
            var logs = await adminRepository.GetRecentSystemLogsAsync(limit);

            return logs.Select(log => new SystemLogDto
            {
                LogId = log.LogId,
                AccountId = log.AccountId,
                Email = log.Account?.Email ?? "Hệ thống",
                FullName = log.Account?.FullName ?? "Unknown",
                RoleName = log.Account?.Role?.RoleName ?? "N/A",
                ActionType = log.ActionType ?? "UNKNOWN",
                TableName = log.TableName ?? "Unknown",
                IpAddress = log.Ipaddress,
                OldValues = log.OldValues?.ToString(),
                NewValues = log.NewValues?.ToString(),
                CreatedAt = log.CreatedAt
            });
        }
    }
}
