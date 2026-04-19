using EMS.Application.Features.Sessions.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.Sessions.Services
{
    public interface ISessionService
    {
        Task<IEnumerable<SessionDto>> GetSessionsByClassIdAsync(Guid classId);
        
        Task<IEnumerable<TeacherScheduleDto>> GetTeacherScheduleAsync(DateTime startDate, DateTime endDate);
        Task<SessionDto> CreateSessionAsync(CreateSessionDto request);
        Task<SessionDto> UpdateSessionAsync(Guid sessionId, UpdateSessionDto request);
        Task DeleteSessionAsync(Guid sessionId);

        Task<IEnumerable<AttendanceResponseDto>> GetAttendanceListAsync(Guid sessionId);
        Task TakeAttendanceBulkAsync(Guid sessionId, IEnumerable<TakeAttendanceDto> requests);
        Task UpdateAttendanceAsync(Guid attendanceId, UpdateAttendanceDto request);
        Task<IEnumerable<ClassAttendanceHistoryDto>> GetClassAttendanceHistoryAsync(Guid classId);
    }
}
