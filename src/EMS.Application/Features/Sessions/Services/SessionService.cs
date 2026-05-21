using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.Sessions.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMS.Application.Features.Sessions.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SessionService> _logger;

        public SessionService(
            ISessionRepository sessionRepository,
            IClassRepository classRepository,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            ILogger<SessionService> logger)
        {
            _sessionRepository = sessionRepository;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _logger = logger;
        }

        private async Task CheckSessionConflictAsync(Guid teacherId, DateOnly date, TimeOnly? startTime, TimeOnly? endTime, Guid? excludeSessionId = null)
        {
            if (!startTime.HasValue || !endTime.HasValue)
            {
                return;
            }

            var existingSessions = await _sessionRepository.GetSessionsByTeacherAndDateAsync(teacherId, date, excludeSessionId);

            bool isOverlap = existingSessions.Any(s =>
                s.StartTime.HasValue && s.EndTime.HasValue &&
                (
                    (startTime.Value >= s.StartTime.Value && startTime.Value < s.EndTime.Value) ||
                    (endTime.Value > s.StartTime.Value && endTime.Value <= s.EndTime.Value) ||
                    (startTime.Value <= s.StartTime.Value && endTime.Value >= s.EndTime.Value)
                ));

            if (isOverlap)
            {
                throw new ConflictException("Lịch học bị trùng với một buổi học khác của bạn trong cùng thời gian.");
            }
        }

        private static string GetAttendanceStatusLabel(string status, bool? isExcused)
        {
            return status == "Present"
                ? "Có mặt"
                : (isExcused == true ? "Vắng có phép" : "Vắng không phép");
        }

        public async Task<IEnumerable<SessionDto>> GetSessionsByClassIdAsync(Guid classId)
        {
            var sessions = await _sessionRepository.GetSessionsByClassIdAsync(classId);
            return sessions.Select(s => new SessionDto
            {
                SessionId = s.SessionId,
                ClassId = s.ClassId,
                Title = s.Title,
                Date = s.Date,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                MeetingLink = s.MeetingLink,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            });
        }

        public async Task<SessionDetailDto> GetSessionDetailAsync(Guid sessionId)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy buổi học.");
            }

            var classObj = await _classRepository.GetByIdAsync(session.ClassId);
            if (classObj == null)
            {
                throw new NotFoundException("Không tìm thấy lớp học.");
            }

            return new SessionDetailDto
            {
                SessionId = session.SessionId,
                ClassId = session.ClassId,
                ClassName = classObj.ClassName,
                Room = classObj.Room,
                Title = session.Title,
                Date = session.Date,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                MeetingLink = session.MeetingLink,
                Topic = session.Topic,
                Note = session.Note,
                Status = session.Status,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };
        }

        public async Task<IEnumerable<TeacherScheduleDto>> GetTeacherScheduleAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new BadRequestException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
            }

            var teacherId = _currentUserService.UserId;
            var start = DateOnly.FromDateTime(startDate);
            var end = DateOnly.FromDateTime(endDate);

            var sessions = await _sessionRepository.GetSessionsByTeacherAndDateRangeAsync(teacherId, start, end);

            return sessions.Select(s => new TeacherScheduleDto
            {
                SessionId = s.SessionId,
                ClassId = s.ClassId,
                ClassName = s.Class?.ClassName ?? "Unknown",
                Title = s.Title,
                Date = s.Date,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Room = s.Class?.Room,
                MeetingLink = s.MeetingLink,
                Status = s.Status
            });
        }

        public async Task<SessionDto> CreateSessionAsync(CreateSessionDto request)
        {
            var classObj = await _classRepository.GetByIdAsync(request.ClassId);
            if (classObj == null)
            {
                throw new NotFoundException("Không tìm thấy lớp học.");
            }

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
            {
                throw new BadRequestException("Thời gian bắt đầu phải trước thời gian kết thúc.");
            }

            await CheckSessionConflictAsync(classObj.TeacherId, request.Date, request.StartTime, request.EndTime);

            var session = new Session
            {
                SessionId = Guid.NewGuid(),
                ClassId = request.ClassId,
                Title = request.Title ?? $"{classObj.ClassName} Session",
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                MeetingLink = request.MeetingLink,
                Topic = request.Topic,
                Note = request.Note,
                Status = "Scheduled",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _sessionRepository.AddSessionAsync(session);

            try
            {
                var targets = await _notificationService.GetAllClassTargetsAsync(session.ClassId);
                if (targets.Any())
                {
                    string timeStr = session.StartTime.HasValue ? session.StartTime.Value.ToString(@"HH\:mm") : "chưa xác định";
                    await _notificationService.SendBulkNotificationWithStudentAsync(
                        targets: targets,
                        title: "Lịch học mới",
                        content: $"Buổi học '{session.Title}' đã được lên lịch vào ngày {session.Date:dd/MM/yyyy} lúc {timeStr}.",
                        actionUrl: "/schedule",
                        type: "Schedule");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi thông báo tạo session.");
            }

            return new SessionDto
            {
                SessionId = session.SessionId,
                ClassId = session.ClassId,
                Title = session.Title,
                Date = session.Date,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                MeetingLink = session.MeetingLink,
                Status = session.Status,
                CreatedAt = session.CreatedAt
            };
        }

        public async Task<SessionDto> UpdateSessionAsync(Guid sessionId, UpdateSessionDto request)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy buổi học.");
            }

            var classObj = await _classRepository.GetByIdAsync(session.ClassId);
            if (classObj == null)
            {
                throw new NotFoundException("Không tìm thấy lớp học.");
            }

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
            {
                throw new BadRequestException("Thời gian bắt đầu phải trước thời gian kết thúc.");
            }

            await CheckSessionConflictAsync(classObj.TeacherId, request.Date, request.StartTime, request.EndTime, sessionId);

            session.Title = request.Title;
            session.Date = request.Date;
            session.StartTime = request.StartTime;
            session.EndTime = request.EndTime;
            session.MeetingLink = request.MeetingLink;
            session.Topic = request.Topic;
            session.Note = request.Note;
            session.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateSessionAsync(session);

            try
            {
                var targets = await _notificationService.GetAllClassTargetsAsync(session.ClassId);
                if (targets.Any())
                {
                    string timeStr = session.StartTime.HasValue ? session.StartTime.Value.ToString(@"HH\:mm") : "chưa xác định";
                    string dateStr = request.Date.ToString("dd/MM/yyyy");
                    string titleStr = request.Title ?? session.Title ?? "Buổi học";

                    await _notificationService.SendBulkNotificationWithStudentAsync(
                        targets: targets,
                        title: "Thay đổi lịch học",
                        content: $"Buổi học '{titleStr}' đã cập nhật lại thời gian: {timeStr} ngày {dateStr}.",
                        actionUrl: "/schedule",
                        type: "Schedule");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi thông báo cập nhật session.");
            }

            return new SessionDto
            {
                SessionId = session.SessionId,
                ClassId = session.ClassId,
                Title = session.Title,
                Date = session.Date,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                MeetingLink = session.MeetingLink,
                Status = session.Status,
                CreatedAt = session.CreatedAt
            };
        }

        public async Task DeleteSessionAsync(Guid sessionId)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy buổi học.");
            }

            await _sessionRepository.DeleteSessionAsync(session);
        }

        public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceListAsync(Guid sessionId)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy buổi học.");
            }

            var attendances = await _sessionRepository.GetAttendancesBySessionIdAsync(sessionId);
            var students = await _sessionRepository.GetStudentsForSessionAsync(sessionId);

            var result = new List<AttendanceResponseDto>();

            foreach (var student in students)
            {
                var existingAttendance = attendances.FirstOrDefault(a => a.StudentId == student.StudentId);

                if (existingAttendance != null)
                {
                    result.Add(new AttendanceResponseDto
                    {
                        AttendanceId = existingAttendance.AttendanceId,
                        StudentId = student.StudentId,
                        FullName = student.Student.FullName,
                        Status = existingAttendance.Status,
                        IsExcused = existingAttendance.IsExcused,
                        Note = existingAttendance.Note
                    });
                }
                else
                {
                    result.Add(new AttendanceResponseDto
                    {
                        AttendanceId = Guid.Empty,
                        StudentId = student.StudentId,
                        FullName = student.Student.FullName,
                        Status = "Not Taken",
                        IsExcused = false,
                        Note = null
                    });
                }
            }

            return result;
        }

        public async Task TakeAttendanceBulkAsync(Guid sessionId, IEnumerable<TakeAttendanceDto> requests)
        {
            if (requests == null || !requests.Any())
            {
                throw new BadRequestException("Danh sách điểm danh không được để trống.");
            }

            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy buổi học.");
            }

            var existingAttendances = await _sessionRepository.GetAttendancesBySessionIdAsync(sessionId);

            var newAttendances = new List<Attendance>();
            var toUpdate = new List<Attendance>();

            foreach (var req in requests)
            {
                var existing = existingAttendances.FirstOrDefault(a => a.StudentId == req.StudentId);

                if (existing != null)
                {
                    existing.Status = req.Status;
                    existing.IsExcused = req.IsExcused;
                    existing.Note = req.Note;
                    existing.UpdatedAt = DateTime.UtcNow;
                    toUpdate.Add(existing);
                }
                else
                {
                    newAttendances.Add(new Attendance
                    {
                        AttendanceId = Guid.NewGuid(),
                        SessionId = sessionId,
                        StudentId = req.StudentId,
                        Status = req.Status,
                        IsExcused = req.IsExcused,
                        Note = req.Note,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (toUpdate.Any())
            {
                await _sessionRepository.UpdateRangeAsync(toUpdate);
            }

            if (newAttendances.Any())
            {
                await _sessionRepository.AddAttendancesAsync(newAttendances);
            }

            try
            {
                var studentsInClass = await _sessionRepository.GetStudentsForSessionAsync(sessionId);

                foreach (var req in requests)
                {
                    var studentInfo = studentsInClass.FirstOrDefault(s => s.StudentId == req.StudentId);
                    if (studentInfo != null)
                    {
                        string statusVietnamese = GetAttendanceStatusLabel(req.Status, req.IsExcused);

                        await _notificationService.SendNotificationAsync(
                            targetAccountId: studentInfo.Student.AccountId,
                            studentId: req.StudentId,
                            title: "Thông báo điểm danh",
                            content: $"Bạn đã được điểm danh: {statusVietnamese} trong buổi học '{session.Title}' ngày {session.Date:dd/MM/yyyy}.",
                            actionUrl: "/schedule",
                            type: "Attendance");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi thông báo điểm danh hàng loạt.");
            }
        }

        public async Task UpdateAttendanceAsync(Guid attendanceId, UpdateAttendanceDto request)
        {
            var attendance = await _sessionRepository.GetAttendanceByIdAsync(attendanceId);
            if (attendance == null)
            {
                throw new NotFoundException("Không tìm thấy bản ghi điểm danh.");
            }

            attendance.Status = request.Status;
            attendance.IsExcused = request.IsExcused;
            attendance.Note = request.Note;
            attendance.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAttendanceAsync(attendance);

            try
            {
                string statusVietnamese = GetAttendanceStatusLabel(request.Status, request.IsExcused);

                await _notificationService.SendNotificationAsync(
                    targetAccountId: attendance.Student.AccountId,
                    studentId: attendance.StudentId,
                    title: "Cập nhật điểm danh",
                    content: $"Trạng thái điểm danh của buổi học '{attendance.Session.Title}' ngày {attendance.Session.Date:dd/MM/yyyy} đã được cập nhật thành: {statusVietnamese}.",
                    actionUrl: "/schedule",
                    type: "Attendance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi thông báo cập nhật điểm danh.");
            }
        }

        public async Task<IEnumerable<ClassAttendanceHistoryDto>> GetClassAttendanceHistoryAsync(Guid classId)
        {
            var sessions = await _sessionRepository.GetSessionsByClassIdAsync(classId);
            var enrollments = await _classRepository.GetClassMemberAsync(classId);

            var attendancesBySession = new Dictionary<Guid, IEnumerable<Attendance>>();
            foreach (var session in sessions)
            {
                attendancesBySession[session.SessionId] = await _sessionRepository.GetAttendancesBySessionIdAsync(session.SessionId);
            }

            var result = new List<ClassAttendanceHistoryDto>();

            foreach (var enrollment in enrollments)
            {
                var historyDto = new ClassAttendanceHistoryDto
                {
                    StudentId = enrollment.StudentId,
                    FullName = enrollment.Student?.FullName ?? enrollment.Student?.Account?.FullName ?? "Unknown",
                    Avatar = enrollment.Student?.Account?.AvatarUrl,
                    Attendances = new List<StudentAttendanceRecordDto>()
                };

                foreach (var session in sessions)
                {
                    var attendanceList = attendancesBySession[session.SessionId];
                    var attendance = attendanceList.FirstOrDefault(a => a.StudentId == enrollment.StudentId);

                    historyDto.Attendances.Add(new StudentAttendanceRecordDto
                    {
                        SessionId = session.SessionId,
                        Date = session.Date,
                        StartTime = session.StartTime,
                        EndTime = session.EndTime,
                        Title = session.Title,
                        AttendanceId = attendance?.AttendanceId,
                        Status = attendance?.Status ?? "Not Taken",
                        IsExcused = attendance?.IsExcused,
                        Note = attendance?.Note
                    });
                }

                result.Add(historyDto);
            }

            return result;
        }
    }
}
