using EMS.Application.Features.Assignments.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assignments.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        // private readonly IUnitOfWork _unitOfWork;
        private readonly ISubmissionRepository _submissionRepository;

        public AssignmentService(IAssignmentRepository assignmentRepository, ISubmissionRepository submissionRepository)
        {
            _assignmentRepository = assignmentRepository;
            _submissionRepository = submissionRepository;
        }

        public async Task<Guid> CreateAssignmentAsync(CreateAssignmentDto request)
        {
            var assignment = new Assignment
            {
                AssignmentId = Guid.NewGuid(),
                ClassId = request.ClassId,
                AuthorId = request.AuthorId,
                GradeCategoryId = request.GradeCategoryId,
                Title = request.Title,
                Description = request.Description,
                AttachmentPath = request.AttachmentPath,
                DueDate = request.DueDate,
                Status = "Published",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow 
            };

            await _assignmentRepository.AddAsync(assignment);
            // await _unitOfWork.SaveChangesAsync(); // <-- BẮT BUỘC PHẢI GỌI HÀM NÀY ĐỂ LƯU XUỐNG DB

            return assignment.AssignmentId;
        }

        public async Task UpdateAssignmentAsync(Guid id, UpdateAssignmentDto request)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null)
            {
                throw new Exception($"Assignment with ID {id} not found.");
            }
            assignment.Title = request.Title;
            assignment.Description = request.Description;
            assignment.AttachmentPath = request.AttachmentPath;
            assignment.DueDate = request.DueDate;
            assignment.GradeCategoryId = request.GradeCategoryId;
            assignment.Status = request.Status;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _assignmentRepository.UpdateAsync(assignment);
            // await _unitOfWork.SaveChangesAsync(); // <-- BẮT BUỘC PHẢI GỌI HÀM NÀY
        }
      

        // 1. DELETE 
        public async Task DeleteAssignmentAsync(Guid id)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id);
            if (assignment == null) throw new Exception("Assignment not found.");

            assignment.IsDeleted = true;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _assignmentRepository.UpdateAsync(assignment);
            // await _unitOfWork.SaveChangesAsync(); // <-- Nhớ gọi hàm này
        }

        // 2. VIEW ASSIGNED ASSIGNMENTS 
        public async Task<IEnumerable<AssignmentSummaryDto>> GetAssignmentsByClassIdAsync(Guid classId)
        {
            var assignments = await _assignmentRepository.GetByClassIdAsync(classId);

            return assignments.Select(a => new AssignmentSummaryDto
            {
                AssignmentId = a.AssignmentId,
                Title = a.Title,
                DueDate = a.DueDate,
                Status = a.Status
            });
        }

        // 3. VIEW ASSIGNMENT SUBMISSIONS 
        public async Task<AssignmentSubmissionsDto> GetAssignmentSubmissionsAsync(Guid assignmentId)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null) throw new Exception("Assignment not found.");

            var submissions = await _submissionRepository.GetSubmissionsByAssignmentIdAsync(assignmentId);

            return new AssignmentSubmissionsDto
            {
                AssignmentId = assignment.AssignmentId,
                Title = assignment.Title,
                DueDate = assignment.DueDate,
                Submissions = submissions.Select(s => new SubmissionBasicDto
                {
                    SubmissionId = s.SubmissionId,
                    StudentId = s.StudentId,
                    SubmittedAt = (DateTime)s.SubmittedAt,
                    Status = s.Status,
                    Grade = s.Grade
                }).ToList()
            };
        }
    }

}
