using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using BCrypt.Net;

// Import các namespace từ dự án chính của bạn
using EMS.Application.Features.Auth.Services;
using EMS.Application.Common.Interfaces;
using EMS.Domain.Interfaces;
using EMS.Application.Features.Auth.DTOs;
using EMS.Domain.Entities;

namespace EMS.Application.Tests.Features.Auth.Services
{
    [TestFixture] // Báo cho NUnit biết đây là class chạy Test
    public class AuthServiceTests
    {
        // 1. Khai báo các đối tượng Mock
        private Mock<IAccountRepository> _mockAccountRepo;
        private Mock<IJwtTokenGenerator> _mockJwtGenerator;
        private Mock<IOtpService> _mockOtpService;
        private Mock<IEmailService> _mockEmailService;
        private Mock<ICurrentUserService> _mockCurrentUserService;

        // Khai báo class cần test
        private AuthService _authService;

        // 2. Hàm Setup: Chạy trước MỖI test case để dọn dẹp và reset lại dữ liệu
        [SetUp]
        public void SetUp()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
            _mockOtpService = new Mock<IOtpService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            // Bơm các mock object vào AuthService
            _authService = new AuthService(
                _mockAccountRepo.Object,
                _mockJwtGenerator.Object,
                _mockOtpService.Object,
                _mockEmailService.Object,
                _mockCurrentUserService.Object
            );
        }

        // 3. Viết Test Case Bình thường (Happy Path)
        [Test]
        public async Task LoginAsync_ValidCredentials_ReturnsToken()
        {
            // ARRANGE (Chuẩn bị)
            var request = new LoginRequest
            {
                Identifier = "admin@ems.com",
                Password = "Password123!",
                SelectedRole = "Teacher"
            };

            var fakeAccount = new Account
            {
                AccountId = Guid.NewGuid(),
                Email = "admin@ems.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Status = "Active",
                Role = new Role { RoleName = "Teacher" }
            };

            // Setup Mock: Dạy repo cách trả về dữ liệu giả
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(request.Identifier))
                            .ReturnsAsync(fakeAccount);

            _mockJwtGenerator.Setup(x => x.GenerateToken(fakeAccount, "Teacher", false, null))
                             .Returns("valid_mock_token");

            // ACT (Thực thi)
            var response = await _authService.LoginAsync(request);

            // ASSERT (Xác nhận)
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Token, Is.EqualTo("valid_mock_token"));
            Assert.That(response.RoleName, Is.EqualTo("Teacher"));
        }

        // 4. Viết Test Case Bất thường (Gộp nhiều trường hợp bằng TestCase)
        [TestCase("teacher@ems.com", "Teacher", "Banned", "Tài khoản đã bị khóa!")]
        [TestCase("teacher@ems.com", "Teacher", "Unverified", "Tài khoản của bạn chưa được kích hoạt. Vui lòng xác thực mã OTP!")]
        public void LoginAsync_InvalidStatus_ThrowsException(
            string email, string role, string status, string expectedMsg)
        {
            // ARRANGE
            var request = new LoginRequest
            {
                Identifier = email,
                Password = "ValidPassword123!",
                SelectedRole = role
            };

            var fakeAccount = new Account
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword123!"),
                Status = status, // Thay đổi linh hoạt theo [TestCase]
                Role = new Role { RoleName = role }
            };

            _mockAccountRepo.Setup(x => x.GetByEmailAsync(request.Identifier))
                            .ReturnsAsync(fakeAccount);

            // ACT & ASSERT
            var exception = Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(request));
            Assert.That(exception.Message, Is.EqualTo(expectedMsg));
        }

        [Test]
        public void LoginAsync_AccountNotFound_ThrowsException()
        {
            // ARRANGE
            var request = new LoginRequest { Identifier = "notfound@ems.com", SelectedRole = "Teacher" };

            // Setup trả về null vì không tìm thấy
            _mockAccountRepo.Setup(x => x.GetByEmailAsync(request.Identifier))
                            .ReturnsAsync((Account)null);

            // ACT & ASSERT
            var exception = Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(request));
            Assert.That(exception.Message, Is.EqualTo("Thông tin đăng nhập không chính xác!"));
        }
    }
}