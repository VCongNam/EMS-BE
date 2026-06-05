 using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Auth.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly ICaptchaService captchaService;
        private readonly IMemoryCache memoryCache;

        public AuthService(IAccountRepository accountRepository, IJwtTokenGenerator jwtTokenGenerator, IOtpService otpService, IEmailService emailService, 
            ICurrentUserService currentUserService, ICaptchaService captchaService, IMemoryCache memoryCache)
        {
            this.accountRepository = accountRepository;
            this.jwtTokenGenerator = jwtTokenGenerator;
            this.otpService = otpService;
            this.emailService = emailService;
            this.currentUserService = currentUserService;
            this.captchaService = captchaService;
            this.memoryCache = memoryCache;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            bool isCaptchaValid = await captchaService.VerifyCaptchaAsync(request.CaptchaToken);
            if (!isCaptchaValid)
            {
                throw new BadRequestException("Captcha không hợp lệ. Vui lòng thử lại.");
            }

            var allowedRoles = new List<string> { "Teacher", "TA" };
            string requestedRole = request.RoleName;

            if (!allowedRoles.Contains(requestedRole))
            {
                throw new NotFoundException("Quyền đăng ký không hợp lệ. Chỉ được chọn Giáo viên hoặc Trợ giảng.");
            }

            var roleEntity = await accountRepository.GetRoleByNameAsync(requestedRole);
            if (roleEntity == null)
            {
                throw new NotFoundException($"Lỗi hệ thống: Role '{requestedRole}' chưa được cấu hình trong DB.");
            }

            string plainOtp = otpService.GenerateOtp();
            string hashedOtp = otpService.HashOtp(plainOtp);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var existingAccount = await accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null)
            {
                if (existingAccount.Status == "Active")
                {
                    throw new ConflictException("Email này đã được sử dụng và xác thực!");
                }

                if (existingAccount.Status == "Unverified")
                {
                    existingAccount.PasswordHash = hashedPassword;
                    existingAccount.FullName = request.FullName;
                    existingAccount.VerificationToken = hashedOtp;
                    existingAccount.VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
                    existingAccount.RoleId = roleEntity.RoleId; 

                    await accountRepository.UpdateAsync(existingAccount);

                    await emailService.SendEmailAsync(new EmailMessage
                    {
                        To = request.Email,
                        Subject = "EMS - Xác thực tài khoản (Gửi lại)",
                        Body = $"Chào {request.FullName}, mã OTP đăng ký mới của bạn là: <b>{plainOtp}</b>. Hiệu lực 15 phút."
                    });

                    return new AuthResponse
                    {
                        AccountId = existingAccount.AccountId,
                        FullName = existingAccount.FullName,
                        RoleName = existingAccount.Role.RoleName
                    };
                }
            }

            var newAccount = new Account
            {
                AccountId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                RoleId = roleEntity.RoleId,
                Status = "Unverified",
                VerificationToken = hashedOtp,
                VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };

            if (requestedRole == "Teacher")
            {
                newAccount.Teacher = new Teacher 
                { 
                    Bio = null, 
                    BankAccount = null, 
                    BankAccountName = null, 
                    BankName = null, 
                    Specialization = null 
                };
            }
            else if (requestedRole == "TA")
            {
                newAccount.TeachingAssistant = new TeachingAssistant 
                { 
                    Bio = null, 
                    BankAccount = null, 
                    BankAccountName = null, 
                    BankName = null 
                };
            }

            var saveAccount = await accountRepository.AddAsync(newAccount);

            await emailService.SendEmailAsync(new EmailMessage
            {
                To = request.Email,
                Subject = "EMS - Xác thực tài khoản",
                Body = $"Chào {request.FullName}, mã OTP đăng ký của bạn là: <b>{plainOtp}</b>. Hiệu lực 15 phút."
            });

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
            if (account == null) throw new NotFoundException("Tài khoản không tồn tại!");
            if (account.Status == "Active") throw new BadRequestException("Tài khoản đã được xác thực!");

            if (!otpService.VerifyOtp(request.OtpCode, account.VerificationToken ?? ""))
                throw new BadRequestException("Mã OTP không chính xác!");

            if (account.VerificationTokenExpiresAt < DateTime.UtcNow)
                throw new BadRequestException("Mã OTP đã hết hạn!");

            account.Status = "Active";
            account.VerificationToken = null;
            account.VerificationTokenExpiresAt = null;

            await accountRepository.UpdateAsync(account);
            return true;
        }

        public async Task<bool> VerifyOnboardingAsync(OnboardingRequest request)
        {

            var account = await accountRepository.GetByPhoneAsync(request.PhoneNumber);
            if (account == null)
                throw new  NotFoundException("Số điện thoại này chưa được đăng ký trong hệ thống.");
            if (account.Status == "Active")
                throw new BadRequestException("Tài khoản đã được xác thực!");

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new BadRequestException("Mật khẩu mới và xác nhận mật khẩu không khớp!");
            }

            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, account.PasswordHash);
            if (!isOldPasswordValid) throw new BadRequestException("Mật khẩu cũ không chính xác!");


            account.Status = "Active";
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await accountRepository.UpdateAsync(account);
            return true;
        }




        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            Account? account;

            if (request.SelectedRole == "Student")
                account = await accountRepository.GetByPhoneAsync(request.Identifier);
            else
                account = await accountRepository.GetByEmailAsync(request.Identifier);

            if (account == null || account.Role.RoleName != request.SelectedRole)
                throw new BadRequestException("Thông tin đăng nhập không chính xác!");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
                throw new BadRequestException("Mật khẩu không chính xác!");

            if (account.Status == "Unverified")
            {
                throw new BadRequestException("Tài khoản của bạn chưa được kích hoạt");
            }


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
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null) throw new NotFoundException("Email này chưa được đăng ký");

            string plainOtp = otpService.GenerateOtp();

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
            if (account == null) throw new NotFoundException("Email này không tồn tại trong hệ thống !");

            if (!otpService.VerifyOtp(request.OtpCode, account.ResetPasswordToken))
                throw new BadRequestException("Mã OTP không chính xác!");

            if (account.ResetPasswordTokenExpiresAt < DateTime.UtcNow)
                throw new BadRequestException("Mã OTP đã hết hạn!");

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            account.ResetPasswordToken = null;
            account.ResetPasswordTokenExpiresAt = null;

            await accountRepository.UpdateAsync(account);
            return true;
        }

        public async Task<AuthResponse> SelectProfileAsync(Guid studentId)
        {
            var accountId = currentUserService.UserId;

            var account = await accountRepository.GetByIdAsync(accountId);
            var student = account?.Students.FirstOrDefault(s => s.StudentId == studentId);

            if (student == null) throw new BadRequestException("Profile học sinh không hợp lệ!");

            return new AuthResponse
            {
                AccountId = account!.AccountId,
                FullName = student.FullName ?? account.FullName,
                RoleName = "Student",
                Token = jwtTokenGenerator.GenerateToken(account, "Student", false, studentId),
                Status = account.Status
            };
        }

        public async Task<bool> ResendOtpAsync(ResendOtpRequest request)
        {
            var account = await accountRepository.GetByEmailAsync(request.Email);
            if (account == null)
                throw new  NotFoundException("Email này chưa được đăng ký trong hệ thống.");
            if (account.Status == "Active")
                throw new BadRequestException("Tài khoản đã được xác thực!");

            string cacheKey = $"ResendOTP_{request.Email}";

            if (memoryCache.TryGetValue(cacheKey, out int resendCount))
            {
                if (resendCount >= 3)
                {
                    throw new BadRequestException("Bạn đã vượt quá giới hạn 3 lần gửi lại mã OTP trong hôm nay. Vui lòng thử lại vào ngày mai hoặc liên hệ Admin!");
                }
                memoryCache.Set(cacheKey, resendCount + 1, TimeSpan.FromHours(24));
            }
            else
            {
                // Lần đầu bấm gửi lại -> Set count = 1, tồn tại trong 24h
                memoryCache.Set(cacheKey, 1, TimeSpan.FromHours(24));
            }

            // Tiến hành tạo OTP mới và gửi mail
            string plainOtp = otpService.GenerateOtp();
            string hashedOtp = otpService.HashOtp(plainOtp);

            account.VerificationToken = hashedOtp;
            account.VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

            await accountRepository.UpdateAsync(account);

            await emailService.SendEmailAsync(new EmailMessage
            {
                To = request.Email,
                Subject = "EMS - Gửi lại mã xác thực OTP",
                Body = $"Chào {account.FullName}, mã OTP mới của bạn là: <b>{plainOtp}</b>. Hiệu lực 15 phút."
            });

            return true;
        }

    }
}
