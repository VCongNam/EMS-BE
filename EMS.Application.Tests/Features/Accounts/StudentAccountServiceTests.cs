using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Application.Features.Accounts.Services;
using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Accounts
{
    [TestFixture]
    public class StudentAccountServiceTests
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

            _service = new StudentAccountService(
                _mockAccountRepo.Object,
                _mockStudentRepo.Object,
                _mockCurrentUser.Object
            );
        }

        // Helper: Tạo file Excel giả lập trong MemoryStream để test Import
        private Mock<IFormFile> CreateMockExcelFile(bool isEmpty = false, string extension = ".xlsx")
        {
            var mockFile = new Mock<IFormFile>();
            var ms = new MemoryStream();

            if (!isEmpty && extension == ".xlsx")
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Sheet1");
                    // Header
                    ws.Cell(1, 1).Value = "Name";
                    ws.Cell(1, 2).Value = "Phone";
                    ws.Cell(1, 3).Value = "DOB";
                    ws.Cell(1, 4).Value = "Address";

                    // Dòng 2: Hợp lệ
                    ws.Cell(2, 1).Value = "Nguyễn Văn A";
                    ws.Cell(2, 2).Value = "0987654321";
                    ws.Cell(2, 3).Value = "2005-01-01";
                    ws.Cell(2, 4).Value = "Hà Nội";

                    // Dòng 3: Không hợp lệ (Thiếu tên, sai số điện thoại)
                    ws.Cell(3, 1).Value = "";
                    ws.Cell(3, 2).Value = "SaiSoDienThoai";
                    ws.Cell(3, 3).Value = "invalid_date";
                    ws.Cell(3, 4).Value = "Hồ Chí Minh";

                    wb.SaveAs(ms);
                }
            }

            var fileBytes = ms.ToArray();
            var resultStream = new MemoryStream(fileBytes);

            mockFile.Setup(f => f.FileName).Returns($"students{extension}");
            mockFile.Setup(f => f.Length).Returns(isEmpty ? 0 : resultStream.Length);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken ct) =>
                {
                    resultStream.Position = 0;
                    return resultStream.CopyToAsync(target, ct);
                });

            return mockFile;
        }

        #region 1. CreateStudentAsync Tests

        [Test]
        public void CreateStudentAsync_EmptyName_ThrowsException()
        {
            var request = new CreateStudentDto { FullName = "   ", PhoneNumber = "0987654321" };
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateStudentAsync(request));
            Assert.That(ex.Message, Does.Contain("không được để trống"));
        }

        [Test]
        public void CreateStudentAsync_InvalidPhone_ThrowsException()
        {
            var request = new CreateStudentDto { FullName = "Nguyễn Văn A", PhoneNumber = "123" };
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateStudentAsync(request));
            Assert.That(ex.Message, Does.Contain("Số điện thoại không hợp lệ"));
        }

        [Test]
        public void CreateStudentAsync_NewAccount_WeakPassword_ThrowsException()
        {
            var request = new CreateStudentDto { FullName = "Nguyễn Văn A", PhoneNumber = "0987654321", Password = "weak" };
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync((Account)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.CreateStudentAsync(request));
            Assert.That(ex.Message, Does.Contain("không đủ độ phức tạp"));
        }

        [Test]
        public async Task CreateStudentAsync_NewAccount_ValidData_CreatesAccountAndStudent()
        {
            var request = new CreateStudentDto
            {
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0987654321",
                Password = "StrongPassword@123",
                DOB = new DateTime(2000, 1, 1)
            };

            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync((Account)null);
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync("Student")).ReturnsAsync(new Role { RoleId = Guid.NewGuid() });

            // Giả định hồ sơ học sinh chưa có
            _mockStudentRepo.Setup(r => r.IsStudentExistAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>())).ReturnsAsync((Student)null);

            var result = await _service.CreateStudentAsync(request);

            Assert.That(result.IsNewAccount, Is.True);
            Assert.That(result.InitialPassword, Is.EqualTo(request.Password));
            _mockAccountRepo.Verify(r => r.AddAsync(It.Is<Account>(a => a.PhoneNumber == "0987654321")), Times.Once);
            _mockStudentRepo.Verify(r => r.AddAsync(It.Is<Student>(s => s.FullName == "Nguyễn Văn A")), Times.Once);
            _mockStudentRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task CreateStudentAsync_ExistingAccount_ExistingStudentProfile_ReturnsExistingStudentId()
        {
            var request = new CreateStudentDto { FullName = "Nguyễn Văn A", PhoneNumber = "0987654321", DOB = new DateTime(2000, 1, 1) };

            var existingAccount = new Account { AccountId = Guid.NewGuid() };
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(request.PhoneNumber)).ReturnsAsync(existingAccount);

            var existingStudent = new Student { StudentId = Guid.NewGuid() };
            _mockStudentRepo.Setup(r => r.IsStudentExistAsync(existingAccount.AccountId, "Nguyễn Văn A", DateOnly.FromDateTime(request.DOB))).ReturnsAsync(existingStudent);

            var result = await _service.CreateStudentAsync(request);

            Assert.That(result.IsNewAccount, Is.False);
            Assert.That(result.InitialPassword, Is.Null);
            Assert.That(result.StudentId, Is.EqualTo(existingStudent.StudentId));

            // Không tạo mới bất cứ thứ gì
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
            _mockStudentRepo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Never);
        }

        #endregion

        #region 2. ImportStudentsFromExcelAsync Tests

        [Test]
        public void ImportStudentsFromExcelAsync_FileNullOrEmpty_ThrowsException()
        {
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync("Student")).ReturnsAsync(new Role { RoleId = Guid.NewGuid() });
            var mockFile = CreateMockExcelFile(isEmpty: true);
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.ImportStudentsFromExcelAsync(mockFile.Object));
            Assert.That(ex.Message, Does.Contain("không được để trống"));
        }

        [Test]
        public void ImportStudentsFromExcelAsync_FileNotXlsx_ThrowsException()
        {
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync("Student")).ReturnsAsync(new Role { RoleId = Guid.NewGuid() });
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("document.pdf");
            mockFile.Setup(f => f.Length).Returns(1024);
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.ImportStudentsFromExcelAsync(mockFile.Object));
            Assert.That(ex.Message, Does.Contain("hỗ trợ file Excel định dạng .xlsx"));
        }

        [Test]
        public async Task ImportStudentsFromExcelAsync_ValidFile_ProcessesRowsAndReturnsResult()
        {
            var mockFile = CreateMockExcelFile(isEmpty: false, extension: ".xlsx");
            _mockAccountRepo.Setup(r => r.GetRoleByNameAsync("Student")).ReturnsAsync(new Role { RoleId = Guid.NewGuid() });

            // Trả về null để nó tạo Account mới cho cả 2 dòng
            _mockAccountRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>())).ReturnsAsync((Account)null);
            _mockStudentRepo.Setup(r => r.IsStudentExistAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>())).ReturnsAsync((Student)null);

            var result = await _service.ImportStudentsFromExcelAsync(mockFile.Object);

            Assert.That(result.TotalRows, Is.EqualTo(2)); // Tổng cộng có 2 row dữ liệu (Dòng 2 và 3)

            // Dòng 2 hợp lệ
            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(result.NewAccounts.Count, Is.EqualTo(1));
            Assert.That(result.NewAccounts[0].FullName, Is.EqualTo("Nguyễn Văn A"));

            // Dòng 3 không hợp lệ (Tên rỗng, SDT sai, v.v.)
            Assert.That(result.FailedCount, Is.EqualTo(1));
            Assert.That(result.ErrorList.Count, Is.EqualTo(1));
            Assert.That(result.ErrorList[0].ErrorMessage, Does.Contain("không được để trống"));

            Assert.That(result.Base64ExcelReport, Is.Not.Null.And.Not.Empty); // Có xuất báo cáo
            _mockStudentRepo.Verify(r => r.SaveChangesAsync(), Times.Once); // Save DB cho dòng Success
        }

        #endregion

        #region 3. ExportImportResultToExcel Tests

        [Test]
        public void ExportImportResultToExcel_ValidData_ReturnsByteArray()
        {
            var data = new ImportResultDto
            {
                NewAccounts = new List<StudentImportSuccessDto>
                {
                    new StudentImportSuccessDto { FullName = "New", PhoneNumber = "01", Password = "123" }
                },
                ExistedAccounts = new List<StudentImportSuccessDto>
                {
                    new StudentImportSuccessDto { FullName = "Exist", PhoneNumber = "02" }
                },
                ErrorList = new List<ImportErrorDto>
                {
                    new ImportErrorDto { RowNumber = 3, StudentName = "Err", ErrorMessage = "Lỗi" }
                }
            };

            var bytes = _service.ExportImportResultToExcel(data);

            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes.Length, Is.GreaterThan(0));
        }

        #endregion

        #region 4. ResetStudentPasswordAsync Tests

        [Test]
        public void ResetStudentPasswordAsync_NotTeacher_ThrowsException()
        {
            _mockCurrentUser.Setup(c => c.Role).Returns("Student"); // Không phải Teacher
            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.ResetStudentPasswordAsync(Guid.NewGuid(), "NewPass123!"));
            Assert.That(ex.Message, Does.Contain("Bạn phải là giáo viên"));
        }

        [Test]
        public void ResetStudentPasswordAsync_StudentNotFound_ThrowsKeyNotFoundException()
        {
            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockStudentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Student)null);

            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.ResetStudentPasswordAsync(Guid.NewGuid(), "NewPass123!"));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy hồ sơ học sinh"));
        }

        [Test]
        public void ResetStudentPasswordAsync_TeacherDoesNotHaveStudent_ThrowsException()
        {
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);

            _mockStudentRepo.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(new Student());
            _mockStudentRepo.Setup(r => r.IsTeacherHasStudent(studentId, teacherId)).ReturnsAsync(false); // Check = false

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.ResetStudentPasswordAsync(studentId, "NewPass123!"));
            Assert.That(ex.Message, Does.Contain("không thuộc các lớp của bạn"));
        }

        [Test]
        public void ResetStudentPasswordAsync_AccountNotFound_ThrowsKeyNotFoundException()
        {
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);

            _mockStudentRepo.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(new Student { AccountId = accountId });
            _mockStudentRepo.Setup(r => r.IsTeacherHasStudent(studentId, teacherId)).ReturnsAsync(true);

            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account)null); // Mất Account

            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.ResetStudentPasswordAsync(studentId, "NewPass123!"));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy tài khoản liên kết"));
        }

        [Test]
        public async Task ResetStudentPasswordAsync_ValidData_UpdatesPasswordAndStatus()
        {
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            _mockCurrentUser.Setup(c => c.Role).Returns("Teacher");
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);

            _mockStudentRepo.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(new Student { AccountId = accountId });
            _mockStudentRepo.Setup(r => r.IsTeacherHasStudent(studentId, teacherId)).ReturnsAsync(true);

            var account = new Account { Status = "Active", PasswordHash = "OldHash" };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.ResetStudentPasswordAsync(studentId, "NewStrongPass123!");

            Assert.That(result, Is.True);
            Assert.That(account.Status, Is.EqualTo("Unverified"));
            Assert.That(BCrypt.Net.BCrypt.Verify("NewStrongPass123!", account.PasswordHash), Is.True);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }

        #endregion
    }
}