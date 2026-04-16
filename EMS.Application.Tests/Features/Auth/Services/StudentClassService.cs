using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using EMS.Application.Features.Classes.Services;
using EMS.Application.Common.Interfaces;
using EMS.Domain.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Domain.Entities;

namespace EMS.Application.Tests.Features.Classes.Services
{
    [TestFixture]
    public class StudentClassServiceTests
    {
        private Mock<ICurrentUserService> _mockCurrentUser;
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<IAssignmentRepository> _mockAssignmentRepo;
        private StudentClassService _service;

        [SetUp]
        public void SetUp()
        {
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockClassRepo = new Mock<IClassRepository>();
            _mockAssignmentRepo = new Mock<IAssignmentRepository>();

            _service = new StudentClassService(
                _mockCurrentUser.Object,
                _mockClassRepo.Object,
                _mockAssignmentRepo.Object
            );
        }

        // 1. Trường hợp: Không có StudentId trong Token (Chưa đăng nhập/Token lỗi)
        [Test]
        public void GetMyClassesAsync_StudentIdMissing_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockCurrentUser.Setup(u => u.StudentId).Returns((Guid?)null);
            var filter = new EnrolledClassFilter { Page = 1, Size = 10 };

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetMyClassesAsync(filter));
        }

        // 2. Trường hợp: Thành công và kiểm tra Mapping dữ liệu + Trạng thái lớp học
        // Chúng ta dùng TestCase để kiểm tra logic tính ClassStatus dựa trên StartDate/EndDate
        [TestCase(-5, 5, "Ongoing")]    // Bắt đầu 5 ngày trước, kết thúc sau 5 ngày -> Đang diễn ra
        [TestCase(2, 10, "Scheduled")]  // Bắt đầu sau 2 ngày nữa -> Sắp diễn ra
        [TestCase(-10, -2, "Completed")] // Kết thúc từ 2 ngày trước -> Đã xong
        public async Task GetMyClassesAsync_ValidData_ReturnsCorrectStatus(int startOffset, int endOffset, string expectedStatus)
        {
            // ARRANGE
            var studentId = Guid.NewGuid();
            var filter = new EnrolledClassFilter { Page = 1, Size = 10 };
            var now = DateOnly.FromDateTime(DateTime.UtcNow);

            _mockCurrentUser.Setup(u => u.StudentId).Returns(studentId);

            // Tạo dữ liệu giả cho ClassEnrollment
            var fakeEnrollments = new List<ClassEnrollment>
            {
                new ClassEnrollment
                {
                    ClassId = Guid.NewGuid(),
                    EnrolledDate = now.AddDays(-1),
                    Status = "Active",
                    Class = new Class
                    {
                        ClassName = "Lớp Toán 10",
                        StartDate = now.AddDays(startOffset), // Thay đổi theo TestCase
                        EndDate = now.AddDays(endOffset),     // Thay đổi theo TestCase
                        Teacher = new Teacher
                        {
                            TeacherNavigation = new Account { FullName = "Thầy Giáo A" }
                        }
                    }
                }
            };

            // Setup Repo trả về Tuple (danh sách, tổng số lượng)
            _mockClassRepo.Setup(r => r.GetClassByStudentIdAsync(studentId, filter.Page, filter.Size))
                          .ReturnsAsync((fakeEnrollments, 1));

            // ACT
            var result = await _service.GetMyClassesAsync(filter);

            // ASSERT
            Assert.Multiple(() => {
                Assert.That(result.Items.Count, Is.EqualTo(1));
                Assert.That(result.Items[0].ClassName, Is.EqualTo("Lớp Toán 10"));
                Assert.That(result.Items[0].ClassStatus, Is.EqualTo(expectedStatus)); // Kiểm tra logic quan trọng nhất
                Assert.That(result.TotalCount, Is.EqualTo(1));
            });
        }

        // 3. Trường hợp: Kiểm tra tính toán phân trang (Pagination)
        [Test]
        public async Task GetMyClassesAsync_Pagination_CalculatesTotalPagesCorrectly()
        {
            // ARRANGE
            var studentId = Guid.NewGuid();
            var filter = new EnrolledClassFilter { Page = 1, Size = 10 }; // Mỗi trang 10 item
            var totalCount = 25; // Tổng 25 item -> Phải ra 3 trang

            _mockCurrentUser.Setup(u => u.StudentId).Returns(studentId);

            // Chỉ cần list rỗng nhưng totalCount = 25
            _mockClassRepo.Setup(r => r.GetClassByStudentIdAsync(studentId, filter.Page, filter.Size))
                          .ReturnsAsync((new List<ClassEnrollment>(), totalCount));

            // ACT
            var result = await _service.GetMyClassesAsync(filter);

            // ASSERT
            Assert.That(result.TotalPages, Is.EqualTo(3)); // 25/10 làm tròn lên là 3
        }
    }
}