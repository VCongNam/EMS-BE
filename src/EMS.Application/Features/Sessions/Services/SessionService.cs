using EMS.Application.Features.Sessions.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Features.Sessions.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;

        public SessionService(ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
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
                MeetingLink = s.MeetingLink,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            });
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
                        FullName = student.Student.StudentNavigation.FullName, 
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
                        FullName = student.Student.StudentNavigation.FullName,
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
