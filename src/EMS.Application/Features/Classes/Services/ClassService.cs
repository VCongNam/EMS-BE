using CloudinaryDotNet;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Notifications.Services;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using FluentValidation;
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
        private readonly IValidator<CreateClassDto> _createClassValidator;

        public ClassService(
             IClassRepository classRepository,
             ISessionRepository sessionRepository,
             ICurrentUserService currentUser,
             INotificationService notificationService,
             IValidator<CreateClassDto> createClassValidator) 
        {
            _classRepository = classRepository;
            _sessionRepository = sessionRepository;
            _currentUser = currentUser;
            _notificationService = notificationService;
            _createClassValidator = createClassValidator;
        }

        public async Task<Guid> CreateClassAsync(CreateClassDto request)
        {
            var currentTeacherId = _currentUser.UserId;
            if (request.Schedules != null && request.Schedules.Any())
            {
                var existingClasses = await _classRepository.GetClassesByTeacherIdAsync(currentTeacherId);

                var overlappingClasses = existingClasses.Where(c =>
                    c.Status != "Archived" && c.Status != "Completed" &&
                    c.StartDate <= request.EndDate && c.EndDate >= request.StartDate).ToList();

                foreach (var oldClass in overlappingClasses)
                {
                    foreach (var oldSchedule in oldClass.ClassSchedules) 
                    {
                        foreach (var newSchedule in request.Schedules)
                        {
                            if (oldSchedule.DayOfWeek == newSchedule.DayOfWeek)
                            {
                                if (newSchedule.StartTime < oldSchedule.EndTime && newSchedule.EndTime > oldSchedule.StartTime)
                                {
                                    var displayDay = oldSchedule.DayOfWeek == 7 ? "Chủ Nhật" : $"Thứ {oldSchedule.DayOfWeek + 1}";

                                    throw new BadRequestException(
                                        $"Trùng lịch dạy! Lớp '{oldClass.ClassName}' đang học vào {displayDay} " +
                                        $"từ {oldSchedule.StartTime} đến {oldSchedule.EndTime}.");


                                }
                            }
                        }
                    }
                }
            }

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
                CreatedAt = DateTime.UtcNow,
                ClassSchedules = new List<ClassSchedule>(),
                Sessions = new List<Session>()
            };

            if (request.Schedules != null && request.Schedules.Any())
            {
                foreach(var s in request.Schedules)
                {
                    short scheduleDayOfWeek = s.DayOfWeek == 0 ? (short)7 : s.DayOfWeek;
                    newClass.ClassSchedules.Add(new ClassSchedule
                    {
                        ScheduleId = Guid.NewGuid(),
                        ClassId = newClass.ClassId,
                        DayOfWeek = scheduleDayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    });
                }

                int lessonCount = 1; 
                for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
                {
                    short currentDayOfWeek = (short)d.DayOfWeek;
                    if (currentDayOfWeek == 0) currentDayOfWeek = 7;

                    var matchingSchedules = newClass.ClassSchedules.Where(s => s.DayOfWeek == currentDayOfWeek);
                    foreach (var s in matchingSchedules)
                    {
                        newClass.Sessions.Add(new Session
                        {
                            SessionId = Guid.NewGuid(),
                            ClassId = newClass.ClassId,
                            Title = $"Buổi {lessonCount}: {newClass.ClassName}",
                            Date = d,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime,
                            Status = "Scheduled",
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                        lessonCount++;
                    }
                }
            }

            await _classRepository.AddAsync(newClass);

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
            var classEntity = await _classRepository.GetByIdAsync(classId);
            if (classEntity == null)
            {
                throw new Exception("Không tìm thấy lớp học!");
            }
            var currentMembers = await _classRepository.GetClassMemberAsync(classId);
            int currentStudentCount = currentMembers.Count();
            if (classEntity.MaxStudents.HasValue && currentStudentCount >= classEntity.MaxStudents.Value)
            {
                throw new Exception($"Lớp học đã đạt số lượng tối đa ({classEntity.MaxStudents.Value} học sinh). Không thể thêm mới!");
            }

            var existingEnrollment = await _classRepository.GetEnrollmentAsync(classId, request.StudentID);

            if (existingEnrollment != null)
            {
                if (existingEnrollment.Status == "Dropped")
                {
                    existingEnrollment.Status = "Active";
                    existingEnrollment.EnrolledDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    await _classRepository.UpdateEnrollmentAsync(existingEnrollment);
                }
                else
                {
                    throw new Exception("Học sinh đã được thêm vào lớp này và đang hoạt động!");
                }
            }
            else
            {
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
            }

            //Notification
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
            var now = DateOnly.FromDateTime(DateTime.UtcNow);

            var result = classes.Where(c => c.Status != "Archived").Select(c => new ClassSummaryDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                Room = c.Room,
                Status = now < c.StartDate ? "Scheduled"
                           : now > c.EndDate ? "Completed"
                           : "Ongoing",
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

            classroom.Status = "Active"; 
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

        public async Task<bool> RemoveStudentFromClassAsync(Guid classId, Guid studentId)
        {
            // Lấy thông tin người đang thực hiện thao tác
            var currentUserId = _currentUser.UserId;
            var currentUserRole = _currentUser.Role;

            // 1. Kiểm tra tồn tại và trạng thái lớp học
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null)
            {
                throw new Exception($"Không tìm thấy lớp học với ID {classId}.");
            }

            if (classroom.Status == "Archived" || classroom.Status == "Completed")
            {
                throw new Exception("Không thể thay đổi danh sách học sinh của lớp đã kết thúc hoặc lưu trữ.");
            }

            // 2. Phân quyền (Authorization): Chỉ Admin hoặc đúng Giáo viên chủ nhiệm mới được đuổi
            if (currentUserRole != "Teacher" && classroom.TeacherId != currentUserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền đuổi học sinh khỏi lớp này. Chỉ Giáo viên phụ trách mới được phép thao tác.");
            }

            // 3. Lấy thông tin ghi danh
            var enrollment = await _classRepository.GetEnrollmentAsync(classId, studentId);
            if (enrollment == null)
            {
                throw new Exception("Học sinh này không có mặt trong danh sách lớp.");
            }

            if (enrollment.Status == "Dropped")
            {
                throw new Exception("Học sinh này đã được rút khỏi lớp từ trước.");
            }

            // 4. Thực thi (Soft Update) - Giữ nguyên lịch sử học phí và điểm danh cũ
            enrollment.Status = "Dropped";
            enrollment.DroppedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            enrollment.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateEnrollmentAsync(enrollment);

            return true;
        }
        public async Task<bool> RestoreStudentInClassAsync(Guid classId, Guid studentId)
        {
            var currentUserId = _currentUser.UserId;
            var currentUserRole = _currentUser.Role;

            // 1. Kiểm tra tồn tại và trạng thái lớp học
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null) throw new Exception("Lớp học không tồn tại.");

            if (classroom.Status == "Archived" || classroom.Status == "Completed")
                throw new Exception("Không thể khôi phục học sinh vào lớp đã kết thúc hoặc lưu trữ.");

            // 2. Phân quyền
            if (currentUserRole != "Teacher" && classroom.TeacherId != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            // 3. Lấy thông tin ghi danh
            var enrollment = await _classRepository.GetEnrollmentAsync(classId, studentId);
            if (enrollment == null)
                throw new Exception("Học sinh này chưa từng được ghi danh vào lớp.");

            if (enrollment.Status == "Active")
                throw new Exception("Học sinh này vẫn đang học bình thường trong lớp.");

            // 4. Thực thi: Đổi Status và Xóa ngày Dropped
            enrollment.Status = "Active";
            enrollment.DroppedDate = null; // Quan trọng: Xóa ngày rút lớp
            enrollment.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateEnrollmentAsync(enrollment);

            // Mở rộng sau này: Ghi log "Restore Student", Khôi phục Hóa đơn (nếu cần)...

            return true;
        }

        public async Task<IEnumerable<ClassStaffDto>> GetClassStaffOnlyAsync(Guid classId)
        {
            var classroom = await _classRepository.GetClassStaffAsync(classId);

            if (classroom == null) throw new Exception("Không tìm thấy lớp học.");

            var staffList = new List<ClassStaffDto>();

            // 1. Lấy Giáo viên chủ nhiệm của lớp
            if (classroom.Teacher?.TeacherNavigation != null)
            {
                staffList.Add(new ClassStaffDto
                {
                    UserId = classroom.TeacherId,
                    FullName = classroom.Teacher.TeacherNavigation.FullName,
                    Email = classroom.Teacher.TeacherNavigation.Email,
                    AvatarUrl = classroom.Teacher.TeacherNavigation.AvatarUrl,
                    Role = "Teacher"
                });
            }

            // 2. Lấy TẤT CẢ Trợ giảng được assigned vào lớp này
            // classroom.ClassTa chỉ chứa các TA của ClassId hiện tại nhờ câu Include ở Repository
            if (classroom.ClassTa != null)
            {
                var assignedTAs = classroom.ClassTa
                    .Where(cta => cta.Status == "Active") // Chỉ lấy những người đang Active trong lớp này
                    .Select(cta => new ClassStaffDto
                    {
                        UserId = cta.Taid,
                        FullName = cta.Ta.Ta.FullName,
                        Email = cta.Ta.Ta.Email,
                        AvatarUrl = cta.Ta.Ta.AvatarUrl,
                        Role = "TA"
                    });

                staffList.AddRange(assignedTAs);
            }

            return staffList;
        }

    }
}
