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
            if (classroom == null) throw new Exception("Class not found.");
            if (classroom.TeacherId != _currentUserService.UserId) throw new Exception("You do not have access to this class's gradebook.");
        }

        public async Task<IEnumerable<GradeCategoryDto>> GetGradeCategoriesByClassAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null) throw new Exception("Class not found.");

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
                throw new Exception($"Cannot add category. Total weight would exceed 100%. Current total: {currentTotal}%");
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
            if (category == null || category.ClassId != classId) throw new Exception("Grade Category not found in this class.");

            var existingCategories = await _gradeCategoryRepository.GetByClassIdAsync(classId);
            var otherTotal = existingCategories.Where(c => c.GradeCategoryId != request.GradeCategoryId).Sum(c => c.Weight);
            
            if (otherTotal + request.Weight > 100)
            {
                throw new Exception($"Cannot update category. Total weight would exceed 100%. Other categories total: {otherTotal}%");
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
                throw new Exception("Total weight of grade categories cannot exceed 100.");
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
            if (category == null || category.ClassId != classId) throw new Exception("Grade Category not found.");

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

            // Calculate active total weights from current categories in class
            var totalWeight = response.Columns.GroupBy(c => c.GradeCategoryId)
                                                .Sum(g => g.First().Weight);

            // Build student rows
            foreach (var e in enrollments)
            {
                var studentRow = new GradebookStudentRowDto
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student?.FullName ?? "Unknown"
                };

                double sumGradeWeight = 0;
                double sumCurrentWeights = 0; // Sum of category weights that the student has been graded on.
                                              // Wait, the formula requested is: Total(Grade * Weight) / Total_Weights (of graded).
                                              // We need to carefully define "Total Weights". If they missed an assignment, do they get 0 or is it excluded?
                                              // "Tổng (Điểm * Hệ số) / Tổng các Hệ số hiện có" -> means we divide by sum of ALL weights.

                foreach (var col in response.Columns)
                {
                    var sub = submissions.FirstOrDefault(s => s.AssignmentId == col.AssignmentId && s.StudentId == e.StudentId);
                    studentRow.Grades.Add(new StudentGradeEntryDto
                    {
                        AssignmentId = col.AssignmentId,
                        SubmissionId = sub?.SubmissionId,
                        Grade = sub?.Grade
                    });

                    if (sub?.Grade != null)
                    {
                        sumGradeWeight += (double)sub.Grade * (double)col.Weight;
                        sumCurrentWeights += (double)col.Weight; 
                        // Wait, is sumCurrentWeights for all columns, or only for columns where the student has a grade?
                        // If we divide by sumCurrentWeights of *graded* items, then `Average = sumGradeWeight / sumCurrentWeights`. 
                    }
                }

                // If no totalWeight, or no sumCurrentWeights
                if (sumCurrentWeights > 0)
                {
                    studentRow.FinalAverage = (decimal)Math.Round(sumGradeWeight / sumCurrentWeights, 2);
                }
                else
                {
                    studentRow.FinalAverage = 0;
                }

                response.StudentRows.Add(studentRow);
            }

            return response;
        }
        public async Task<byte[]> ExportClassGradebookToExcelAsync(Guid classId)
        {
            var data = await GetClassGradebookAsync(classId);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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
    }
}
