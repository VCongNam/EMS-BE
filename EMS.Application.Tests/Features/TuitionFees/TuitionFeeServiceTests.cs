using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Application.Features.TuitionFees.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.TuitionFees
{
    [TestFixture]
    public class TuitionFeeServiceTests
    {
        private Mock<ITuitionFeeRepository> _mockTuitionFeeRepo;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILogger<TuitionFeeService>> _mockLogger;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private TuitionFeeService _service;

        [SetUp]
        public void Setup()
        {
            _mockTuitionFeeRepo = new Mock<ITuitionFeeRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<TuitionFeeService>>();
            _mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new TuitionFeeService(
                _mockTuitionFeeRepo.Object,
                _mockNotificationService.Object,
                _mockLogger.Object,
                _mockCurrentUser.Object
            );
        }

        #region 1. GetInvoicesPreviewAsync Tests

        [Test]
        public void GetInvoicesPreviewAsync_NotTeacherOwnsClass_ThrowsUnauthorized()
        {
            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.GetInvoicesPreviewAsync(Guid.NewGuid(), 1, 2024, Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("không có quyền thao tác"));
        }

        [Test]
        public void GetInvoicesPreviewAsync_InvoicesAlreadyExist_ThrowsBadRequest()
        {
            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
            _mockTuitionFeeRepo.Setup(r => r.HasInvoicesForPeriodAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _service.GetInvoicesPreviewAsync(Guid.NewGuid(), 1, 2024, Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("đã phát hành hóa đơn rồi"));
        }

        [Test]
        public void GetInvoicesPreviewAsync_EarlyBilling_ThrowsBadRequest()
        {
            // Test cố tình xuất hóa đơn của tháng hiện tại (Chưa hết tháng)
            var now = DateTime.UtcNow;
            var classId = Guid.NewGuid();

            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(true);
            _mockTuitionFeeRepo.Setup(r => r.HasInvoicesForPeriodAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(false);

            _mockTuitionFeeRepo.Setup(r => r.GetClassByIdAsync(classId)).ReturnsAsync(new Class
            {
                StartDate = DateOnly.FromDateTime(now.AddYears(-1)),
                EndDate = DateOnly.FromDateTime(now.AddYears(1))
            });

            var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _service.GetInvoicesPreviewAsync(classId, now.Month, now.Year, Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Chưa thể phát hành hóa đơn kỳ"));
        }

        [Test]
        public async Task GetInvoicesPreviewAsync_ValidRequest_ReturnsCalculatedPreviewSorted()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            // Lùi về 2 tháng trước để pass logic kiểm tra "Early Billing"
            var targetDate = DateTime.UtcNow.AddMonths(-2);
            int month = targetDate.Month;
            int year = targetDate.Year;

            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockTuitionFeeRepo.Setup(r => r.HasInvoicesForPeriodAsync(classId, month, year)).ReturnsAsync(false);

            _mockTuitionFeeRepo.Setup(r => r.GetClassByIdAsync(classId)).ReturnsAsync(new Class
            {
                TuitionFee = 100000,
                StartDate = DateOnly.FromDateTime(targetDate.AddYears(-1)),
                EndDate = DateOnly.FromDateTime(targetDate.AddYears(1))
            });

            var activeStudentId = Guid.NewGuid();
            var droppedStudentId = Guid.NewGuid();

            var studentsToBill = new List<ClassEnrollment>
    {
        new ClassEnrollment { StudentId = droppedStudentId, Status = "Dropped", Student = new Student { FullName = "Anh Dropped" } },
        new ClassEnrollment { StudentId = activeStudentId, Status = "Active", Student = new Student { FullName = "Bình Active" } }
    };
            _mockTuitionFeeRepo.Setup(r => r.GetStudentsForBillingAsync(classId, month, year)).ReturnsAsync(studentsToBill);

            var attendanceDict = new Dictionary<Guid, (int Attended, int Excused, int Unexcused)>
    {
        { activeStudentId, (Attended: 5, Excused: 0, Unexcused: 0) } // Active đi học 5 buổi
    };
            _mockTuitionFeeRepo.Setup(r => r.GetDetailedAttendanceCountsAsync(classId, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(attendanceDict);
            _mockTuitionFeeRepo.Setup(r => r.CountScheduledSessionsAsync(classId, month, year)).ReturnsAsync(5);

            var result = await _service.GetInvoicesPreviewAsync(classId, month, year, teacherId);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].StudentId, Is.EqualTo(activeStudentId)); // Active lên đầu tiên
            Assert.That(result[0].Amount, Is.EqualTo(500000)); // 5 buổi * 100k
            Assert.That(result[1].StudentId, Is.EqualTo(droppedStudentId));
            Assert.That(result[1].Amount, Is.EqualTo(0)); // Không có điểm danh -> 0 buổi
        }
        #endregion

        #region 2. ConfirmAndGenerateInvoicesAsync Tests

        [Test]
        public void ConfirmAndGenerateInvoicesAsync_AlreadyIssued_ThrowsBadRequest()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var dto = new ConfirmInvoicesDto { PeriodMonth = 5, PeriodYear = 2024 };

            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockTuitionFeeRepo.Setup(r => r.HasInvoicesForPeriodAsync(classId, dto.PeriodMonth, dto.PeriodYear)).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _service.ConfirmAndGenerateInvoicesAsync(classId, dto, teacherId));
            Assert.That(ex.Message, Does.Contain("không thể phát hành thêm"));
        }

        [Test]
        public void ConfirmAndGenerateInvoicesAsync_EmptyValidInvoices_ThrowsBadRequest()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var dto = new ConfirmInvoicesDto
            {
                PeriodMonth = 5,
                PeriodYear = 2024,
                Invoices = new List<ConfirmInvoiceItemDto> { new ConfirmInvoiceItemDto { AttendedSessions = 0 } } // Session = 0 -> bị skip
            };

            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockTuitionFeeRepo.Setup(r => r.HasInvoicesForPeriodAsync(classId, dto.PeriodMonth, dto.PeriodYear)).ReturnsAsync(false);
            _mockTuitionFeeRepo.Setup(r => r.GetClassByIdAsync(classId)).ReturnsAsync(new Class { TuitionFee = 100000 });

            var ex = Assert.ThrowsAsync<BadRequestException>(async () =>
                await _service.ConfirmAndGenerateInvoicesAsync(classId, dto, teacherId));
            Assert.That(ex.Message, Does.Contain("Không có hóa đơn nào hợp lệ để tạo"));
        }

        [Test]
        public async Task ConfirmAndGenerateInvoicesAsync_ValidData_GeneratesAndNotifies()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var dto = new ConfirmInvoicesDto
            {
                PeriodMonth = 5,
                PeriodYear = 2024,
                DueDate = DateTime.UtcNow.AddDays(7),
                Invoices = new List<ConfirmInvoiceItemDto> { new ConfirmInvoiceItemDto { StudentId = studentId, AttendedSessions = 4 } }
            };

            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(classId, teacherId)).ReturnsAsync(true);
            _mockTuitionFeeRepo.Setup(r => r.GetClassByIdAsync(classId)).ReturnsAsync(new Class { TuitionFee = 100000 });
            _mockTuitionFeeRepo.Setup(r => r.AddInvoicesWithEnrollmentsAsync(It.IsAny<List<Invoice>>(), null, classId, dto.PeriodMonth, dto.PeriodYear)).ReturnsAsync(true);

            // Mock Target Notification
            var targetAccountId = Guid.NewGuid();
            _mockNotificationService.Setup(n => n.GetStudentTargetsAsync(classId))
                                    .ReturnsAsync(new List<(Guid AccId, Guid? StdId)> { (targetAccountId, studentId) });

            await _service.ConfirmAndGenerateInvoicesAsync(classId, dto, teacherId);

            _mockTuitionFeeRepo.Verify(r => r.AddInvoicesWithEnrollmentsAsync(It.Is<List<Invoice>>(l => l.Count == 1 && l[0].Amount == 400000), null, classId, dto.PeriodMonth, dto.PeriodYear), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(targetAccountId, studentId, "Thông báo học phí", It.IsAny<string>(), It.IsAny<string>(), "Invoice"), Times.Once);
        }

        #endregion

        #region 3. ExtendInvoiceDueDateAsync Tests

        [Test]
        public void ExtendInvoiceDueDateAsync_InvoiceNotFound_ThrowsNotFound()
        {
            _mockTuitionFeeRepo.Setup(r => r.GetInvoiceByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Invoice)null);
            var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _service.ExtendInvoiceDueDateAsync(Guid.NewGuid(), 5, Guid.NewGuid()));
        }

        [Test]
        public void ExtendInvoiceDueDateAsync_Unauthorized_ThrowsUnauthorized()
        {
            var invoice = new Invoice { Class = new Class { TeacherId = Guid.NewGuid() } };
            _mockTuitionFeeRepo.Setup(r => r.GetInvoiceByIdAsync(It.IsAny<Guid>())).ReturnsAsync(invoice);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.ExtendInvoiceDueDateAsync(Guid.NewGuid(), 5, Guid.NewGuid()));
        }

        [Test]
        public async Task ExtendInvoiceDueDateAsync_Valid_UpdatesDueDate()
        {
            var teacherId = Guid.NewGuid();
            var originalDueDate = DateTime.UtcNow;
            var invoice = new Invoice { DueDate = originalDueDate, Class = new Class { TeacherId = teacherId } };

            _mockTuitionFeeRepo.Setup(r => r.GetInvoiceByIdAsync(It.IsAny<Guid>())).ReturnsAsync(invoice);

            await _service.ExtendInvoiceDueDateAsync(Guid.NewGuid(), 5, teacherId);

            Assert.That(invoice.DueDate, Is.EqualTo(originalDueDate.AddDays(5)));
            _mockTuitionFeeRepo.Verify(r => r.UpdateInvoiceAsync(invoice), Times.Once);
        }

        #endregion

        #region 4. ExtendClassInvoicesDueDateAsync Tests

        [Test]
        public void ExtendClassInvoicesDueDateAsync_Unauthorized_ThrowsUnauthorized()
        {
            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.ExtendClassInvoicesDueDateAsync(Guid.NewGuid(), new ExtendClassInvoicesDto(), Guid.NewGuid()));
        }

        [Test]
        public async Task ExtendClassInvoicesDueDateAsync_Valid_ExtendsOnlyUnpaidInvoices()
        {
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var dto = new ExtendClassInvoicesDto { AdditionalDays = 5 };

            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(classId, teacherId)).ReturnsAsync(true);

            var oldDate = DateTime.UtcNow;
            var invoices = new List<Invoice>
            {
                new Invoice { Status = "Pending", DueDate = oldDate },
                new Invoice { Status = "Paid", DueDate = oldDate } // Không được cộng thêm ngày
            };
            _mockTuitionFeeRepo.Setup(r => r.GetInvoicesByClassAndPeriodAsync(classId, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(invoices);

            await _service.ExtendClassInvoicesDueDateAsync(classId, dto, teacherId);

            Assert.That(invoices[0].DueDate, Is.EqualTo(oldDate.AddDays(5)));
            Assert.That(invoices[1].DueDate, Is.EqualTo(oldDate));
            _mockTuitionFeeRepo.Verify(r => r.UpdateInvoicesAsync(invoices), Times.Once);
        }

        #endregion

        #region 5. UpdateClassFeeAsync Tests

        [Test]
        public void UpdateClassFeeAsync_Unauthorized_ThrowsUnauthorized()
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _service.UpdateClassFeeAsync(Guid.NewGuid(), new UpdateClassFeeConfigDto()));
            Assert.That(ex.Message, Does.Contain("không có quyền sửa"));
        }

        [Test]
        public async Task UpdateClassFeeAsync_Valid_UpdatesConfig()
        {
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockTuitionFeeRepo.Setup(r => r.IsTeacherOwnsClassAsync(classId, teacherId)).ReturnsAsync(true);

            await _service.UpdateClassFeeAsync(classId, new UpdateClassFeeConfigDto { TuitionFee = 150000, PaymentDeadlineDays = 5 });

            _mockTuitionFeeRepo.Verify(r => r.UpdateClassFeeConfigAsync(classId, "Postpaid", 150000, 5), Times.Once);
        }

        #endregion

        #region 6. ReviewTransactionAsync Tests

        [Test]
        public void ReviewTransactionAsync_NotFound_ThrowsKeyNotFound()
        {
            _mockTuitionFeeRepo.Setup(r => r.GetTransactionWithInvoiceAsync(It.IsAny<Guid>())).ReturnsAsync((Transaction)null);
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.ReviewTransactionAsync(Guid.NewGuid(), true, Guid.NewGuid(), null));
        }

        [Test]
        public void ReviewTransactionAsync_AlreadyProcessed_ThrowsInvalidOperation()
        {
            var trans = new Transaction { Status = "Successful" }; // Đã xử lý
            _mockTuitionFeeRepo.Setup(r => r.GetTransactionWithInvoiceAsync(It.IsAny<Guid>())).ReturnsAsync(trans);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.ReviewTransactionAsync(Guid.NewGuid(), true, Guid.NewGuid(), null));
            Assert.That(ex.Message, Does.Contain("đã được xử lý trước đó"));
        }

        [Test]
        public void ReviewTransactionAsync_InsufficientAmount_ThrowsInvalidOperation()
        {
            var trans = new Transaction { Status = "Pending", AmountPaid = 50000, Invoice = new Invoice { Amount = 100000 } };
            _mockTuitionFeeRepo.Setup(r => r.GetTransactionWithInvoiceAsync(It.IsAny<Guid>())).ReturnsAsync(trans);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.ReviewTransactionAsync(Guid.NewGuid(), true, Guid.NewGuid(), null));
            Assert.That(ex.Message, Does.Contain("không đủ so với hóa đơn"));
        }

        [Test]
        public async Task ReviewTransactionAsync_Approve_UpdatesAndNotifies()
        {
            var studentId = Guid.NewGuid();
            var trans = new Transaction
            {
                Status = "Pending",
                AmountPaid = 100000,
                Invoice = new Invoice { Amount = 100000, StudentId = studentId, Class = new Class { ClassName = "Math" }, Description = "Desc" }
            };
            _mockTuitionFeeRepo.Setup(r => r.GetTransactionWithInvoiceAsync(It.IsAny<Guid>())).ReturnsAsync(trans);
            _mockNotificationService.Setup(n => n.GetAccountIdByStudentIdAsync(studentId)).ReturnsAsync(Guid.NewGuid());

            await _service.ReviewTransactionAsync(Guid.NewGuid(), true, Guid.NewGuid(), null);

            Assert.That(trans.Status, Is.EqualTo("Successful"));
            Assert.That(trans.Invoice.Status, Is.EqualTo("Paid"));
            Assert.That(trans.Invoice.Description, Does.Contain("[Duyệt tay"));
            _mockTuitionFeeRepo.Verify(r => r.UpdateTransactionStatusAsync(trans, trans.Invoice), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), studentId, "Thanh toán thành công", It.IsAny<string>(), It.IsAny<string>(), "Invoice"), Times.Once);
        }

        [Test]
        public async Task ReviewTransactionAsync_Reject_UpdatesToFailedAndNotifies()
        {
            var studentId = Guid.NewGuid();
            var trans = new Transaction
            {
                Status = "Pending",
                AmountPaid = 100000,
                Invoice = new Invoice { Status = "Pending", Amount = 100000, StudentId = studentId, Class = new Class { ClassName = "Math" }, Description = "Desc" }
            };
            _mockTuitionFeeRepo.Setup(r => r.GetTransactionWithInvoiceAsync(It.IsAny<Guid>())).ReturnsAsync(trans);
            _mockNotificationService.Setup(n => n.GetAccountIdByStudentIdAsync(studentId)).ReturnsAsync(Guid.NewGuid());

            await _service.ReviewTransactionAsync(Guid.NewGuid(), false, Guid.NewGuid(), "Mờ quá");

            Assert.That(trans.Status, Is.EqualTo("Failed"));
            Assert.That(trans.Note, Is.EqualTo("Mờ quá"));
            Assert.That(trans.Invoice.Status, Is.EqualTo("Pending")); // Vẫn giữ nguyên trạng thái hóa đơn
            Assert.That(trans.Invoice.Description, Does.Contain("[Từ chối"));
            _mockTuitionFeeRepo.Verify(r => r.UpdateTransactionStatusAsync(trans, trans.Invoice), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<Guid>(), studentId, "Giao dịch bị từ chối", It.Is<string>(s => s.Contains("Lý do: Mờ quá")), It.IsAny<string>(), "Invoice"), Times.Once);
        }

        #endregion
    }
}