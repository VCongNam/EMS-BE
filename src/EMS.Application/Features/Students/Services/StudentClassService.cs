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
    public class StudentClassService : IStudentClassService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IClassRepository _classRepository;
        private readonly IAssignmentRepository _assignmentRepository;

        public StudentClassService(ICurrentUserService currentUser, IClassRepository classRepository, IAssignmentRepository assignmentRepository)
        {
            _currentUser = currentUser;
            _classRepository = classRepository;
            _assignmentRepository = assignmentRepository;
        }



        public async Task<PagedResult<EnrolledClassDto>> GetMyClassesAsync(EnrolledClassFilter filter)
        {
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

            var (entities, totalCount) = await _classRepository.GetClassByStudentIdAsync(studentId, filter.Page, filter.Size, filter.Status);
            var responseItems = entities.Select(ce => new EnrolledClassDto
            {
                ClassID = ce.ClassId,
                ClassName = ce.Class?.ClassName ?? "N/A",
                StartDate = (DateOnly)(ce.Class?.StartDate),
                EndDate = (DateOnly)(ce.Class?.EndDate),
                TeacherName = ce.Class?.Teacher.TeacherNavigation.FullName,
                EnrollmentStatus = ce.Status,
                EnrolledDate = (DateOnly)ce.EnrolledDate,
            }).ToList();
            return new PagedResult<EnrolledClassDto>
            {
                Items = responseItems,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size)
            };
        }

        public async Task<EnrolledClassDetailDto> GetClassDetailAsync(Guid classId)
        {
            Guid studentId = _currentUser.UserId;

            var enrollmentEntity = await _classRepository.GetClassSummaryAsync(classId, studentId);
            bool isEnrolled = await _classRepository.IsStudentAlreadyEnrolledAsync(classId, studentId);
            if(enrollmentEntity == null && !isEnrolled)
            {
                throw new UnauthorizedAccessException("Không tìm thấy lớp học hoặc bạn không có quyền truy cập!");
            }
            int pendingAssignmentCount = await _assignmentRepository.CountPendingAssignmentAsync(classId, studentId);
            return new EnrolledClassDetailDto
            {
                ClassID = enrollmentEntity.ClassId,
                ClassName = enrollmentEntity.Class.ClassName,
                TeacherName = enrollmentEntity.Class.Teacher.TeacherNavigation.FullName,
                PendingAssignmentsCount = pendingAssignmentCount
            };
        }

        public async Task<PagedResult<PostDto>> GetClassPostsAsync(Guid classId, PostFilter filter)
        {
            Guid studentId = _currentUser.UserId;
            bool isEnrolled = await _classRepository.IsStudentAlreadyEnrolledAsync(classId, studentId);
            if (!isEnrolled)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập!");
            }
            if (filter.FromDate > filter.ToDate)
            {
                throw new ArgumentException("Ngày bắt đầu phải trước ngày kết thúc!");
            }
            var (entities, totalCount) = await _classRepository.GetClassPostAsync(classId, filter.Page, filter.Size, filter.FromDate, filter.ToDate);
            var items = entities.Select(p => new PostDto
            {
                PostID = p.PostId,
                Content = p.Content,
                CreatedAt = (DateTime)p.CreatedAt,
            }).ToList();
            return new PagedResult<PostDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size),
                CurrentPage = filter.Page
            };
        }
    }
}
