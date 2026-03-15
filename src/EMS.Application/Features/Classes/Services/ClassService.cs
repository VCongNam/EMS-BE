using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUser;

        public ClassService(IClassRepository classRepository, ICurrentUserService currentUser)
        {
            _classRepository = classRepository;
            _currentUser = currentUser;
        }

        public async Task<Guid> CreateClassAsync(CreateClassDto request)
        {
            // 1. Xử lý Subject (Tìm hoặc Tạo mới)
            var subject = await _classRepository.GetSubjectByNameAndGradeAsync(request.SubjectName, request.GradeLevel);
            if (subject == null)
            {
                subject = new Subject
                {
                    SubjectId = Guid.NewGuid(),
                    SubjectName = request.SubjectName,
                    GradeLevel = request.GradeLevel,
                    IsDeleted = false
                };
                await _classRepository.AddSubjectAsync(subject);
            }

            // 2. Tạo Class
            var newClass = new Class
            {
                ClassId = Guid.NewGuid(),
                TeacherId = _currentUser.UserId,
                ClassName = request.ClassName,
                SubjectId = subject.SubjectId,
                MaxStudents = request.MaxStudents,
                Room = request.Room,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TuitionFee = request.TuitionFee,
                Status = "Scheduled",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _classRepository.AddAsync(newClass);

            // 3. Thêm Lịch học (Schedules)
            if (request.Schedules != null && request.Schedules.Any())
            {
                var schedules = request.Schedules.Select(s => new ClassSchedule
                {
                    ScheduleId = Guid.NewGuid(),
                    ClassId = newClass.ClassId,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                });
                await _classRepository.AddSchedulesAsync(schedules);
            }

            return newClass.ClassId;
        }
        public async Task<IEnumerable<ClassMemberResponse>> GetClassMembersAsync(Guid classId)
        {
            var enrollments = await _classRepository.GetClassMemberAsync(classId);


            var memberList = enrollments.Select(ce => new ClassMemberResponse
            {
                StudentID = ce.StudentId,
                FullName = ce.Student.StudentNavigation.FullName,
                Email = ce.Student.StudentNavigation.Email,
                ParentName = ce.Student.ParentName,
                ParentPhone = ce.Student.ParentPhone,
                EnrolledDate = ce.EnrolledDate,
                Status = ce.Status
            }).ToList();

            return memberList;
        }

        public async Task<bool> AssignStudentAsync(Guid classId, AssignStudentDto request)
        {
            bool isEnrolled = await _classRepository.IsStudentAlreadyEnrolledAsync(classId, request.StudentID);
            if (isEnrolled)
            {
                throw new Exception("Student is assigned to this class");
            }
            var newEnrollment = new ClassEnrollment
            {
                EnrollmentId = Guid.NewGuid(),
                ClassId = classId,
                StudentId = request.StudentID,
                EnrolledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            await _classRepository.AddEnrollmentAsync(newEnrollment);
            return true;
        }
        public async Task<IEnumerable<ClassSummaryDto>> GetTeacherDashboardAsync()
        {
            var teacherId = _currentUser.UserId;
            var classes = await _classRepository.GetClassesByTeacherIdAsync(teacherId);

            var result = classes.Select(c => new ClassSummaryDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                Room = c.Room,
                Status = c.Status ?? string.Empty,
                StartDate = c.StartDate,
                SubjectName = c.Subject?.SubjectName ?? "N/A",
                GradeLevel = c.Subject?.GradeLevel ?? 0,
                MaxStudents = c.MaxStudents,
                CurrentStudents = c.ClassEnrollments.Count(ce => ce.Status == "Active"),
                Schedules = c.ClassSchedules.Select(s => new ScheduleDto
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList()
            });

            return result;
        }

        public async Task<ClassDetailDto> GetClassDetailAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);

            if (classroom == null)
            {
                throw new Exception($"Class with ID {classId} not found.");
            }

            return new ClassDetailDto
            {
                ClassId = classroom.ClassId,
                TeacherId = classroom.TeacherId,
                ClassName = classroom.ClassName,
                Room = classroom.Room,
                TuitionFee = classroom.TuitionFee,
                StartDate = classroom.StartDate,
                EndDate = classroom.EndDate,
                Status = classroom.Status,
                CreatedAt = (DateTime)classroom.CreatedAt
            };
        }

        // Update Class
        public async Task UpdateClassAsync(Guid classId, UpdateClassDto request)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null)
            {
                throw new Exception($"Class with ID {classId} not found.");
            }

            // 1. Xử lý Subject (Tìm hoặc Tạo mới)
            var subject = await _classRepository.GetSubjectByNameAndGradeAsync(request.SubjectName, request.GradeLevel);
            if (subject == null)
            {
                subject = new Subject
                {
                    SubjectId = Guid.NewGuid(),
                    SubjectName = request.SubjectName,
                    GradeLevel = request.GradeLevel,
                    IsDeleted = false
                };
                await _classRepository.AddSubjectAsync(subject);
            }

            // 2. Cập nhật thông tin Class
            classroom.ClassName = request.ClassName;
            classroom.SubjectId = subject.SubjectId;
            classroom.MaxStudents = request.MaxStudents;
            classroom.Room = request.Room;
            classroom.StartDate = request.StartDate;
            classroom.EndDate = request.EndDate;
            classroom.TuitionFee = request.TuitionFee;
            classroom.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateAsync(classroom);

            // 3. Cập nhật Lịch học (Xóa cũ, Thêm mới)
            await _classRepository.DeleteSchedulesAsync(classId);
            if (request.Schedules != null && request.Schedules.Any())
            {
                var schedules = request.Schedules.Select(s => new ClassSchedule
                {
                    ScheduleId = Guid.NewGuid(),
                    ClassId = classId,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                });
                await _classRepository.AddSchedulesAsync(schedules);
            }
        }

        //  Archive Class 
        public async Task ArchiveClassAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null)
            {
                throw new Exception($"Class with ID {classId} not found.");
            }

            classroom.Status = "Archived";
            classroom.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateAsync(classroom);
        }

    }
}
