using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Gradebook.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EMS.Application.Features.Gradebook.Services
{
    public class GradebookService : IGradebookService
    {
        private readonly IGradeCategoryRepository _gradeCategoryRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;

        public GradebookService(
            IGradeCategoryRepository gradeCategoryRepository,
            IAssignmentRepository assignmentRepository,
            ISubmissionRepository submissionRepository,
            IClassRepository classRepository,
            ICurrentUserService currentUserService)
        {
            _gradeCategoryRepository = gradeCategoryRepository;
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
        }

        private async Task RequireTeacherAccessAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null) throw new NotFoundException("Không tìm thấy lớp học.");
            var tas = await _classRepository.GetTAsByClassIdAsync(classId);
            bool isAssigned = false;
            if (_currentUserService.Role == "TA")
            {
                isAssigned = tas.Any(ta => ta.Taid == _currentUserService.UserId);
            }

            if (classroom.TeacherId != _currentUserService.UserId && !isAssigned)
                throw new ForbiddenAccessException("Bạn không có quyền truy cập bảng điểm của lớp này.");
        }

        public async Task<IEnumerable<GradeCategoryDto>> GetGradeCategoriesByClassAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null) throw new NotFoundException("Không tìm thấy lớp học.");

            var categories = await _gradeCategoryRepository.GetByClassIdAsync(classId);

            return categories.Select(c => new GradeCategoryDto
            {
                GradeCategoryId = c.GradeCategoryId,
                ClassId = c.ClassId,
                Name = c.Name,
                Weight = c.Weight
            }).OrderBy(c => c.Name).ToList();
        }

        public async Task<Guid> AddGradeCategoryAsync(Guid classId, CreateGradeCategoryDto request)
        {
            await RequireTeacherAccessAsync(classId);

            var existingCategories = await _gradeCategoryRepository.GetByClassIdAsync(classId);
            var currentTotal = existingCategories.Sum(c => c.Weight);
            if (currentTotal + request.Weight > 100)
            {
                throw new BadRequestException($"Không thể thêm đầu điểm vì tổng trọng số sẽ vượt quá 100%. Tổng hiện tại: {currentTotal}%.");
            }

            var newCategory = new GradeCategory
            {
                GradeCategoryId = Guid.NewGuid(),
                ClassId = classId,
                Name = request.Name,
                Weight = request.Weight
            };

            await _gradeCategoryRepository.AddAsync(newCategory);
            return newCategory.GradeCategoryId;
        }

        public async Task UpdateGradeCategoryAsync(Guid classId, UpdateGradeCategoryDto request)
        {
            await RequireTeacherAccessAsync(classId);

            var category = await _gradeCategoryRepository.GetByIdAsync(request.GradeCategoryId);
            if (category == null || category.ClassId != classId) throw new NotFoundException("Không tìm thấy đầu điểm trong lớp học này.");

            var existingCategories = await _gradeCategoryRepository.GetByClassIdAsync(classId);
            var otherTotal = existingCategories.Where(c => c.GradeCategoryId != request.GradeCategoryId).Sum(c => c.Weight);
            
            if (otherTotal + request.Weight > 100)
            {
                throw new BadRequestException($"Không thể cập nhật đầu điểm vì tổng trọng số sẽ vượt quá 100%. Tổng các đầu điểm còn lại: {otherTotal}%.");
            }

            category.Name = request.Name;
            category.Weight = request.Weight;

            await _gradeCategoryRepository.UpdateAsync(category);
        }

        public async Task BulkUpdateCategoriesAsync(Guid classId, BulkUpdateGradeCategoryDto request)
        {
            await RequireTeacherAccessAsync(classId);

            var totalWeight = request.Categories.Sum(c => c.Weight);
            if (totalWeight > 100)
            {
                throw new BadRequestException("Tổng trọng số các đầu điểm không được vượt quá 100%.");
            }

            var existingCategories = await _gradeCategoryRepository.GetByClassIdAsync(classId);

            foreach (var update in request.Categories)
            {
                var categoryToUpdate = existingCategories.FirstOrDefault(c => c.GradeCategoryId == update.GradeCategoryId);
                if (categoryToUpdate != null)
                {
                    categoryToUpdate.Name = update.Name;
                    categoryToUpdate.Weight = update.Weight;
                }
            }

            await _gradeCategoryRepository.UpdateWeightsAsync(existingCategories);
        }

        public async Task DeleteGradeCategoryAsync(Guid classId, Guid categoryId)
        {
            await RequireTeacherAccessAsync(classId);

            var category = await _gradeCategoryRepository.GetByIdAsync(categoryId);
            if (category == null || category.ClassId != classId) throw new NotFoundException("Không tìm thấy đầu điểm.");

            await _gradeCategoryRepository.DeleteAsync(category);
        }

        public async Task<GradebookResponseDto> GetClassGradebookAsync(Guid classId)
        {
            await RequireTeacherAccessAsync(classId);
            var classroom = await _classRepository.GetByIdAsync(classId);
            var enrollments = await _classRepository.GetClassMemberAsync(classId);
            var assignments = await _assignmentRepository.GetByClassIdAsync(classId);
            var submissions = await _submissionRepository.GetSubmissionsForClassAsync(classId);

            var response = new GradebookResponseDto
            {
                ClassId = classId,
                ClassName = classroom?.ClassName ?? "Unknown Class"
            };

            // Build columns
            foreach (var a in assignments.OrderBy(x => x.DueDate))
            {
                response.Columns.Add(new GradebookColumnDto
                {
                    AssignmentId = a.AssignmentId,
                    Title = a.Title,
                    GradeCategoryId = a.GradeCategoryId,
                    GradeCategoryName = a.GradeCategory?.Name ?? "General",
                    Weight = a.GradeCategory?.Weight ?? 0
                });
            }

            // Build student rows
            foreach (var e in enrollments)
            {
                var studentRow = new GradebookStudentRowDto
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? "Unknown"
                };

                // Group assignments by category để tính avg từng category
                var categoryData = new Dictionary<Guid, (double sumGrades, int count, double weight)>();

                foreach (var col in response.Columns)
                {
                    var sub = submissions.FirstOrDefault(s =>
                        s.AssignmentId == col.AssignmentId && s.StudentId == e.StudentId);

                    studentRow.Grades.Add(new StudentGradeEntryDto
                    {
                        AssignmentId = col.AssignmentId,
                        SubmissionId = sub?.SubmissionId,
                        Grade = sub?.Grade
                    });

                    // Chỉ tính nếu có điểm và có category
                    if (sub?.Grade != null && col.GradeCategoryId.HasValue)
                    {
                        var catId = col.GradeCategoryId.Value;
                        if (!categoryData.ContainsKey(catId))
                            categoryData[catId] = (0, 0, (double)col.Weight);

                        var existing = categoryData[catId];
                        categoryData[catId] = (
                            existing.sumGrades + (double)sub.Grade,
                            existing.count + 1,
                           (double)col.Weight
                        );
                    }
                }

                // Tính FinalAverage: Tổng(AvgCategory * Weight) / Tổng(Weight có điểm)
                double sumGradeWeight = 0;
                double sumCurrentWeights = 0;

                foreach (var (_, data) in categoryData)
                {
                    var avgInCategory = data.sumGrades / data.count;
                    sumGradeWeight += avgInCategory * data.weight;
                    sumCurrentWeights += data.weight;
                }

                studentRow.FinalAverage = sumCurrentWeights > 0
                    ? (decimal)Math.Round(sumGradeWeight / sumCurrentWeights, 2)
                    : 0;

                response.StudentRows.Add(studentRow);
            }

            return response;
        }
        public async Task<byte[]> ExportClassGradebookToExcelAsync(Guid classId)
        {
            var data = await GetClassGradebookAsync(classId);

            ExcelPackage.License.SetNonCommercialPersonal("YourName");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Gradebook");

            // Header Generation
            worksheet.Cells[1, 1].Value = "Student Name";
            int colIndex = 2;
            foreach (var col in data.Columns)
            {
                worksheet.Cells[1, colIndex].Value = col.Title;
                worksheet.Cells[2, colIndex].Value = $"{col.Weight}%";
                colIndex++;
            }
            worksheet.Cells[1, colIndex].Value = "Final Average";

            // Row Generation
            int rowIndex = 3;
            foreach (var row in data.StudentRows)
            {
                worksheet.Cells[rowIndex, 1].Value = row.StudentName;
                colIndex = 2;
                foreach (var col in data.Columns)
                {
                    var grade = row.Grades.FirstOrDefault(g => g.AssignmentId == col.AssignmentId)?.Grade;
                    worksheet.Cells[rowIndex, colIndex].Value = grade.HasValue ? grade.Value.ToString() : "-";
                    colIndex++;
                }
                worksheet.Cells[rowIndex, colIndex].Value = row.FinalAverage;
                rowIndex++;
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> ExportClassGradebookToPdfAsync(Guid classId)
        {
            var data = await GetClassGradebookAsync(classId);

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(compose => 
                    {
                        compose.Text($"Gradebook - {data.ClassName}")
                            .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(120); // Student Name
                            foreach (var col in data.Columns)
                            {
                                columns.RelativeColumn();
                            }
                            columns.RelativeColumn(); // Final Average
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Text("Student Name").SemiBold();
                            foreach (var col in data.Columns)
                            {
                                header.Cell().Text($"{col.Title}\n({col.Weight}%)").SemiBold();
                            }
                            header.Cell().Text("Final Average").SemiBold();
                        });

                        // Rows
                        foreach (var row in data.StudentRows)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text(row.StudentName);
                            foreach (var col in data.Columns)
                            {
                                var grade = row.Grades.FirstOrDefault(g => g.AssignmentId == col.AssignmentId)?.Grade;
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text(grade.HasValue ? grade.Value.ToString() : "-");
                            }
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text(row.FinalAverage.ToString()).SemiBold();
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task SaveBulkGradesAsync(Guid classId, BulkSaveGradesRequest request)
        {
            await RequireTeacherAccessAsync(classId);

            if (request.ChangedGrades == null || !request.ChangedGrades.Any())
                return;

            var assignmentIds = request.ChangedGrades.Select(g => g.AssignmentId).Distinct().ToList();

            var existingSubmissions = await _submissionRepository.GetByAssignmentIdsAsync(assignmentIds);

            var submissionsToInsert = new List<Submission>();
            var submissionsToUpdate = new List<Submission>();

            foreach (var cell in request.ChangedGrades)
            {
                var existingSub = existingSubmissions.FirstOrDefault(s =>
                    s.AssignmentId == cell.AssignmentId && s.StudentId == cell.StudentId);

                if (existingSub != null)
                {
                    if (existingSub.Grade != cell.Grade)
                    {
                        existingSub.Grade = cell.Grade;
                        existingSub.Status = "Graded";
                        submissionsToUpdate.Add(existingSub);
                    }
                }
                else
                {
                    if (cell.Grade.HasValue)
                    {
                        var newSubmission = new Submission
                        {
                            SubmissionId = Guid.NewGuid(),
                            AssignmentId = cell.AssignmentId,
                            StudentId = cell.StudentId,
                            Grade = cell.Grade,
                            Status = "Graded",
                            SubmittedAt = DateTime.UtcNow 
                        };
                        submissionsToInsert.Add(newSubmission);
                    }
                }
            }

         
            if (submissionsToInsert.Any())
            {
                await _submissionRepository.AddRangeAsync(submissionsToInsert);
            }

            if (submissionsToUpdate.Any())
            {
                await _submissionRepository.UpdateRangeAsync(submissionsToUpdate);
            }
        }

        public async Task<StudentGradeBookDto> GetStudentGradeReportAsync(Guid classId)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            var classEntity = await _classRepository.GetByIdAsync(classId);
            if (classEntity == null) throw new Exception("Không tìm thấy lớp học.");
            bool isEnrolled = await _classRepository.IsStudentAlreadyEnrolledAsync(classId, studentId);
            if(isEnrolled == false) throw new Exception("Bạn chưa tham gia vào lớp học này.");
            var categories = await _gradeCategoryRepository.GetStudentGradeDetailsAsync(classId, studentId);
            var reportDto = new StudentGradeBookDto
            {
                ClassId = classId,
                StudentId = studentId,
                CurrentAverageScore = 0
            };
            decimal totalWeightedScore = 0;
            decimal totalValidWeight = 0;

            foreach (var category in categories)
            {
                var categoryDto = new CategoryGradeDto
                {
                    CategoryName = category.Name,
                    Weight = category.Weight
                };

                var validScores = new List<decimal>();

                foreach (var assignment in category.Assignments)
                {
                    // Tìm bài nộp của học sinh
                    var submission = assignment.Submissions.FirstOrDefault();

                    var assignmentDto = new AssignmentGradeItemDto
                    {
                        AssignmentId = assignment.AssignmentId,
                        Title = assignment.Title,
                        Score = submission?.Grade,

                        CommentFeedback = submission != null && submission.SubmissionFeedbacks.Any()
                            ? string.Join("; ", submission.SubmissionFeedbacks.OrderByDescending(f => f.CreatedAt).Select(f => f.Content))
                            : "Chưa có nhận xét"
                    };

                    categoryDto.Assignments.Add(assignmentDto);

                    if (submission != null && submission.Status == "Graded" && submission.Grade.HasValue)
                    {
                        validScores.Add(submission.Grade.Value);
                    }
                }
                if (validScores.Any())
                {
                    categoryDto.CategoryScore = validScores.Average();

                    totalWeightedScore += categoryDto.CategoryScore.Value * category.Weight;
                    totalValidWeight += category.Weight;
                }

                reportDto.GradeReportTable.Add(categoryDto);
            }

            // Tính điểm trung bình hiện tại (Overall GPA)
            if (totalValidWeight > 0)
            {
                reportDto.CurrentAverageScore = Math.Round(totalWeightedScore / totalValidWeight, 2);
            }
            return reportDto;
        }

    }
}
