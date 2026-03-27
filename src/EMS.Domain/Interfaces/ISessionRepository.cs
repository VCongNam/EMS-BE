using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ISessionRepository
    {
        Task<IEnumerable<Session>> GetSessionsByClassIdAsync(Guid classId);
        Task<Session?> GetSessionByIdAsync(Guid sessionId);
        Task AddSessionsAsync(IEnumerable<Session> sessions);

        Task<IEnumerable<Attendance>> GetAttendancesBySessionIdAsync(Guid sessionId);
        Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId);
        Task AddAttendancesAsync(IEnumerable<Attendance> attendances);
        Task UpdateAttendanceAsync(Attendance attendance);
        Task UpdateRangeAsync(IEnumerable<Attendance> attendances);
        
        Task<IEnumerable<ClassEnrollment>> GetStudentsForSessionAsync(Guid sessionId);
    }
}
