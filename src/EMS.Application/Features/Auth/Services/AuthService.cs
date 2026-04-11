using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Auth.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IJwtTokenGenerator jwtTokenGenerator;
        private readonly IOtpService otpService;
        private readonly IEmailService emailService;
        private readonly ICurrentUserService currentUserService;

        public AuthService(IAccountRepository accountRepository, IJwtTokenGenerator jwtTokenGenerator, IOtpService otpService, IEmailService emailService, ICurrentUserService currentUserService)
        {
            this.accountRepository = accountRepository;
            this.jwtTokenGenerator = jwtTokenGenerator;
            this.otpService = otpService;
            this.emailService = emailService;
            this.currentUserService = currentUserService;
        }

        // Đăng ký
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingAccount = await accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null) throw new Exception("Email đã được sử dụng!");

            var allowedRoles = new List<string> { "Teacher", "TA" };
            string requestedRole = request.RoleName;

            if (!allowedRoles.Contains(requestedRole))
            {
                throw new Exception("Quyền đăng ký không hợp lệ. Chỉ được chọn Giáo viên hoặc Trợ giảng.");
            }

            var roleEntity = await accountRepository.GetRoleByNameAsync(requestedRole);
            if (roleEntity == null)
            {
                throw new Exception($"Lỗi hệ thống: Role '{requestedRole}' chưa được cấu hình trong DB.");
            }

            string plainOtp = otpService.GenerateOtp();

            // ĐÃ SỬA: Gọi SendEmailAsync thay vì QueueEmailAsync
            await emailService.SendEmailAsync(new EmailMessage
            {
                To = request.Email,
                Subject = "EMS - Xác thực tài khoản",
                Body = $"Chào {request.FullName}, mã OTP đăng ký của bạn là: <b>{plainOtp}</b>. Hiệu lực 15 phút."
            });

            string hashedOtp = otpService.HashOtp(plainOtp);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newAccount = new Account();

            if (requestedRole == "Teacher")
            {
                newAccount = new Account
                {
                    AccountId = Guid.NewGuid(),
                    Email = request.Email,
                    PasswordHash = hashedPassword,
                    FullName = request.FullName,
                    RoleId = roleEntity.RoleId,
                    Status = "Unverified",
                    VerificationToken = hashedOtp,
                    VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    CreatedAt = DateTime.UtcNow,
                 
                    Teacher = new Teacher
                    {
                        Bio = null,
                        BankAccount = null,
                        BankAccountName = null,
                        BankName = null,
                        Specialization = null
                    }
                };
            }
            else if (requestedRole == "TA")
            {
                newAccount = new Account
                {
                    AccountId = Guid.NewGuid(),
                    Email = request.Email,
                    PasswordHash = hashedPassword,
                    FullName = request.FullName,
                    RoleId = roleEntity.RoleId,
                    Status = "Unverified",
                    VerificationToken = hashedOtp,
                    VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    CreatedAt = DateTime.UtcNow,
                    TeachingAssistant = new TeachingAssistant
                    {
                        Bio = null,
                        BankAccount = null,
                        BankAccountName = null,
                        BankName = null
                    }
                };
            }

            var saveAccount = await accountRepository.AddAsync(newAccount);

            return new AuthResponse
            {
                AccountId = saveAccount.AccountId,
                FullName = saveAccount.FullName,
                RoleName = saveAccount.Role.RoleName
            };
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) throw new Exception("Tài khoản không tồn tại!");
            if (account.Status == "Active") throw new Exception("Tài khoản đã được xác thực!");

            // Sửa lỗi cảnh báo vàng CS8604 bằng cách kiểm tra null (VerificationToken ?? "")
            if (!otpService.VerifyOtp(request.OtpCode, account.VerificationToken ?? ""))
                throw new Exception("Mã OTP không chính xác!");

            if (account.VerificationTokenExpiresAt < DateTime.UtcNow)
                throw new Exception("Mã OTP đã hết hạn!");

            account.Status = "Active";
            account.VerificationToken = null;
            account.VerificationTokenExpiresAt = null;

            await accountRepository.UpdateAsync(account);
            return true;
        }

        // Đăng nhập
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            Account? account;

            // 1. Tìm tài khoản theo Identifier
            if (request.SelectedRole == "Student")
                account = await accountRepository.GetByPhoneAsync(request.Identifier);
            else
                account = await accountRepository.GetByEmailAsync(request.Identifier);

            if (account == null || account.Role.RoleName != request.SelectedRole)
                throw new Exception("Thông tin đăng nhập không chính xác!");

            // 2. Check mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
                throw new Exception("Mật khẩu không chính xác!");

            // ============================================================
            // 3. CHẶN CỨNG: CHƯA ACTIVE THÌ KHÔNG CẤP TOKEN (DÙNG CHO TẤT CẢ)
            // ============================================================
            if (account.Status == "Unverified")
            {
                // Quăng lỗi để Frontend bắt và chuyển hướng sang trang xác thực OTP
                throw new Exception("Tài khoản của bạn chưa được kích hoạt. Vui lòng xác thực mã OTP!");
            }

            if (account.Status == "Banned")
                throw new Exception("Tài khoản đã bị khóa!");

            // LUỒNG CHO HỌC SINH (Student)
            if (request.SelectedRole == "Student")
            {
                return new AuthResponse
                {
                    AccountId = account.AccountId,
                    FullName = account.FullName,
                    RoleName = "Student",
                    RequiresProfileSelection = true,
                    TempToken = jwtTokenGenerator.GenerateToken(account, "Student", isTempToken: true),
                    AvailableProfiles = account.Students.Select(s => new StudentProfileDto
                    {
                        StudentId = s.StudentId,
                        FullName = s.FullName ?? "Học sinh"
                    }).ToList()
                };
            }

            // LUỒNG CHO ADMIN, TEACHER, TA (Vào thẳng)
            var mainToken = jwtTokenGenerator.GenerateToken(account, account.Role.RoleName);
            return new AuthResponse
            {
                AccountId = account.AccountId,
                FullName = account.FullName,
                RoleName = account.Role.RoleName,
                Token = mainToken,
                Status = account.Status,
                RequiresProfileSelection = false
            };
        }

        // Quên mật khẩu 
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) return true;

            string plainOtp = otpService.GenerateOtp();

            // ĐÃ SỬA: Gọi SendEmailAsync thay vì QueueEmailAsync
            await emailService.SendEmailAsync(new EmailMessage
            {
                To = request.Email,
                Subject = "EMS - Khôi phục mật khẩu",
                Body = $"Mã OTP là: <b>{plainOtp}</b>"
            });

            account.ResetPasswordToken = otpService.HashOtp(plainOtp);
            account.ResetPasswordTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

            await accountRepository.UpdateAsync(account);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) throw new Exception("Yêu cầu không hợp lệ!");

            if (!otpService.VerifyOtp(request.OtpCode, account.ResetPasswordToken))
                throw new Exception("Mã OTP không chính xác!");

            if (account.ResetPasswordTokenExpiresAt < DateTime.UtcNow)
                throw new Exception("Mã OTP đã hết hạn!");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.ResetPasswordToken = null;
            account.ResetPasswordTokenExpiresAt = null;

            await accountRepository.UpdateAsync(account);
            return true;
        }

        // 2. Hàm Chọn Profile (Tự lấy AccountId từ CurrentUserService)
        public async Task<AuthResponse> SelectProfileAsync(Guid studentId)
        {
            var accountId = currentUserService.UserId;

            var account = await accountRepository.GetByIdAsync(accountId);
            var student = account?.Students.FirstOrDefault(s => s.StudentId == studentId);

            if (student == null) throw new Exception("Profile học sinh không hợp lệ!");

            return new AuthResponse
            {
                AccountId = account!.AccountId,
                FullName = student.FullName ?? account.FullName,
                RoleName = "Student",
                Token = jwtTokenGenerator.GenerateToken(account, "Student", false, studentId),
                Status = account.Status
            };
        }
        public async Task<bool> VerifyOnboardingAsync(OnboardingRequest request)
        {

            var account = await accountRepository.GetByPhoneAsync(request.PhoneNumber);
            if (account == null)
                throw new Exception("Số điện thoại này chưa được đăng ký trong hệ thống.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash))
                throw new Exception("Mật khẩu hiện tại không chính xác!");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.Status = "Active";

            await accountRepository.UpdateAsync(account);
            return true;
        }
        public async Task<bool> ResendOtpAsync(ResendOtpRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);

            if (account == null)
                throw new Exception("Email này chưa được đăng ký trong hệ thống.");

            string plainOtp = otpService.GenerateOtp();

            await emailService.SendEmailAsync(new EmailMessage
            {
                To = request.Email,
                Subject = "EMS - Gửi lại mã xác thực OTP",
                Body = $"Chào {account.FullName}, mã OTP mới của bạn là: <b>{plainOtp}</b>. Hiệu lực 15 phút."
            });

            // 5. Mã hóa mã OTP và thiết lập thời gian hết hạn mới (Y hệt hàm Register)
            string hashedOtp = otpService.HashOtp(plainOtp);

            account.VerificationToken = hashedOtp;
            account.VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

            // 6. Cập nhật vào Database
            await accountRepository.UpdateAsync(account);

            return true;
        }

    }
}
