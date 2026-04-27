using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Gradebook.DTOs;
using EMS.Application.Features.Gradebook.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;

namespace EMS.Application.Tests.Features.Gradebook
{
    [TestFixture]
    public class GradebookServiceTests
    {
        private Mock<IGradeCategoryRepository> _mockGradeCategoryRepo;
        private Mock<IAssignmentRepository> _mockAssignmentRepo;
        private Mock<ISubmissionRepository> _mockSubmissionRepo;
        private Mock<IClassRepository> _mockClassRepo;
        private Mock<ICurrentUserService> _mockCurrentUser;
        private GradebookService _service;

        [SetUp]
        public void Setup()
        {
            _mockGradeCategoryRepo = new Mock<IGradeCategoryRepository>();
            _mockAssignmentRepo = new Mock<IAssignmentRepository>();
            _mockSubmissionRepo = new Mock<ISubmissionRepository>();
            _mockClassRepo = new Mock<IClassRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();

            _service = new GradebookService(
                _mockGradeCategoryRepo.Object,
                _mockAssignmentRepo.Object,
                _mockSubmissionRepo.Object,
                _mockClassRepo.Object,
                _mockCurrentUser.Object
            );
        }

        // Helper Method để vượt qua private hàm RequireTeacherAccessAsync
        private void SetupRequireTeacherAccess(Guid classId, Guid teacherId, bool isTA = false)
        {
            _mockCurrentUser.Setup(c => c.UserId).Returns(teacherId);
            _mockCurrentUser.Setup(c => c.Role).Returns(isTA ? "TA" : "Teacher");

            var classroom = new Class { ClassId = classId, TeacherId = isTA ? Guid.NewGuid() : teacherId };
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            if (isTA)
            {
                _mockClassRepo.Setup(r => r.GetTAsByClassIdAsync(classId))
                              .ReturnsAsync(new List<ClassTum> { new ClassTum { Taid = teacherId } });
            }
            else
            {
                _mockClassRepo.Setup(r => r.GetTAsByClassIdAsync(classId)).ReturnsAsync(new List<ClassTum>());
            }
        }

        #region 1. GetGradeCategoriesByClassAsync Tests

