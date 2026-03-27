using EMS.Application.Features.Sessions.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.Sessions.Services
{
    public interface ISessionService
    {
        Task<IEnumerable<SessionDto>> GetSessionsByClassIdAsync(Guid classId);
        
        Task<IEnumerable<AttendanceResponseDto>> GetAttendanceListAsync(Guid sessionId);
        Task TakeAttendanceBulkAsync(Guid sessionId, IEnumerable<TakeAttendanceDto> requests);
        Task UpdateAttendanceAsync(Guid attendanceId, UpdateAttendanceDto request);
    }
}
