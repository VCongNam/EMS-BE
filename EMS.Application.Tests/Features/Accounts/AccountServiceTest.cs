using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Application.Features.Accounts.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Tests.Features.Accounts
{
    [TestFixture]
    public class AccountServiceTests
    {
        private Mock<IAccountRepository> _mockRepo;
        private Mock<ICurrentUserService> _mockUser;
        private Mock<ISupabaseStorageService> _mockStorage;
        private AccountService _service;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<IAccountRepository>();
            _mockUser = new Mock<ICurrentUserService>();
            _mockStorage = new Mock<ISupabaseStorageService>();

            _service = new AccountService(_mockRepo.Object, _mockUser.Object, _mockStorage.Object);
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            return obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null);
        }

        #region 1. GetProfileAsync Tests

        [Test]
        public void UTC_GetProfile_AccountNotFound_ThrowsException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.GetProfileAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Is.EqualTo("Tài khoản không tồn tại!"));
        }

        [Test]
        public async Task UTC_GetProfile_TeacherRole_ReturnsCorrectData()
        {
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "Teacher" },
                Teacher = new Teacher { Bio = "Experienced Teacher", BankName = "OCB" },
                CreatedAt = DateTime.UtcNow,
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.GetProfileAsync(accountId);

            Assert.That(result.RoleName, Is.EqualTo("Teacher"));
            Assert.That(result.RoleSpecificData, Is.Not.Null);
            Assert.That(GetPropertyValue(result.RoleSpecificData, "BankName"), Is.EqualTo("OCB"));
            Assert.That(GetPropertyValue(result.RoleSpecificData, "Bio"), Is.EqualTo("Experienced Teacher"));
        }

        [Test]
        public async Task UTC_GetProfile_TARole_ReturnsCorrectData()
        {
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "TA" },
                TeachingAssistant = new TeachingAssistant { Bio = "Helpful TA", BankName = "MBBank" },
                CreatedAt = DateTime.UtcNow,
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.GetProfileAsync(accountId);

            Assert.That(result.RoleName, Is.EqualTo("TA"));
            Assert.That(result.RoleSpecificData, Is.Not.Null);
            Assert.That(GetPropertyValue(result.RoleSpecificData, "BankName"), Is.EqualTo("MBBank"));
        }

        [Test]
        public async Task UTC_GetProfile_StudentRole_ReturnsCorrectData()
        {
            var accountId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockUser.Setup(u => u.StudentId).Returns(studentId);

            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "Student" },
                Students = new List<Student> { new Student { StudentId = studentId, Address = "Hanoi" } },
                CreatedAt = DateTime.UtcNow,
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.GetProfileAsync(accountId);

            Assert.That(result.RoleName, Is.EqualTo("Student"));
            Assert.That(result.RoleSpecificData, Is.Not.Null);
            Assert.That(GetPropertyValue(result.RoleSpecificData, "Address"), Is.EqualTo("Hanoi"));
        }

        [Test]
        public void UTC_GetProfile_StudentRole_StudentProfileNotFound_ThrowsException()
        {
            var accountId = Guid.NewGuid();
            _mockUser.Setup(u => u.StudentId).Returns(Guid.NewGuid()); // Một StudentId không khớp

            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "Student" },
                Students = new List<Student>(), // Không có data
                CreatedAt = DateTime.UtcNow,
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.GetProfileAsync(accountId));
            Assert.That(ex.Message, Is.EqualTo("Hồ sơ học sinh không tồn tại"));
        }

        #endregion

        #region 2. UpdateTeacherProfileAsync & UpdateTAProfileAsync Tests

        [Test]
        public void UTC_UpdateTeacherProfile_InvalidAccountOrRole_ThrowsException()
        {
            var accountId = Guid.NewGuid();
            var account = new Account { Role = new Role { RoleName = "Student" } }; 
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.UpdateTeacherProfileAsync(accountId, new UpdateTeacherProfileRequest()));
            Assert.That(ex.Message, Is.EqualTo("Tài khoản không hợp lệ hoặc không phải Giáo viên!"));
        }

        [Test]
        public async Task UTC_UpdateTeacherProfile_Success_UpdatesAndReturnsProfile()
        {
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "Teacher" },
                Teacher = new Teacher { Bio = "Old Bio" },
                CreatedAt = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new UpdateTeacherProfileRequest { FullName = "New Name", Bio = "New Bio" };

            var result = await _service.UpdateTeacherProfileAsync(accountId, request);

            Assert.That(account.FullName, Is.EqualTo("New Name"));
            Assert.That(account.Teacher.Bio, Is.EqualTo("New Bio"));
            _mockRepo.Verify(r => r.UpdateAsync(account), Times.Once);
            Assert.That(result.RoleName, Is.EqualTo("Teacher"));
        }

        [Test]
        public void UTC_UpdateTAProfile_InvalidAccountOrRole_ThrowsException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account)null); 

            var ex = Assert.ThrowsAsync<Exception>(() => _service.UpdateTAProfileAsync(Guid.NewGuid(), new UpdateTAProfileRequest()));
            Assert.That(ex.Message, Is.EqualTo("Tài khoản không hợp lệ hoặc không phải Trợ giảng!"));
        }

        [Test]
        public async Task UTC_UpdateTAProfile_Success_UpdatesAndReturnsProfile()
        {
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "TA" },
                TeachingAssistant = new TeachingAssistant(),
                CreatedAt = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new UpdateTAProfileRequest { FullName = "TA Name", BankName = "TPBank" };

            await _service.UpdateTAProfileAsync(accountId, request);

            Assert.That(account.FullName, Is.EqualTo("TA Name"));
            Assert.That(account.TeachingAssistant.BankName, Is.EqualTo("TPBank"));
            _mockRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }

        #endregion

        #region 3. UpdateStudentProfileAsync Tests

        [Test]
        public void UTC_UpdateStudentProfile_EmptyName_ThrowsException()
        {
            var accountId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockUser.Setup(u => u.StudentId).Returns(studentId);

            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "Student" },
                Students = new List<Student> { new Student { StudentId = studentId } },
                CreatedAt = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new UpdateStudentProfileRequest { FullName = "   " }; 

            var ex = Assert.ThrowsAsync<Exception>(() => _service.UpdateStudentProfileAsync(accountId, request));
            Assert.That(ex.Message, Is.EqualTo("Tên học sinh không được để trống"));
        }

        [Test]
        public async Task UTC_UpdateStudentProfile_Success_UpdatesAndReturnsProfile()
        {
            var accountId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockUser.Setup(u => u.StudentId).Returns(studentId);

            var studentInfo = new Student { StudentId = studentId, FullName = "Old Name" };
            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "Student" },
                Students = new List<Student> { studentInfo },
                CreatedAt = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new UpdateStudentProfileRequest { FullName = "New Student Name", Address = "Da Nang" };

            var result = await _service.UpdateStudentProfileAsync(accountId, request);

            Assert.That(studentInfo.FullName, Is.EqualTo("New Student Name"));
            Assert.That(studentInfo.Address, Is.EqualTo("Da Nang"));
            Assert.That(account.FullName, Is.EqualTo("New Student Name"));
            _mockRepo.Verify(r => r.UpdateAsync(account), Times.Once);
            Assert.That(result.RoleName, Is.EqualTo("Student"));
        }

        [Test]
        public async Task UTC_UpdateStudentProfile_StudentNotFound_DoesNotUpdateButRunsSuccessfully()
        {
            var accountId = Guid.NewGuid();
            _mockUser.Setup(u => u.StudentId).Returns(Guid.NewGuid()); 

            var account = new Account
            {
                AccountId = accountId,
                Role = new Role { RoleName = "Teacher" },
                Students = new List<Student>(),
                CreatedAt = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new UpdateStudentProfileRequest { FullName = "Name" };

            await _service.UpdateStudentProfileAsync(accountId, request);

            _mockRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }

        #endregion

        #region 4. ChangePasswordAsync Tests

        [Test]
        public void UTC_ChangePassword_AccountNotFound_ThrowsException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest()));
            Assert.That(ex.Message, Is.EqualTo("Tài khoản không tồn tại!"));
        }

        [Test]
        public void UTC_ChangePassword_WrongOldPassword_ThrowsException()
        {
            var accountId = Guid.NewGuid();
            var hashedPass = BCrypt.Net.BCrypt.HashPassword("Correct123");
            var account = new Account { AccountId = accountId, PasswordHash = hashedPass };

            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new ChangePasswordRequest { OldPassword = "WrongPassword", NewPassword = "New123" };

            var ex = Assert.ThrowsAsync<Exception>(() => _service.ChangePasswordAsync(accountId, request));
            Assert.That(ex.Message, Is.EqualTo("Mật khẩu cũ không chính xác!"));
        }

        [Test]
        public async Task UTC_ChangePassword_Success_UpdatesHashedPassword()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var oldPass = "Old123@abc";

            var originalHash = BCrypt.Net.BCrypt.HashPassword(oldPass);

            var account = new Account { AccountId = accountId, PasswordHash = originalHash };

            _mockRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
            var request = new ChangePasswordRequest { OldPassword = oldPass, NewPassword = "NewSecurePass!1" };

            // Act
            var result = await _service.ChangePasswordAsync(accountId, request);

            // Assert
            Assert.That(result, Is.True);

            Assert.That(account.PasswordHash, Is.Not.EqualTo(originalHash), "Mật khẩu mới chưa được Hash và cập nhật!");
            Assert.That(account.PasswordHash, Is.Not.Null.And.Not.Empty);

            _mockRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }

        #endregion
    }
}