        [Test]
        public void GetGradeCategoriesByClassAsync_ClassNotFound_ThrowsException()
        {
            _mockClassRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Class)null);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.GetGradeCategoriesByClassAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Is.EqualTo("Class not found."));
        }

        [Test]
        public async Task GetGradeCategoriesByClassAsync_ValidClass_ReturnsOrderedCategories()
        {
            var classId = Guid.NewGuid();
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Class());

            var categories = new List<GradeCategory>
            {
                new GradeCategory { Name = "Midterm", Weight = 30 },
                new GradeCategory { Name = "Attendance", Weight = 10 },
                new GradeCategory { Name = "Final", Weight = 60 }
            };
            _mockGradeCategoryRepo.Setup(r => r.GetByClassIdAsync(classId)).ReturnsAsync(categories);

            var result = await _service.GetGradeCategoriesByClassAsync(classId);
            var listResult = result.ToList();

            Assert.That(listResult.Count, Is.EqualTo(3));
            Assert.That(listResult[0].Name, Is.EqualTo("Attendance")); // Đã sort Alphabet A-M-F
            Assert.That(listResult[1].Name, Is.EqualTo("Final"));
            Assert.That(listResult[2].Name, Is.EqualTo("Midterm"));
        }

        #endregion

        #region 2. AddGradeCategoryAsync Tests

        [Test]
        public void AddGradeCategoryAsync_WeightExceeds100_ThrowsException()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var existingCategories = new List<GradeCategory> { new GradeCategory { Weight = 80 } };
            _mockGradeCategoryRepo.Setup(r => r.GetByClassIdAsync(classId)).ReturnsAsync(existingCategories);

            var request = new CreateGradeCategoryDto { Name = "New Cat", Weight = 30 };

            var ex = Assert.ThrowsAsync<Exception>(() => _service.AddGradeCategoryAsync(classId, request));
            Assert.That(ex.Message, Does.Contain("Total weight would exceed 100%"));
        }

        [Test]
        public async Task AddGradeCategoryAsync_ValidWeight_AddsCategory()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var existingCategories = new List<GradeCategory> { new GradeCategory { Weight = 50 } };
            _mockGradeCategoryRepo.Setup(r => r.GetByClassIdAsync(classId)).ReturnsAsync(existingCategories);

            var request = new CreateGradeCategoryDto { Name = "New Cat", Weight = 50 };

            var resultId = await _service.AddGradeCategoryAsync(classId, request);

            Assert.That(resultId, Is.Not.EqualTo(Guid.Empty));
            _mockGradeCategoryRepo.Verify(r => r.AddAsync(It.Is<GradeCategory>(c => c.Name == "New Cat" && c.Weight == 50)), Times.Once);
        }

        #endregion

        #region 3. UpdateGradeCategoryAsync Tests

        [Test]
        public void UpdateGradeCategoryAsync_CategoryNotFound_ThrowsException()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            _mockGradeCategoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GradeCategory)null);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.UpdateGradeCategoryAsync(classId, new UpdateGradeCategoryDto()));
            Assert.That(ex.Message, Is.EqualTo("Grade Category not found in this class."));
        }

        [Test]
        public void UpdateGradeCategoryAsync_WeightExceeds100_ThrowsException()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var catId = Guid.NewGuid();
            var categoryToUpdate = new GradeCategory { GradeCategoryId = catId, ClassId = classId, Weight = 20 };

            var existingCategories = new List<GradeCategory>
            {
                categoryToUpdate,
                new GradeCategory { GradeCategoryId = Guid.NewGuid(), Weight = 70 } // Các category khác chiếm 70%
            };

            _mockGradeCategoryRepo.Setup(r => r.GetByIdAsync(catId)).ReturnsAsync(categoryToUpdate);
            _mockGradeCategoryRepo.Setup(r => r.GetByClassIdAsync(classId)).ReturnsAsync(existingCategories);

            var request = new UpdateGradeCategoryDto { GradeCategoryId = catId, Weight = 40 }; // Tổng sẽ là 70 + 40 = 110

            var ex = Assert.ThrowsAsync<Exception>(() => _service.UpdateGradeCategoryAsync(classId, request));
            Assert.That(ex.Message, Does.Contain("Total weight would exceed 100%"));
        }

        [Test]
        public async Task UpdateGradeCategoryAsync_ValidWeight_UpdatesSuccessfully()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var catId = Guid.NewGuid();
            var categoryToUpdate = new GradeCategory { GradeCategoryId = catId, ClassId = classId, Weight = 20, Name = "Old" };

            _mockGradeCategoryRepo.Setup(r => r.GetByIdAsync(catId)).ReturnsAsync(categoryToUpdate);
            _mockGradeCategoryRepo.Setup(r => r.GetByClassIdAsync(classId)).ReturnsAsync(new List<GradeCategory> { categoryToUpdate });

            var request = new UpdateGradeCategoryDto { GradeCategoryId = catId, Weight = 30, Name = "New" };

            await _service.UpdateGradeCategoryAsync(classId, request);

            Assert.That(categoryToUpdate.Name, Is.EqualTo("New"));
            Assert.That(categoryToUpdate.Weight, Is.EqualTo(30));
            _mockGradeCategoryRepo.Verify(r => r.UpdateAsync(categoryToUpdate), Times.Once);
        }

        #endregion

        #region 4. BulkUpdateCategoriesAsync Tests

        [Test]
        public void BulkUpdateCategoriesAsync_TotalExceeds100_ThrowsException()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var request = new BulkUpdateGradeCategoryDto
            {
                Categories = new List<UpdateGradeCategoryDto>
                {
                    new UpdateGradeCategoryDto { Weight = 60 },
                    new UpdateGradeCategoryDto { Weight = 50 } // Tổng 110
                }
            };

            var ex = Assert.ThrowsAsync<Exception>(() => _service.BulkUpdateCategoriesAsync(classId, request));
            Assert.That(ex.Message, Does.Contain("cannot exceed 100"));
        }

        [Test]
        public async Task BulkUpdateCategoriesAsync_ValidTotal_UpdatesSuccessfully()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var catId1 = Guid.NewGuid();
            var catId2 = Guid.NewGuid();

            var existingCategories = new List<GradeCategory>
            {
                new GradeCategory { GradeCategoryId = catId1, Weight = 20 },
                new GradeCategory { GradeCategoryId = catId2, Weight = 30 }
            };

            _mockGradeCategoryRepo.Setup(r => r.GetByClassIdAsync(classId)).ReturnsAsync(existingCategories);

            var request = new BulkUpdateGradeCategoryDto
            {
                Categories = new List<UpdateGradeCategoryDto>
                {
                    new UpdateGradeCategoryDto { GradeCategoryId = catId1, Weight = 40, Name = "Cat 1" },
                    new UpdateGradeCategoryDto { GradeCategoryId = catId2, Weight = 60, Name = "Cat 2" }
                }
            };

            await _service.BulkUpdateCategoriesAsync(classId, request);

            Assert.That(existingCategories[0].Weight, Is.EqualTo(40));
            Assert.That(existingCategories[1].Weight, Is.EqualTo(60));
            _mockGradeCategoryRepo.Verify(r => r.UpdateWeightsAsync(existingCategories), Times.Once);
        }

        #endregion

        #region 5. DeleteGradeCategoryAsync Tests

        [Test]
        public void DeleteGradeCategoryAsync_CategoryNotFound_ThrowsException()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            _mockGradeCategoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GradeCategory)null);

            var ex = Assert.ThrowsAsync<Exception>(() => _service.DeleteGradeCategoryAsync(classId, Guid.NewGuid()));
            Assert.That(ex.Message, Is.EqualTo("Grade Category not found."));
        }

        [Test]
        public async Task DeleteGradeCategoryAsync_ValidCategory_DeletesSuccessfully()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var category = new GradeCategory { ClassId = classId };
            _mockGradeCategoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(category);

            await _service.DeleteGradeCategoryAsync(classId, Guid.NewGuid());

            _mockGradeCategoryRepo.Verify(r => r.DeleteAsync(category), Times.Once);
        }

        #endregion

        #region 6. GetClassGradebookAsync Tests

        [Test]
        public async Task GetClassGradebookAsync_WithData_CalculatesCorrectFinalAverage()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid(); 

            SetupRequireTeacherAccess(classId, teacherId);

            _mockClassRepo.Setup(r => r.GetByIdAsync(classId))
                          .ReturnsAsync(new Class
                          {
                              ClassId = classId,
                              ClassName = "Test Class",
                              TeacherId = teacherId
                          });

            var studentId = Guid.NewGuid();
            _mockClassRepo.Setup(r => r.GetClassMemberAsync(classId))
                          .ReturnsAsync(new List<ClassEnrollment> { new ClassEnrollment { StudentId = studentId, Student = new Student { FullName = "John Doe" } } });

            var catId1 = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            _mockAssignmentRepo.Setup(r => r.GetByClassIdAsync(classId))
                               .ReturnsAsync(new List<Assignment> {
                           new Assignment { AssignmentId = assignmentId, GradeCategoryId = catId1, GradeCategory = new GradeCategory { Weight = 100 } }
                               });

            _mockSubmissionRepo.Setup(r => r.GetSubmissionsForClassAsync(classId))
                               .ReturnsAsync(new List<Submission> {
                           new Submission { StudentId = studentId, AssignmentId = assignmentId, Grade = 8.5m }
                               });

            // Act
            var result = await _service.GetClassGradebookAsync(classId);

            // Assert
            Assert.That(result.ClassName, Is.EqualTo("Test Class"));
            Assert.That(result.StudentRows.Count, Is.EqualTo(1));
            Assert.That(result.StudentRows[0].FinalAverage, Is.EqualTo(8.5m));
            Assert.That(result.StudentRows[0].Grades[0].Grade, Is.EqualTo(8.5m));
        }

        #endregion

        #region 8. SaveBulkGradesAsync Tests

        [Test]
        public async Task SaveBulkGradesAsync_EmptyRequest_ReturnsEarly()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            await _service.SaveBulkGradesAsync(classId, new BulkSaveGradesRequest { ChangedGrades = new List<GradeCellDto>() });

            _mockSubmissionRepo.Verify(r => r.UpdateRangeAsync(It.IsAny<List<Submission>>()), Times.Never);
            _mockSubmissionRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<Submission>>()), Times.Never);
        }

        [Test]
        public async Task SaveBulkGradesAsync_MixedChanges_UpdatesAndInsertsCorrectly()
        {
            var classId = Guid.NewGuid();
            SetupRequireTeacherAccess(classId, Guid.NewGuid());

            var assignmentId = Guid.NewGuid();
            var stdUpdateId = Guid.NewGuid();
            var stdInsertId = Guid.NewGuid();

            var existingSubmissions = new List<Submission>
            {
                new Submission { StudentId = stdUpdateId, AssignmentId = assignmentId, Grade = 5m }
            };
            _mockSubmissionRepo.Setup(r => r.GetByAssignmentIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(existingSubmissions);

            var request = new BulkSaveGradesRequest
            {
                ChangedGrades = new List<GradeCellDto>
                {
                    new GradeCellDto { StudentId = stdUpdateId, AssignmentId = assignmentId, Grade = 9m }, // Update
                    new GradeCellDto { StudentId = stdInsertId, AssignmentId = assignmentId, Grade = 7m }  // Insert
                }
            };

            await _service.SaveBulkGradesAsync(classId, request);

            _mockSubmissionRepo.Verify(r => r.UpdateRangeAsync(It.Is<List<Submission>>(l => l.Count == 1 && l[0].Grade == 9m)), Times.Once);
            _mockSubmissionRepo.Verify(r => r.AddRangeAsync(It.Is<List<Submission>>(l => l.Count == 1 && l[0].Grade == 7m)), Times.Once);
        }

        #endregion

        #region 9. GetStudentGradeReportAsync Tests

        [Test]
        public void GetStudentGradeReportAsync_StudentIdNull_ThrowsUnauthorized()
        {
            _mockCurrentUser.Setup(c => c.StudentId).Returns((Guid?)null);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetStudentGradeReportAsync(Guid.NewGuid()));
            Assert.That(ex.Message, Does.Contain("Student ID is missing"));
        }

        [Test]
        public void GetStudentGradeReportAsync_ClassNotEnrolled_ThrowsException()
        {
            var classId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Class());
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(classId, studentId)).ReturnsAsync(false); // Chưa enroll

            var ex = Assert.ThrowsAsync<Exception>(() => _service.GetStudentGradeReportAsync(classId));
            Assert.That(ex.Message, Does.Contain("Bạn chưa tham gia vào lớp học này."));
        }

        [Test]
        public async Task GetStudentGradeReportAsync_ValidStudent_ReturnsCorrectReport()
        {
            var classId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.StudentId).Returns(studentId);
            _mockClassRepo.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Class());
            _mockClassRepo.Setup(r => r.IsStudentAlreadyEnrolledAsync(classId, studentId)).ReturnsAsync(true);

            var categories = new List<GradeCategory>
            {
                new GradeCategory
                {
                    Name = "Test Category", Weight = 100,
                    Assignments = new List<Assignment>
                    {
                        new Assignment
                        {
                            Title = "Assignment 1",
                            Submissions = new List<Submission>
                            {
                                new Submission { Grade = 9.0m, Status = "Graded", SubmissionFeedbacks = new List<SubmissionFeedback>() }
                            }
                        }
                    }
                }
            };
            _mockGradeCategoryRepo.Setup(r => r.GetStudentGradeDetailsAsync(classId, studentId)).ReturnsAsync(categories);

            var result = await _service.GetStudentGradeReportAsync(classId);

            Assert.That(result.CurrentAverageScore, Is.EqualTo(9.0m));
            Assert.That(result.GradeReportTable.Count, Is.EqualTo(1));
            Assert.That(result.GradeReportTable[0].CategoryScore, Is.EqualTo(9.0m));
        }

        #endregion
    }
}