using EMS.Application.Features.Sessions.DTOs;
using EMS.Application.Features.Sessions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IStudentScheduleService _scheduleService;

        public SessionController(ISessionService sessionService, IStudentScheduleService scheduleService)
        {
            _sessionService = sessionService;
            _scheduleService = scheduleService;
        }

        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetSessionsByClassId(Guid classId)
        {
            var sessions = await _sessionService.GetSessionsByClassIdAsync(classId);
            return Ok(sessions);
        }

        [HttpGet("{sessionId:guid}")]
        public async Task<IActionResult> GetSessionDetail(Guid sessionId)
        {
            var session = await _sessionService.GetSessionDetailAsync(sessionId);
            return Ok(session);
        }

        [HttpGet("teacher-schedule")]
        public async Task<IActionResult> GetTeacherSchedule([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var schedule = await _sessionService.GetTeacherScheduleAsync(startDate, endDate);
            return Ok(schedule);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto request)
        {
            var session = await _sessionService.CreateSessionAsync(request);
            return Ok(session);
        }

        [HttpPut("{sessionId}")]
        public async Task<IActionResult> UpdateSession(Guid sessionId, [FromBody] UpdateSessionDto request)
        {
            var session = await _sessionService.UpdateSessionAsync(sessionId, request);
            return Ok(session);
        }

        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            await _sessionService.DeleteSessionAsync(sessionId);
            return Ok(new { Message = "Session deleted successfully." });
        }

        [HttpGet("{sessionId}/attendance")]
        public async Task<IActionResult> GetAttendanceList(Guid sessionId)
        {
            var attendances = await _sessionService.GetAttendanceListAsync(sessionId);
            return Ok(attendances);
        }

        [HttpPost("{sessionId}/attendance")]
        public async Task<IActionResult> TakeAttendance(Guid sessionId, [FromBody] List<TakeAttendanceDto> requests)
        {
            await _sessionService.TakeAttendanceBulkAsync(sessionId, requests);
            return Ok(new { Message = "Attendance saved successfully." });
        }

        [HttpPut("attendance/{attendanceId}")]
        public async Task<IActionResult> UpdateAttendance(Guid attendanceId, [FromBody] UpdateAttendanceDto request)
        {
            await _sessionService.UpdateAttendanceAsync(attendanceId, request);
            return Ok(new { Message = "Attendance updated successfully." });
        }

        [HttpGet("student/schedule")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentSchedules([FromQuery] ScheduleFilter filter)
        {
            var result = await _scheduleService.GetStudentSchedulesAsync(filter);

            return Ok(new
            {
                Message = "Lấy lịch học thành công",
                Data = result
            });
        }

        [HttpGet("class/{classId}/attendance-history")]
        public async Task<IActionResult> GetClassAttendanceHistory(Guid classId)
        {
            var history = await _sessionService.GetClassAttendanceHistoryAsync(classId);
            return Ok(history);
        }
    }
}
