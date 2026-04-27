using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Application.Features.Accounts.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Tests.Features.StudentAccount
{
    [TestFixture]
    public class CreateStudentAsyncTests
    {
        private Mock<IAccountRepository> _mockAccountRepo;
        private Mock<IStudentRepository> _mockStudentRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private StudentAccountService _service;

        [SetUp]
        public void Setup()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockStudentRepo = new Mock<IStudentRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();

            _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _mockCurrentUser.Setup(x => x.Role).Returns("Teacher");
            _service = new StudentAccountService(_mockAccountRepo.Object, _mockStudentRepo.Object, _mockCurrentUser.Object);
        }

        [Test]
        public void UTCID01_EmptyFullName_ThrowsException()
        {
            // Arrange
            var request = new CreateStudentDto { FullName = "", PhoneNumber = "0987654321" };

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(() => _service.CreateStudentAsync(request));
            Assert.That(ex.Message, Is.EqualTo("Tên học sinh không được để trống."));
        }

        [Test]
        public void UTCID03_NewAccount_WeakPassword_ThrowsException()
        {
            // Arrange
            var request = new CreateStudentDto { FullName = "Nguyen Van A", PhoneNumber = "0987654321", Password = "123" };
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((Account)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(() => _service.CreateStudentAsync(request));
            Assert.That(ex.Message, Does.Contain("Mật khẩu tạo mới không đủ độ phức tạp"));
        }

        [Test]
        public async Task UTCID06_ValidNewStudent_ReturnsSuccessAndSavesToDb()
        {
            // Arrange
            var request = new CreateStudentDto
            {
                FullName = "Nguyen Van A",
                PhoneNumber = "0987654321",
                Password = "StrongPassword@123", // Giả sử thỏa mãn DataValidator
                DOB = new DateTime(2010, 1, 1)
            };

            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((Account)null);
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync("Student")).ReturnsAsync(new Role { RoleId = Guid.NewGuid() });
            _mockStudentRepo.Setup(r => r.IsStudentExistAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>())).ReturnsAsync((Student)null);

            // Act
            var result = await _service.CreateStudentAsync(request);

            // Assert
            Assert.That(result.IsNewAccount, Is.True);
            Assert.That(result.InitialPassword, Is.EqualTo(request.Password));
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once); // Confirm Account created
            _mockStudentRepo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Once); // Confirm Student created
            _mockStudentRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
