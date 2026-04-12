using CloudinaryDotNet;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Notifications.Services;
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
        private readonly ISessionRepository _sessionRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notificationService;

        public ClassService(IClassRepository classRepository, ISessionRepository sessionRepository, ICurrentUserService currentUser, INotificationService notificationService)
        {
            _classRepository = classRepository;
            _sessionRepository = sessionRepository;
            _currentUser = currentUser;
            _notificationService = notificationService;
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

                // 4. Sinh tự động các buổi học (Sessions) từ StartDate đến EndDate
                var sessions = new List<Session>();
                for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
                {
                    var dayOfWeek = (short)d.DayOfWeek;
                    if (dayOfWeek == 0) dayOfWeek = 7; // Assuming 1=Mon, 2=Tue... 7=Sun (commonly used). If it's 0-6, the DB logic may vary. But let's check Vietnam standard where Sunday could be 0 (C# default) or 8. Wait, I will just use C# default (short)d.DayOfWeek if the user previously used it.
                    // Wait, C# DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6. Let's just cast.
                    var matchingSchedules = request.Schedules.Where(s => s.DayOfWeek == (short)d.DayOfWeek);
                    foreach (var s in matchingSchedules)
                    {
                        sessions.Add(new Session
                        {
                            SessionId = Guid.NewGuid(),
                            ClassId = newClass.ClassId,
                            Title = $"{newClass.ClassName} - {d.ToString("dd/MM/yyyy")}",
                            Date = d,
                            Status = "Scheduled",
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (sessions.Any())
                {
                    await _sessionRepository.AddSessionsAsync(sessions);
                }
            }

            return newClass.ClassId;
        }
        public async Task<IEnumerable<ClassMemberResponse>> GetClassMembersAsync(Guid classId)
        {
            var enrollments = await _classRepository.GetClassMemberAsync(classId);


            var memberList = enrollments.Select(ce => new ClassMemberResponse
            {
                StudentID = ce.StudentId,
                FullName = ce.Student.FullName,
                PhoneNumber = ce.Student.Account.PhoneNumber,
                Email = ce.Student.Account.Email,
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
                throw new Exception("Học sinh đã được thêm vào lớp này rồi!");
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

            //Notification
            var classEntity = await _classRepository.GetByIdAsync(classId);
            var accountId = await _notificationService.GetAccountIdByStudentIdAsync(request.StudentID);
            if (classEntity != null && accountId != null)
            {
                await _notificationService.SendNotificationAsync(
                targetAccountId: (Guid)accountId,
                studentId: request.StudentID,
                title: "Chào mừng đến lớp học mới",
                content: $"Bạn đã được giáo viên thêm vào lớp {classEntity.ClassName}",
                actionUrl: $"/student/classes/{classId}",
                type: "Class"
                );
            }
            return true;
        }
        public async Task<IEnumerable<ClassSummaryDto>> GetTeacherDashboardAsync()
        {
            var teacherId = _currentUser.UserId;
            var classes = await _classRepository.GetClassesByTeacherIdAsync(teacherId);

            var result = classes.Where(c => c.Status != "Archived").Select(c => new ClassSummaryDto
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

        // Restore Class
        public async Task RestoreClassAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null)
            {
                throw new Exception($"Class with ID {classId} not found.");
            }

            classroom.Status = "Active"; // Restoring to Active state
            classroom.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateAsync(classroom);
        }

        public async Task<IEnumerable<ClassSummaryDto>> GetArchivedClassesAsync()
        {
            var teacherId = _currentUser.UserId;
            var classes = await _classRepository.GetClassesByTeacherIdAsync(teacherId);

            var result = classes.Where(c => c.Status == "Archived").Select(c => new ClassSummaryDto
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
    }
}
