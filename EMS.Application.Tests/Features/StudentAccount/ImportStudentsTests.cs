using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.Services;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Tests.Features.StudentAccount
{
    [TestFixture]
    public class ImportStudentsTests
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
        public void UTCID03_NullFile_ThrowsException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(() => _service.ImportStudentsFromExcelAsync(null));
            Assert.That(ex.Message, Is.EqualTo("File không được để trống."));
        }

        [Test]
        public void UTCID02_WrongExtension_ThrowsException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("students.csv");
            mockFile.Setup(f => f.Length).Returns(1000); // Kích thước > 0

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(() => _service.ImportStudentsFromExcelAsync(mockFile.Object));
            Assert.That(ex.Message, Is.EqualTo("Hệ thống chỉ hỗ trợ file Excel định dạng .xlsx"));
        }
    }
}
