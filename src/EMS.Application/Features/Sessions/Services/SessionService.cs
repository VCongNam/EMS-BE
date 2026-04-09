using EMS.Application.Features.Sessions.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Application.Common.Interfaces;

namespace EMS.Application.Features.Sessions.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;

        public SessionService(
            ISessionRepository sessionRepository, 
            IClassRepository classRepository, 
            ICurrentUserService currentUserService)
        {
            _sessionRepository = sessionRepository;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
        }

        private async Task CheckSessionConflictAsync(Guid teacherId, DateOnly date, TimeOnly? startTime, TimeOnly? endTime, Guid? excludeSessionId = null)
        {
            if (!startTime.HasValue || !endTime.HasValue)
                return;

            var existingSessions = await _sessionRepository.GetSessionsByTeacherAndDateAsync(teacherId, date, excludeSessionId);

            bool isOverlap = existingSessions.Any(s => 
                s.StartTime.HasValue && s.EndTime.HasValue &&
                (
                    (startTime.Value >= s.StartTime.Value && startTime.Value < s.EndTime.Value) ||
                    (endTime.Value > s.StartTime.Value && endTime.Value <= s.EndTime.Value) ||
                    (startTime.Value <= s.StartTime.Value && endTime.Value >= s.EndTime.Value)
                )
            );

            if (isOverlap)
            {
                throw new Exception("Lịch học bị trùng với một buổi học khác của bạn trong cùng thời gian.");
            }
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

        public async Task<IEnumerable<TeacherScheduleDto>> GetTeacherScheduleAsync(DateTime startDate, DateTime endDate)
        {
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
                throw new Exception($"Class with ID {request.ClassId} not found.");
            }

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
            {
                throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc.");
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
                throw new Exception($"Session with ID {sessionId} not found.");
            }

            var classObj = await _classRepository.GetByIdAsync(session.ClassId);
            var teacherId = classObj?.TeacherId ?? Guid.Empty;

            if (request.StartTime.HasValue && request.EndTime.HasValue && request.StartTime >= request.EndTime)
            {
                throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc.");
            }

            await CheckSessionConflictAsync(teacherId, request.Date, request.StartTime, request.EndTime, sessionId);

            session.Title = request.Title;
            session.Date = request.Date;
            session.StartTime = request.StartTime;
            session.EndTime = request.EndTime;
            session.MeetingLink = request.MeetingLink;
            session.Topic = request.Topic;
            session.Note = request.Note;
            session.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateSessionAsync(session);

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
                throw new Exception($"Session with ID {sessionId} not found.");
            }

            await _sessionRepository.DeleteSessionAsync(session);
        }

        public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceListAsync(Guid sessionId)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new Exception($"Session with ID {sessionId} not found.");
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
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                throw new Exception($"Session with ID {sessionId} not found.");
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
                await _sessionRepository.UpdateRangeAsync(toUpdate);

            if (newAttendances.Any())
                await _sessionRepository.AddAttendancesAsync(newAttendances);
        }

        public async Task UpdateAttendanceAsync(Guid attendanceId, UpdateAttendanceDto request)
        {
            var attendance = await _sessionRepository.GetAttendanceByIdAsync(attendanceId);
            if (attendance == null)
            {
                throw new Exception($"Attendance record with ID {attendanceId} not found.");
            }

            attendance.Status = request.Status;
            attendance.IsExcused = request.IsExcused;
            attendance.Note = request.Note;
            attendance.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAttendanceAsync(attendance);
        }
    }
}
