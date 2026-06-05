using DocumentFormat.OpenXml.Spreadsheet;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Posts.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public class StudentClassService : IStudentClassService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IClassRepository _classRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IStudentRepository _studentRepository;

        public StudentClassService(
            ICurrentUserService currentUser,
            IClassRepository classRepository,
            IAssignmentRepository assignmentRepository,
            IStudentRepository studentRepository)
        {
            _currentUser = currentUser;
            _classRepository = classRepository;
            _assignmentRepository = assignmentRepository;
            _studentRepository = studentRepository;
        }



        public async Task<PagedResult<EnrolledClassDto>> GetMyClassesAsync(EnrolledClassFilter filter)
        {
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            var (entities, totalCount) = await _classRepository.GetClassByStudentIdAsync(studentId, filter.Page, filter.Size);
            var responseItems = entities.Select(ce => new EnrolledClassDto
            {
                ClassID = ce.ClassId,
                ClassName = ce.Class?.ClassName ?? "N/A",
                StartDate = (DateOnly)(ce.Class?.StartDate),
                EndDate = (DateOnly)(ce.Class?.EndDate),
                TeacherName = ce.Class?.Teacher.TeacherNavigation.FullName,
                EnrollmentStatus = ce.Status,
                EnrolledDate = (DateOnly)ce.EnrolledDate,
                Schedules = ce.Class.ClassSchedules.Select(s => new ScheduleDto
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList(),


                ClassStatus = now < ce.Class.StartDate ? "Scheduled"
                          : now > ce.Class.EndDate ? "Completed"
                          : "Ongoing",
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
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

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
                GradeLevel = enrollmentEntity.Class.Subject?.GradeLevel ?? 0,
                TeacherName = enrollmentEntity.Class.Teacher.TeacherNavigation.FullName,
                PendingAssignmentsCount = pendingAssignmentCount
            };
        }

        public async Task<PagedResult<StudentPostDto>> GetClassPostsAsync(Guid classId, PostFilter filter)
        {
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
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
            var items = entities.Select(p => new StudentPostDto
            {
                PostID = p.PostId,
                Content = p.Content,
                CreatedAt = (DateTime)p.CreatedAt,
            }).ToList();
            return new PagedResult<StudentPostDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size),
                CurrentPage = filter.Page
            };
        }

        public async Task<List<ManagedStudentDto>> GetStudentsManagedByTeacherAsync()
        {
            var teacherId = _currentUser.UserId;
            var currentUserRole = _currentUser.Role;
            if (currentUserRole != "Teacher") throw new Exception("Bạn phải là giáo viên để thực hiện hành động này");
            var rawData = await _studentRepository.GetAllManagedStudentAsync(teacherId);

            var result = rawData
                .GroupBy(ce => ce.StudentId)
                .Select(group =>
                {
                    var firstEntry = group.First();
                    return new ManagedStudentDto
                    {
                        StudentId = group.Key,
                        FullName = firstEntry.Student.FullName,
                        PhoneNumber = firstEntry.Student.Account?.PhoneNumber ?? "N/A",
                        ClassNames = group.Select(ce => ce.Class.ClassName).Distinct().ToList()
                    };
                })
                .OrderBy(s => s.FullName)
                .ToList();
            return result;
        }
    }
}
