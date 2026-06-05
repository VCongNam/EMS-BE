using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EMS.Application.Common.DTOs;
using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Application.Features.TuitionFees.Services;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.TuitionFees
{
    [TestFixture]
    public class StudentTuitionServiceTests
    {
        private Mock<ITuitionFeeRepository> _mockTuitionRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<IVietQRService> _mockVietQRService;
        private Mock<ISupabaseStorageService> _mockStorageService;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<ILogger<StudentTuitionService>> _mockLogger;
        private StudentTuitionService _service;

        [SetUp]
        public void Setup()
        {
            _mockTuitionRepo = new Mock<ITuitionFeeRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockVietQRService = new Mock<IVietQRService>();
            _mockStorageService = new Mock<ISupabaseStorageService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockClassRepo = new Mock<IClassRepository>();
            _mockLogger = new Mock<ILogger<StudentTuitionService>>();

            _service = new StudentTuitionService(
                _mockTuitionRepo.Object,
                _mockCurrentUser.Object,
                _mockVietQRService.Object,
                _mockStorageService.Object,
                _mockNotificationService.Object,
                _mockClassRepo.Object,
                _mockLogger.Object
            );
        }

        private Mock<IFormFile> CreateMockFile(string fileName, long length, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            return mockFile;
        }

        #region 1. GetMyTuitionAsync Tests

        [Test]
        public void GetMyTuitionAsync_StudentIdNull_ThrowsUnauthorized()
        {
            _mockCurrentUser.Setup(c => c.StudentId).Returns((Guid?)null);
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.GetMyTuitionAsync(new TuitionFilter()));
            Assert.That(ex.Message, Does.Contain("Student ID is missing"));
        }

        [Test]
        public void GetMyTuitionAsync_ClassFilterNotEnrolled_ThrowsUnauthorized()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(false);

            var filter = new TuitionFilter { ClassID = Guid.NewGuid() };
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.GetMyTuitionAsync(filter));
            Assert.That(ex.Message, Does.Contain("không có quyền truy cập lớp này"));
        }

        [Test]
        public async Task GetMyTuitionAsync_ValidData_CalculatesStatusCorrectly()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var tuples = new List<(Invoice Invoice, Transaction LatestTransaction)>
            {
                (new Invoice { Status = "Paid", Amount = 100 }, null), // Paid -> Đã nộp
                (new Invoice { Status = "Pending", Amount = 100 }, new Transaction { Status = "Pending" }), // Pending -> Chờ xác nhận
                (new Invoice { Status = "Pending", DueDate = DateTime.Now.AddDays(-1) }, null), // Overdue -> Quá hạn
                (new Invoice { Status = "Pending", DueDate = DateTime.Now.AddDays(5) }, null) // Normal -> Chưa nộp
            };

            _mockTuitionRepo.Setup(r => r.GetStudentInvoicesAsync(studentId, It.IsAny<int>(), It.IsAny<int>(), null))
                            .ReturnsAsync((tuples, 4));

            var result = await _service.GetMyTuitionAsync(new TuitionFilter { Size = 10, Page = 1 });

            Assert.That(result.TotalCount, Is.EqualTo(4));
            Assert.That(result.Items[0].DisplayStatus, Is.EqualTo("Đã nộp"));
            Assert.That(result.Items[1].DisplayStatus, Is.EqualTo("Chờ xác nhận"));
            Assert.That(result.Items[2].DisplayStatus, Is.EqualTo("Quá hạn"));
            Assert.That(result.Items[2].CanPay, Is.True);
            Assert.That(result.Items[3].DisplayStatus, Is.EqualTo("Chưa nộp"));
        }

        #endregion

        #region 2. GetTuitionInvoiceDetailAsync Tests

        [Test]
        public void GetTuitionInvoiceDetailAsync_InvoiceNotFound_ThrowsKeyNotFound()
        {
            _mockCurrentUser.Setup(c => c.StudentId).Returns(Guid.NewGuid());

            // Setup Tuple trả về (Invoice = null)
            _mockTuitionRepo.Setup(r => r.GetInvoiceDetailAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                            .ReturnsAsync(((Invoice)null, (Transaction)null, new List<Attendance>()));

            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.GetTuitionInvoiceDetailAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy hóa đơn"));
        }

        [Test]
        public async Task GetTuitionInvoiceDetailAsync_ValidInvoiceFailedTransaction_ReturnsCorrectDisplay()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                Status = "Pending",
                DueDate = DateTime.Now.AddDays(5),
                SessionCount = 10, 
                Amount = 500000  
            };
            var transaction = new Transaction { Status = "Failed" };

            _mockTuitionRepo.Setup(r => r.GetInvoiceDetailAsync(It.IsAny<Guid>(), studentId))
                            .ReturnsAsync((invoice, transaction, new List<Attendance>()));

            // Act
            var result = await _service.GetTuitionInvoiceDetailAsync(invoice.InvoiceId);

            // Assert
            Assert.That(result.StatusDisplay, Is.EqualTo("Giao dịch bị từ chối"));
            Assert.That(result.CanPay, Is.True);
            Assert.That(result.TotalSessions, Is.EqualTo(10));
        }

        #endregion

        #region 3. GetPaymentQrCodeAsync Tests

        [Test]
        public void GetPaymentQrCodeAsync_NotEnrolled_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var invoice = new Invoice { ClassId = Guid.NewGuid() };
            _mockTuitionRepo.Setup(r => r.GetInvoiceWithTeacherBankInfoAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(invoice);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(invoice.ClassId, studentId)).ReturnsAsync(false);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetPaymentQrCodeAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("không thuộc lớp"));
        }

        [Test]
        public void GetPaymentQrCodeAsync_TeacherNoBank_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var invoice = new Invoice { ClassId = Guid.NewGuid(), Class = new Class { Teacher = new Teacher { BankAccount = null } } };
            _mockTuitionRepo.Setup(r => r.GetInvoiceWithTeacherBankInfoAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync(invoice);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(invoice.ClassId, studentId)).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetPaymentQrCodeAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Giáo viên chưa cập nhật thông tin tài khoản"));
        }

        [Test]
        public async Task GetPaymentQrCodeAsync_Valid_ReturnsQrCodeDto()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                Amount = 500000,
                ClassId = Guid.NewGuid(),
                Class = new Class { Teacher = new Teacher { BankName = "VCB", BankAccount = "123", BankAccountName = "GV" } }
            };

            _mockTuitionRepo.Setup(r => r.GetInvoiceWithTeacherBankInfoAsync(invoice.InvoiceId, studentId)).ReturnsAsync(invoice);
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(invoice.ClassId, studentId)).ReturnsAsync(true);
            _mockVietQRService.Setup(v => v.GenerateQRCodeAsync(It.IsAny<VietQRRequest>())).ReturnsAsync("Base64String");

            var result = await _service.GetPaymentQrCodeAsync(invoice.InvoiceId);

            Assert.That(result.QrCodeBase64, Is.EqualTo("Base64String"));
            Assert.That(result.BankName, Is.EqualTo("VCB"));
        }

        #endregion

        #region 4. UploadPaymentProofAsync Tests

        [Test]
        public void UploadPaymentProofAsync_InvoicePaid_ThrowsInvalidOperationException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            // Mock Tuple theo đúng logic (Invoice, Transaction, Attendances)
            var invoice = new Invoice { Status = "Paid" };
            _mockTuitionRepo.Setup(r => r.GetInvoiceDetailAsync(It.IsAny<Guid>(), studentId))
                            .ReturnsAsync((invoice, (Transaction)null, new List<Attendance>())); // Trả về Invoice.Status = Paid

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.UploadPaymentProofAsync(Guid.NewGuid(), new ProofUploadDto()));
            Assert.That(ex.Message, Does.Contain("đã được thanh toán"));
        }

        [Test]
        public async Task UploadPaymentProofAsync_FirstTimeUpload_CreatesTransactionAndNotifies()
        {
            var invoiceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var invoice = new Invoice { Amount = 100000, Status = "Pending" };
            _mockTuitionRepo.Setup(r => r.GetInvoiceDetailAsync(invoiceId, studentId))
                            .ReturnsAsync((invoice, (Transaction)null, new List<Attendance>())); // Giao dịch cũ = null

            _mockTuitionRepo.Setup(r => r.GetTransactionStudentAndInvoiceId(invoiceId, studentId)).ReturnsAsync((Transaction)null);

            var mockFile = CreateMockFile("proof.png", 1024, "image/png");
            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>())).ReturnsAsync("url.png");

            var invoiceInfo = new Invoice { Class = new Class { TeacherId = Guid.NewGuid() } };
            _mockTuitionRepo.Setup(r => r.GetInvoicesWithClassAsync(invoiceId)).ReturnsAsync(invoiceInfo);

            var result = await _service.UploadPaymentProofAsync(invoiceId, new ProofUploadDto { ProofImage = mockFile.Object });

            Assert.That(result, Is.True);
            _mockTuitionRepo.Verify(r => r.AddTransactionAsync(It.Is<Transaction>(t => t.Status == "Pending" && t.ProofImageUrl == "url.png")), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Giao dịch học phí mới", It.IsAny<string>(), It.IsAny<string>(), "Invoice"), Times.Once);
        }

        [Test]
        public async Task UploadPaymentProofAsync_ReuploadFailed_UpdatesTransactionAndDeletesOld()
        {
            var invoiceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var invoice = new Invoice { Amount = 100000, Status = "Pending" };
            var failedTrans = new Transaction { Status = "Failed", ProofImageUrl = "old_url.png" };

            _mockTuitionRepo.Setup(r => r.GetInvoiceDetailAsync(invoiceId, studentId))
                            .ReturnsAsync((invoice, failedTrans, new List<Attendance>()));

            _mockTuitionRepo.Setup(r => r.GetTransactionStudentAndInvoiceId(invoiceId, studentId)).ReturnsAsync(failedTrans);

            var mockFile = CreateMockFile("new.png", 1024, "image/png");
            _mockStorageService.Setup(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>())).ReturnsAsync("new_url.png");

            var invoiceInfo = new Invoice { Class = new Class { TeacherId = Guid.NewGuid() } };
            _mockTuitionRepo.Setup(r => r.GetInvoicesWithClassAsync(invoiceId)).ReturnsAsync(invoiceInfo);

            var result = await _service.UploadPaymentProofAsync(invoiceId, new ProofUploadDto { ProofImage = mockFile.Object });

            Assert.That(result, Is.True);
            Assert.That(failedTrans.Status, Is.EqualTo("Pending"));
            Assert.That(failedTrans.ProofImageUrl, Is.EqualTo("new_url.png"));

            _mockTuitionRepo.Verify(r => r.UpdateTransactionAsync(failedTrans), Times.Once);
            _mockStorageService.Verify(s => s.DeleteFileByUrlAsync("old_url.png"), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), null, "Nộp lại minh chứng học phí", It.IsAny<string>(), It.IsAny<string>(), "Invoice"), Times.Once);
        }

        #endregion

        #region 5. GetMyTransactionsAsync Tests

        [Test]
        public void GetMyTransactionsAsync_NoTransactions_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);
            _mockTuitionRepo.Setup(r => r.GetTransactionsByStudentIdAsync(studentId, null)).ReturnsAsync((List<Transaction>)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetMyTransactionsAsync(null));
            Assert.That(ex.Message, Does.Contain("chưa có giao dịch nào"));
        }

        [Test]
        public async Task GetMyTransactionsAsync_Valid_ReturnsMappedListWithFallbackDescription()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var transactions = new List<Transaction>
            {
                new Transaction { Invoice = new Invoice { Description = "Custom Desc" } },
                new Transaction { Invoice = new Invoice { Description = "", PeriodMonth = 10, PeriodYear = 2024 } } // Thử fallback chuỗi
            };
            _mockTuitionRepo.Setup(r => r.GetTransactionsByStudentIdAsync(studentId, null)).ReturnsAsync(transactions);

            var result = await _service.GetMyTransactionsAsync(null);

            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result[0].InvoiceContent, Is.EqualTo("Custom Desc"));
            Assert.That(result[1].InvoiceContent, Is.EqualTo("Học phí tháng 10/2024"));
        }

        #endregion

        #region 6. GetTransactionByIdAsync Tests

        [Test]
        public void GetTransactionByIdAsync_NotFound_ThrowsException()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);
            _mockTuitionRepo.Setup(r => r.GetTransactionDetailAsync(It.IsAny<Guid>(), studentId)).ReturnsAsync((Transaction)null);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _service.GetTransactionByIdAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Không tìm thấy giao dịch"));
        }

        [Test]
        public async Task GetTransactionByIdAsync_Valid_ReturnsDto()
        {
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);

            var transaction = new Transaction { TransactionId = Guid.NewGuid(), AmountPaid = 500000, Invoice = new Invoice { Description = "Test" } };
            _mockTuitionRepo.Setup(r => r.GetTransactionDetailAsync(transaction.TransactionId, studentId)).ReturnsAsync(transaction);

            var result = await _service.GetTransactionByIdAsync(transaction.TransactionId);

            Assert.That(result.TransactionId, Is.EqualTo(transaction.TransactionId));
            Assert.That(result.InvoiceContent, Is.EqualTo("Test"));
            Assert.That(result.AmountPaid, Is.EqualTo(500000));
        }

        #endregion
    }
}