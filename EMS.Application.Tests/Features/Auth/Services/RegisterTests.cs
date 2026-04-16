using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using EMS.Application.Features.Auth.Services;
using EMS.Application.Features.Auth.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Application.Common.Interfaces;

namespace EMS.Application.Tests.Services
{
    [TestFixture]
    public class RegisterTests
    {
        private Mock<IAccountRepository> _mockRepo;
        private Mock<IOtpService> _mockOtp;
        private Mock<IEmailService> _mockEmail;
        private Mock<IJwtTokenGenerator> _mockJwt;
        private Mock<ICurrentUserService> _mockUser;
        private AuthService _service;

        [SetUp]
        public void SetUp()
        {
            _mockRepo = new Mock<IAccountRepository>();
            _mockOtp = new Mock<IOtpService>();
            _mockEmail = new Mock<IEmailService>();
            _mockJwt = new Mock<IJwtTokenGenerator>();
            _mockUser = new Mock<ICurrentUserService>();

            _service = new AuthService(
                _mockRepo.Object, _mockJwt.Object, _mockOtp.Object,
                _mockEmail.Object, _mockUser.Object);
        }

        // UTC 01 & 02: Happy Path
        [TestCase("Teacher")]
        [TestCase("TA")]
        public async Task RegisterAsync_ValidRequest_ReturnsSuccess(string role)
        {
            var request = new RegisterRequest { Email = "test@ems.com", RoleName = role, Password = "123" };
            _mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Account)null);
            _mockRepo.Setup(r => r.GetRoleByNameAsync(role)).ReturnsAsync(new Role { RoleId = Guid.NewGuid(), RoleName = role });
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Account>())).ReturnsAsync((Account a) => { a.Role = new Role { RoleName = role }; return a; });

            var result = await _service.RegisterAsync(request);

            Assert.That(result.RoleName, Is.EqualTo(role));
            _mockEmail.Verify(e => e.SendEmailAsync(It.IsAny<EmailMessage>()), Times.Once);
        }

        // UTC 03: Duplicate Email
        [Test]
        public void RegisterAsync_EmailExists_ThrowsException()
        {
            var request = new RegisterRequest { Email = "exists@ems.com" };
            _mockRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new Account());

            var ex = Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(request));
            Assert.That(ex.Message, Is.EqualTo("Email đã được sử dụng!"));
        }

        // UTC 04 & 06: Invalid/Empty Role
        [TestCase("Student")]
        [TestCase("")]
        public void RegisterAsync_InvalidRole_ThrowsException(string role)
        {
            var request = new RegisterRequest { Email = "new@ems.com", RoleName = role };
            var ex = Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(request));
            Assert.That(ex.Message, Does.Contain("Quyền đăng ký không hợp lệ"));
        }

        // UTC 07: System Failure - Email Service Down
        [Test]
        public void RegisterAsync_EmailServiceDown_ThrowsException()
        {
            var request = new RegisterRequest { Email = "fail@ems.com", RoleName = "Teacher" };
            _mockRepo.Setup(r => r.GetRoleByNameAsync("Teacher")).ReturnsAsync(new Role { RoleId = Guid.NewGuid() });
            _mockEmail.Setup(e => e.SendEmailAsync(It.IsAny<EmailMessage>())).ThrowsAsync(new Exception("SMTP Fail"));

            Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(request));
        }

        // UTC 10: Boundary - Email Case Sensitivity
        [Test]
        public void RegisterAsync_EmailCaseMismatch_ThrowsException()
        {
            var request = new RegisterRequest { Email = "khue@ems.com" };
            _mockRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new Account { Email = "KHUE@EMS.COM" });

            var ex = Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(request));
            Assert.That(ex.Message, Is.EqualTo("Email đã được sử dụng!"));
        }
        #region 7. DATA VALIDATION CASES (Email & Password Strength)

        [TestCase("plainaddress")]
        [TestCase("#@%^%#$@#$@#.com")]
        [TestCase("@example.com")]
        [TestCase("Joe Smith <email@example.com>")]
        [TestCase("email.example.com")]
        public void RegisterAsync_InvalidEmailFormat_ThrowsException(string invalidEmail)
        {
            // ARRANGE
            var request = new RegisterRequest
            {
                Email = invalidEmail,
                Password = "StrongP@ss123!",
                RoleName = "Teacher"
            };

            // ACT & ASSERT
            var ex = Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(request));
            // Note: You need to implement this check in your AuthService to make this test pass
            Assert.That(ex.Message, Does.Contain("Invalid email format"));
        }

        [TestCase("123")] // Too short
        [TestCase("password")] // No numbers or special chars
        [TestCase("ABCDEFGH")] // No lowercase or numbers
        [TestCase("12345678")] // No letters
        public void RegisterAsync_WeakPassword_ThrowsException(string weakPassword)
        {
            // ARRANGE
            var request = new RegisterRequest
            {
                Email = "valid@ems.com",
                Password = weakPassword,
                RoleName = "Teacher"
            };

            // ACT & ASSERT
            var ex = Assert.ThrowsAsync<Exception>(() => _service.RegisterAsync(request));
            // Note: You need to implement password strength logic to make this test pass
            Assert.That(ex.Message, Does.Contain("Password is too weak"));
        }

        [Test]
        public async Task RegisterAsync_StrongPasswordAndValidEmail_ReturnsSuccess()
        {
            // ARRANGE (The "Perfect" Request)
            var request = new RegisterRequest
            {
                Email = "khue.dev@ems.com",
                FullName = "Khue Software",
                Password = "StrongP@ssword123!", // Meets all security criteria
                RoleName = "Teacher"
            };

            _mockRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Account)null);
            _mockRepo.Setup(r => r.GetRoleByNameAsync("Teacher")).ReturnsAsync(new Role { RoleId = Guid.NewGuid(), RoleName = "Teacher" });
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Account>())).ReturnsAsync((Account a) => { a.Role = new Role { RoleName = "Teacher" }; return a; });

            // ACT
            var result = await _service.RegisterAsync(request);

            // ASSERT
            Assert.That(result, Is.Not.Null);
            Assert.That(result.FullName, Is.EqualTo("Khue Software"));
        }

        #endregion
    }
}