using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Auth.DTOs;
using EMS.Application.Features.Auth.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Tests.Features.Auth
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IAccountRepository> _mockAccountRepo;
        private Mock<IJwtTokenGenerator> _mockJwtGenerator;
        private Mock<IOtpService> _mockOtpService;
        private Mock<IEmailService> _mockEmailService;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private AuthService _service;

        [SetUp]
        public void Setup()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
            _mockOtpService = new Mock<IOtpService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new AuthService(
                _mockAccountRepo.Object,
                _mockJwtGenerator.Object,
                _mockOtpService.Object,
                _mockEmailService.Object,
                _mockCurrentUser.Object
            );
        }

        #region 1. RegisterAsync Tests

        [Test]
        public void RegisterAsync_EmailAlreadyExists_ThrowsConflictException()
        {
            var request = new RegisterRequest { Email = "test@test.com" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new Account());

            var ex = Assert.ThrowsAsync<ConflictException>(async () => await _service.RegisterAsync(request));
            Assert.That(ex.Message, Does.Contain("Email đã được sử dụng!"));
        }

        [Test]
        public void RegisterAsync_InvalidRole_ThrowsNotFoundException()
        {
            var request = new RegisterRequest { Email = "new@test.com", RoleName = "Student" }; // Không cho đăng ký tự do role Student
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.RegisterAsync(request));
            Assert.That(ex.Message, Does.Contain("Quyền đăng ký không hợp lệ"));
        }

        [Test]
        public void RegisterAsync_RoleNotInDB_ThrowsNotFoundException()
        {
            var request = new RegisterRequest { Email = "new@test.com", RoleName = "Teacher" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync(request.RoleName)).ReturnsAsync((Role)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.RegisterAsync(request));
            Assert.That(ex.Message, Does.Contain("chưa được cấu hình trong DB"));
        }

        [Test]
        public async Task RegisterAsync_ValidTeacherRequest_CreatesAccountAndSendsEmail()
        {
            var request = new RegisterRequest { Email = "new@test.com", FullName = "GV Toán", Password = "Pass123", RoleName = "Teacher" };
            var role = new Role { RoleId = Guid.NewGuid(), RoleName = "Teacher" };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync(request.RoleName)).ReturnsAsync(role);
            _mockOtpService.Setup(s => s.GenerateOtp()).Returns("123456");
            _mockOtpService.Setup(s => s.HashOtp("123456")).Returns("hashed_otp");

            // Giả lập lưu DB thành công
            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<Account>()))
                            .ReturnsAsync((Account acc) => { acc.Role = role; return acc; }); 

            var result = await _service.RegisterAsync(request);

            Assert.That(result.RoleName, Is.EqualTo("Teacher"));
            Assert.That(result.FullName, Is.EqualTo("GV Toán"));

            _mockEmailService.Verify(e => e.SendEmailAsync(It.Is<EmailMessage>(m => m.To == request.Email)), Times.Once);
            _mockAccountRepo.Verify(r => r.AddAsync(It.Is<Account>(a => a.Teacher != null && a.Status == "Unverified")), Times.Once);
        }

        [Test]
        public async Task RegisterAsync_ValidTARequest_CreatesAccountAndSendsEmail()
        {
            var request = new RegisterRequest { Email = "ta@test.com", FullName = "TA Tiếng Anh", Password = "Pass", RoleName = "TA" };
            var role = new Role { RoleId = Guid.NewGuid(), RoleName = "TA" };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync(request.RoleName)).ReturnsAsync(role);
            _mockOtpService.Setup(s => s.GenerateOtp()).Returns("654321");
            _mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<Account>())).ReturnsAsync((Account acc) => { acc.Role = role; return acc; });

            var result = await _service.RegisterAsync(request);

            Assert.That(result.RoleName, Is.EqualTo("TA"));
            _mockAccountRepo.Verify(r => r.AddAsync(It.Is<Account>(a => a.TeachingAssistant != null)), Times.Once);
        }

        #endregion

        #region 2. VerifyEmailAsync Tests

        [Test]
        public void VerifyEmailAsync_AccountNotFound_ThrowsNotFoundException()
        {
            var request = new VerifyEmailRequest { Email = "test@test.com" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.VerifyEmailAsync(request));
            Assert.That(ex.Message, Does.Contain("Tài khoản không tồn tại!"));
        }

        [Test]
        public void VerifyEmailAsync_AccountAlreadyActive_ThrowsBadRequestException()
        {
            var request = new VerifyEmailRequest { Email = "test@test.com" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new Account { Status = "Active" });

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.VerifyEmailAsync(request));
            Assert.That(ex.Message, Does.Contain("đã được xác thực!"));
        }

        [Test]
        public void VerifyEmailAsync_WrongOtp_ThrowsBadRequestException()
        {
            var request = new VerifyEmailRequest { Email = "test@test.com", OtpCode = "000000" };
            var acc = new Account { Status = "Unverified", VerificationToken = "hash" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.VerifyOtp(request.OtpCode, acc.VerificationToken)).Returns(false);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.VerifyEmailAsync(request));
            Assert.That(ex.Message, Does.Contain("Mã OTP không chính xác!"));
        }

        [Test]
        public void VerifyEmailAsync_ExpiredOtp_ThrowsBadRequestException()
        {
            var request = new VerifyEmailRequest { Email = "test@test.com", OtpCode = "123456" };
            var acc = new Account { Status = "Unverified", VerificationToken = "hash", VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(-5) }; // Đã hết hạn

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.VerifyOtp(request.OtpCode, acc.VerificationToken)).Returns(true);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.VerifyEmailAsync(request));
            Assert.That(ex.Message, Does.Contain("Mã OTP đã hết hạn!"));
        }

        [Test]
        public async Task VerifyEmailAsync_ValidRequest_UpdatesAccountToActive()
        {
            var request = new VerifyEmailRequest { Email = "test@test.com", OtpCode = "123456" };
            var acc = new Account { Status = "Unverified", VerificationToken = "hash", VerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(5) };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.VerifyOtp(request.OtpCode, acc.VerificationToken)).Returns(true);

            var result = await _service.VerifyEmailAsync(request);

            Assert.That(result, Is.True);
            Assert.That(acc.Status, Is.EqualTo("Active"));
            Assert.That(acc.VerificationToken, Is.Null);
            _mockAccountRepo.Verify(r => r.UpdateAsync(acc), Times.Once);
        }

        #endregion

        #region 3. VerifyOnboardingAsync Tests

        [Test]
        public void VerifyOnboardingAsync_AccountNotFound_ThrowsNotFoundException()
        {
            var request = new OnboardingRequest { PhoneNumber = "0987654321" };
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.VerifyOnboardingAsync(request));
            Assert.That(ex.Message, Does.Contain("chưa được đăng ký trong hệ thống"));
        }

        [Test]
        public void VerifyOnboardingAsync_AccountAlreadyActive_ThrowsBadRequestException()
        {
            var request = new OnboardingRequest { PhoneNumber = "0987654321" };
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync(new Account { Status = "Active" });

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.VerifyOnboardingAsync(request));
            Assert.That(ex.Message, Does.Contain("Tài khoản đã được xác thực"));
        }

        [Test]
        public void VerifyOnboardingAsync_PasswordMismatch_ThrowsBadRequestException()
        {
            var request = new OnboardingRequest { PhoneNumber = "0987654321", NewPassword = "Pass1", ConfirmPassword = "Pass2" };
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync(new Account { Status = "Unverified" });

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.VerifyOnboardingAsync(request));
            Assert.That(ex.Message, Does.Contain("Mật khẩu mới và xác nhận mật khẩu không khớp"));
        }

        [Test]
        public void VerifyOnboardingAsync_WrongOldPassword_ThrowsBadRequestException()
        {
            var request = new OnboardingRequest { PhoneNumber = "0987654321", OldPassword = "Wrong", NewPassword = "New", ConfirmPassword = "New" };
            var acc = new Account { Status = "Unverified", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct") };
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync(acc);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.VerifyOnboardingAsync(request));
            Assert.That(ex.Message, Does.Contain("Mật khẩu cũ không chính xác"));
        }

        [Test]
        public async Task VerifyOnboardingAsync_ValidRequest_UpdatesStatusAndPassword()
        {
            var request = new OnboardingRequest { PhoneNumber = "0987654321", OldPassword = "OldPass", NewPassword = "NewPass", ConfirmPassword = "NewPass" };
            var acc = new Account { Status = "Unverified", PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass") };

            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync(acc);

            var result = await _service.VerifyOnboardingAsync(request);

            Assert.That(result, Is.True);
            Assert.That(acc.Status, Is.EqualTo("Active"));
            Assert.That(BCrypt.Net.BCrypt.Verify("NewPass", acc.PasswordHash), Is.True);
            _mockAccountRepo.Verify(r => r.UpdateAsync(acc), Times.Once);
        }

        #endregion

        #region 4. LoginAsync Tests

        [Test]
        public void LoginAsync_InvalidIdentifierOrRole_ThrowsBadRequest()
        {
            var request = new LoginRequest { Identifier = "test@test.com", SelectedRole = "Teacher" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.LoginAsync(request));
            Assert.That(ex.Message, Does.Contain("Thông tin đăng nhập không chính xác"));
        }

        [Test]
        public void LoginAsync_WrongPassword_ThrowsBadRequest()
        {
            var request = new LoginRequest { Identifier = "test@test.com", Password = "Wrong", SelectedRole = "Teacher" };
            var acc = new Account { Role = new Role { RoleName = "Teacher" }, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct") };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync(acc);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.LoginAsync(request));
            Assert.That(ex.Message, Does.Contain("Mật khẩu không chính xác"));
        }

        [Test]
        public void LoginAsync_AccountUnverified_ThrowsBadRequest()
        {
            var request = new LoginRequest { Identifier = "test@test.com", Password = "Pass", SelectedRole = "Teacher" };
            var acc = new Account { Role = new Role { RoleName = "Teacher" }, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass"), Status = "Unverified" };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync(acc);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.LoginAsync(request));
            Assert.That(ex.Message, Does.Contain("chưa được kích hoạt"));
        }

        [Test]
        public async Task LoginAsync_ValidStudent_ReturnsTempTokenAndRequiresProfileSelection()
        {
            var request = new LoginRequest { Identifier = "0987654321", Password = "Pass", SelectedRole = "Student" };
            var acc = new Account
            {
                AccountId = Guid.NewGuid(),
                FullName = "Phụ huynh A",
                Status = "Active",
                Role = new Role { RoleName = "Student" },
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass"),
                Students = new List<Student> { new Student { StudentId = Guid.NewGuid(), FullName = "Học sinh A" } }
            };

            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.Identifier)).ReturnsAsync(acc);
            _mockJwtGenerator.Setup(j => j.GenerateToken(acc, "Student", true, null)).Returns("temp_token");

            var result = await _service.LoginAsync(request);

            Assert.That(result.RequiresProfileSelection, Is.True);
            Assert.That(result.TempToken, Is.EqualTo("temp_token"));
            Assert.That(result.AvailableProfiles.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task LoginAsync_ValidTeacher_ReturnsMainToken()
        {
            var request = new LoginRequest { Identifier = "gv@test.com", Password = "Pass", SelectedRole = "Teacher" };
            var acc = new Account
            {
                AccountId = Guid.NewGuid(),
                Status = "Active",
                Role = new Role { RoleName = "Teacher" },
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass")
            };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync(acc);
            _mockJwtGenerator.Setup(j => j.GenerateToken(acc, "Teacher", false, null)).Returns("main_token");

            var result = await _service.LoginAsync(request);

            Assert.That(result.RequiresProfileSelection, Is.False);
            Assert.That(result.Token, Is.EqualTo("main_token"));
        }

        #endregion

        #region 5. ForgotPasswordAsync Tests

        [Test]
        public void ForgotPasswordAsync_EmailNotFound_ThrowsNotFoundException()
        {
            var request = new ForgotPasswordRequest { Email = "notfound@test.com" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.ForgotPasswordAsync(request));
            Assert.That(ex.Message, Does.Contain("Email này chưa được đăng ký"));
        }

        [Test]
        public async Task ForgotPasswordAsync_ValidRequest_UpdatesTokenAndSendsEmail()
        {
            var request = new ForgotPasswordRequest { Email = "exist@test.com" };
            var acc = new Account { Email = request.Email };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.GenerateOtp()).Returns("555555");
            _mockOtpService.Setup(o => o.HashOtp("555555")).Returns("hashed_reset");

            var result = await _service.ForgotPasswordAsync(request);

            Assert.That(result, Is.True);
            Assert.That(acc.ResetPasswordToken, Is.EqualTo("hashed_reset"));
            _mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<EmailMessage>()), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(acc), Times.Once);
        }

        #endregion

        #region 6. ResetPasswordAsync Tests

        [Test]
        public void ResetPasswordAsync_EmailNotFound_ThrowsNotFoundException()
        {
            var request = new ResetPasswordRequest { Email = "notfound@test.com" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.ResetPasswordAsync(request));
            Assert.That(ex.Message, Does.Contain("không tồn tại trong hệ thống"));
        }

        [Test]
        public void ResetPasswordAsync_WrongOtp_ThrowsBadRequestException()
        {
            var request = new ResetPasswordRequest { Email = "test@test.com", OtpCode = "111" };
            var acc = new Account { ResetPasswordToken = "hash" };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.VerifyOtp(request.OtpCode, acc.ResetPasswordToken)).Returns(false);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.ResetPasswordAsync(request));
            Assert.That(ex.Message, Does.Contain("Mã OTP không chính xác"));
        }

        [Test]
        public void ResetPasswordAsync_ExpiredOtp_ThrowsBadRequestException()
        {
            var request = new ResetPasswordRequest { Email = "test@test.com", OtpCode = "111" };
            var acc = new Account { ResetPasswordToken = "hash", ResetPasswordTokenExpiresAt = DateTime.UtcNow.AddMinutes(-5) };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.VerifyOtp(request.OtpCode, acc.ResetPasswordToken)).Returns(true);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.ResetPasswordAsync(request));
            Assert.That(ex.Message, Does.Contain("Mã OTP đã hết hạn"));
        }

        [Test]
        public async Task ResetPasswordAsync_ValidRequest_UpdatesPasswordAndClearsToken()
        {
            var request = new ResetPasswordRequest { Email = "test@test.com", OtpCode = "111", NewPassword = "NewStrongPassword" };
            var acc = new Account { ResetPasswordToken = "hash", ResetPasswordTokenExpiresAt = DateTime.UtcNow.AddMinutes(5) };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.VerifyOtp(request.OtpCode, acc.ResetPasswordToken)).Returns(true);

            var result = await _service.ResetPasswordAsync(request);

            Assert.That(result, Is.True);
            Assert.That(acc.ResetPasswordToken, Is.Null);
            Assert.That(BCrypt.Net.BCrypt.Verify(request.NewPassword, acc.PasswordHash), Is.True);
            _mockAccountRepo.Verify(r => r.UpdateAsync(acc), Times.Once);
        }

        #endregion

        #region 7. SelectProfileAsync Tests

        [Test]
        public void SelectProfileAsync_ProfileNotFound_ThrowsBadRequestException()
        {
            var studentId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(accountId);

            var acc = new Account { Students = new List<Student>() }; // Rỗng
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(acc);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.SelectProfileAsync(studentId));
            Assert.That(ex.Message, Does.Contain("Profile học sinh không hợp lệ"));
        }

        [Test]
        public async Task SelectProfileAsync_ValidProfile_ReturnsFinalToken()
        {
            var studentId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(accountId);

            var student = new Student { StudentId = studentId, FullName = "Học Sinh B" };
            var acc = new Account { AccountId = accountId, Students = new List<Student> { student } };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(acc);
            _mockJwtGenerator.Setup(j => j.GenerateToken(acc, "Student", false, studentId)).Returns("final_token");

            var result = await _service.SelectProfileAsync(studentId);

            Assert.That(result.Token, Is.EqualTo("final_token"));
            Assert.That(result.FullName, Is.EqualTo("Học Sinh B"));
        }

        #endregion

        #region 8. ResendOtpAsync Tests

        [Test]
        public void ResendOtpAsync_EmailNotFound_ThrowsNotFoundException()
        {
            var request = new ResendOtpRequest { Email = "not@found.com" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.ResendOtpAsync(request));
            Assert.That(ex.Message, Does.Contain("chưa được đăng ký trong hệ thống"));
        }

        [Test]
        public void ResendOtpAsync_AccountAlreadyActive_ThrowsBadRequestException()
        {
            var request = new ResendOtpRequest { Email = "exist@test.com" };
            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new Account { Status = "Active" });

            var ex = Assert.ThrowsAsync<BadRequestException>(async () => await _service.ResendOtpAsync(request));
            Assert.That(ex.Message, Does.Contain("Tài khoản đã được xác thực"));
        }

        [Test]
        public async Task ResendOtpAsync_ValidRequest_SendsEmailAndUpdatesToken()
        {
            var request = new ResendOtpRequest { Email = "exist@test.com" };
            var acc = new Account { Status = "Unverified" };

            _mockAccountRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(acc);
            _mockOtpService.Setup(o => o.GenerateOtp()).Returns("112233");
            _mockOtpService.Setup(o => o.HashOtp("112233")).Returns("new_hash");

            var result = await _service.ResendOtpAsync(request);

            Assert.That(result, Is.True);
            Assert.That(acc.VerificationToken, Is.EqualTo("new_hash"));
            _mockEmailService.Verify(e => e.SendEmailAsync(It.IsAny<EmailMessage>()), Times.Once);
            _mockAccountRepo.Verify(r => r.UpdateAsync(acc), Times.Once);
        }

        #endregion
    }
}