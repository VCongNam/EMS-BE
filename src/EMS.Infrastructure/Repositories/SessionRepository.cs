using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly ApplicationDbContext _context;

        public SessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Session>> GetSessionsByClassIdAsync(Guid classId)
        {
            return await _context.Sessions
                .Where(s => s.ClassId == classId && (s.IsDeleted == null || s.IsDeleted == false))
                .OrderBy(s => s.Date)
                .ToListAsync();
        }

        public async Task<Session?> GetSessionByIdAsync(Guid sessionId)
        {
            return await _context.Sessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && (s.IsDeleted == null || s.IsDeleted == false));
        }

        public async Task AddSessionsAsync(IEnumerable<Session> sessions)
        {
            await _context.Sessions.AddRangeAsync(sessions);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesBySessionIdAsync(Guid sessionId)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                .ThenInclude(s => s.StudentNavigation)
                .Where(a => a.SessionId == sessionId)
                .ToListAsync();
        }

        public async Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId)
        {
            return await _context.Attendances
                .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);
        }

        public async Task AddAttendancesAsync(IEnumerable<Attendance> attendances)
        {
            await _context.Attendances.AddRangeAsync(attendances);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAttendanceAsync(Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<Attendance> attendances)
        {
            _context.Attendances.UpdateRange(attendances);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ClassEnrollment>> GetStudentsForSessionAsync(Guid sessionId)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                return new List<ClassEnrollment>();
            }

            return await _context.ClassEnrollments
                .Include(ce => ce.Student)
                .ThenInclude(s => s.StudentNavigation)
                .Where(ce => ce.ClassId == session.ClassId && ce.Status == "Active")
                .ToListAsync();
        }
    }
}
