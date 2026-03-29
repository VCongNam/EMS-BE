using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentAssignmentService : IStudentAssignmentService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IAssignmentRepository _assignmentRepository;
        public StudentAssignmentService(ICurrentUserService currentUser, IAssignmentRepository assignmentRepository)
        {
            _currentUser = currentUser;
            _assignmentRepository = assignmentRepository;
        }

        public async Task<PagedResult<AssignmentItemDto>> GetClassAssignmentsAsync(Guid classId, AssignmentFilter filter)
        {
            Guid studentId = _currentUser.UserId;
            var (models, totalCount) = await _assignmentRepository.GetStudentAssignmentsAsync(classId, studentId, filter.Page, filter.Size);
            var items = models.Select(m =>
            {
                var a = m.Assignment;
                var s = m.Submission;
                string status = "Chưa nộp";
                if (s!=null)
                {
                    status = s.Grade.HasValue ? "Đã chấm" : "Đã Nộp";
                }
                if(s.SubmittedAt > a.DueDate){
                    status = "Quá hạn";
                }
                return new AssignmentItemDto
                {
                    AssignmentID = a.AssignmentId,
                    Title = a.Title,
                    DueDate = a.DueDate,
                    StudentStatus = status
                };
            }).ToList();
            return new PagedResult<AssignmentItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size),
                CurrentPage = filter.Page
            };
        }
    }
}
