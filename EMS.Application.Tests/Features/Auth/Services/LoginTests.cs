using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using EMS.Application.Features.Auth.Services;
using EMS.Application.Features.Auth.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Application.Common.Interfaces;

namespace EMS.Application.Tests.Features.Auth.Services
{
    [TestFixture]
    public class LoginTests
    {
        private Mock<IAccountRepository> _mockRepo;
        private Mock<IJwtTokenGenerator> _mockJwt;
        private AuthService _service;

        [SetUp]
        public void SetUp()
        {
            _mockRepo = new Mock<IAccountRepository>();
            _mockJwt = new Mock<IJwtTokenGenerator>();

            // Các service phụ không dùng trực tiếp logic login thì mock mặc định
            var mockOtp = new Mock<IOtpService>();
            var mockEmail = new Mock<IEmailService>();
            var mockUser = new Mock<ICurrentUserService>();

            _service = new AuthService(
                _mockRepo.Object, _mockJwt.Object, mockOtp.Object,
                mockEmail.Object, mockUser.Object);
        }

        #region NORMAL CASES (Dòng 5.1 & 5.2 trong Matrix)

        [Test] // UTC 01: Teacher Login Success (Identifier = Email)
        public async Task LoginAsync_TeacherValid_ReturnsToken()
        {
            // ARRANGE
            var request = new LoginRequest
            {
                Identifier = "khue.dev@gmail.com", // Identifier là Email
                Password = "CorrectPass123!",
                SelectedRole = "Teacher"
            };

            var account = new Account
            {
                Email = request.Identifier,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = "Active",
                Role = new Role { RoleName = "Teacher" }
            };

            _mockRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync(account);
            _mockJwt.Setup(j => j.GenerateToken(account, "Teacher", false, null)).Returns("jwt_token");

            // ACT
            var result = await _service.LoginAsync(request);

            // ASSERT
            Assert.That(result.Token, Is.EqualTo("jwt_token"));
        }

        [Test] // UTC 02: Student Login Success (Identifier = Phone Number)
        public async Task LoginAsync_StudentValid_ReturnsTempToken()
        {
            // ARRANGE
            var request = new LoginRequest
            {
                Identifier = "0987654321", // Identifier là SĐT
                Password = "CorrectPass123!",
                SelectedRole = "Student"
            };

            var account = new Account
            {
                PhoneNumber = request.Identifier,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = "Active",
                Role = new Role { RoleName = "Student" },
                Students = new List<Student> { new Student { FullName = "Khuê Student" } }
            };

            _mockRepo.Setup(r => r.GetByPhoneAsync(request.Identifier)).ReturnsAsync(account);
            _mockJwt.Setup(j => j.GenerateToken(account, "Student", true, null)).Returns("temp_token");

            // ACT
            var result = await _service.LoginAsync(request);

            // ASSERT
            Assert.That(result.TempToken, Is.EqualTo("temp_token"));
            Assert.That(result.RequiresProfileSelection, Is.True);
        }
        #endregion

        #region ABNORMAL CASES (Dòng 6.1 -> 6.4 trong Matrix)

        [Test] // UTC 03: Wrong Password
        public void LoginAsync_WrongPassword_ThrowsException()
        {
            var request = new LoginRequest { Identifier = "khue.dev@gmail.com", Password = "WrongPassword", SelectedRole = "Teacher" };
            var account = new Account
            {
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass123!"),
                Role = new Role { RoleName = "Teacher" }
            };
            _mockRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync(account);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.LoginAsync(request));
            Assert.That(ex.Message, Is.EqualTo("Mật khẩu không chính xác!"));
        }

        [Test] // UTC 04: Banned Account
        public void LoginAsync_BannedAccount_ThrowsException()
        {
            var request = new LoginRequest { Identifier = "khue.dev@gmail.com", Password = "CorrectPass123!", SelectedRole = "Teacher" };
            var account = new Account
            {
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = "Banned",
                Role = new Role { RoleName = "Teacher" }
            };
            _mockRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync(account);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.LoginAsync(request));
            Assert.That(ex.Message, Is.EqualTo("Tài khoản đã bị khóa!"));
        }

        [Test] // UTC 07: Identifier Not Found
        public void LoginAsync_UserNotFound_ThrowsException()
        {
            var request = new LoginRequest { Identifier = "wrong@gmail.com", Password = "123", SelectedRole = "Teacher" };
            _mockRepo.Setup(r => r.GetByEmailAsync(request.Identifier)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.LoginAsync(request));
            Assert.That(ex.Message, Is.EqualTo("Thông tin đăng nhập không chính xác!"));
        }
        #endregion
    }
}