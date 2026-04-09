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

        public async Task<IEnumerable<Session>> GetSessionsByTeacherAndDateRangeAsync(Guid teacherId, DateOnly startDate, DateOnly endDate)
        {
            return await _context.Sessions
                .Include(s => s.Class)
                .Where(s => s.Class.TeacherId == teacherId 
                         && s.Date >= startDate 
                         && s.Date <= endDate 
                         && (s.IsDeleted == null || s.IsDeleted == false))
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Session>> GetSessionsByTeacherAndDateAsync(Guid teacherId, DateOnly date, Guid? excludeSessionId = null)
        {
            var query = _context.Sessions
                .Include(s => s.Class)
                .Where(s => s.Class.TeacherId == teacherId 
                         && s.Date == date 
                         && (s.IsDeleted == null || s.IsDeleted == false));

            if (excludeSessionId.HasValue)
            {
                query = query.Where(s => s.SessionId != excludeSessionId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task AddSessionAsync(Session session)
        {
            await _context.Sessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSessionAsync(Session session)
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(Session session)
        {
            session.Status = "Canceled";
            session.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesBySessionIdAsync(Guid sessionId)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                .ThenInclude(s => s.Account)
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
                .ThenInclude(s => s.Account)
                .Where(ce => ce.ClassId == session.ClassId && ce.Status == "Active")
                .ToListAsync();
        }

        public async Task<List<(Session Session, Attendance? Attendance)>> GetStudentSchedulesAsync( Guid studentId, DateTime fromDate, DateTime toDate, Guid? classId)
        {
            var query = _context.Sessions
                .Include(s => s.Class)
                .Where(s => s.Date >= DateOnly.FromDateTime(fromDate) && s.Date <= DateOnly.FromDateTime(toDate))
                .AsNoTracking();

            if (classId.HasValue)
            {
                query = query.Where(s => s.ClassId == classId.Value);
            }

            query = query.Where(s =>_context.ClassEnrollments.Any(ce =>
                ce.ClassId == classId && ce.StudentId == studentId));

            var dbResult = await query
                .OrderBy(s => s.Date) 
                .Select(s => new
                {
                    Session = s,
                    Attendance = _context.Attendances.FirstOrDefault(a =>
                        a.SessionId == s.SessionId &&
                        a.StudentId == studentId)
                })
                .ToListAsync();
            return dbResult.Select(x => (x.Session, x.Attendance)).ToList();
        }
    }
}
