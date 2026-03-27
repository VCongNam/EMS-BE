using EMS.Application.Features.Sessions.DTOs;
using EMS.Application.Features.Sessions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        // [GET] /api/session/class/{classId}
        [HttpGet("session/class/{classId}")]
        public async Task<IActionResult> GetSessionsByClassId(Guid classId)
        {
            try
            {
                var sessions = await _sessionService.GetSessionsByClassIdAsync(classId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // [GET] /api/session/teacher-schedule
        [HttpGet("session/teacher-schedule")]
        public async Task<IActionResult> GetTeacherSchedule([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var schedule = await _sessionService.GetTeacherScheduleAsync(startDate, endDate);
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // [POST] /api/session
        [HttpPost("session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto request)
        {
            try
            {
                var session = await _sessionService.CreateSessionAsync(request);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // [PUT] /api/session/{sessionId}
        [HttpPut("session/{sessionId}")]
        public async Task<IActionResult> UpdateSession(Guid sessionId, [FromBody] UpdateSessionDto request)
        {
            try
            {
                var session = await _sessionService.UpdateSessionAsync(sessionId, request);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // [DELETE] /api/session/{sessionId}
        [HttpDelete("session/{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            try
            {
                await _sessionService.DeleteSessionAsync(sessionId);
                return Ok(new { Message = "Session deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // [GET] /api/session/{sessionId}/attendance
        [HttpGet("session/{sessionId}/attendance")]
        public async Task<IActionResult> GetAttendanceList(Guid sessionId)
        {
            try
            {
                var attendances = await _sessionService.GetAttendanceListAsync(sessionId);
                return Ok(attendances);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // [POST] /api/session/{sessionId}/attendance
        [HttpPost("session/{sessionId}/attendance")]
        public async Task<IActionResult> TakeAttendance(Guid sessionId, [FromBody] IEnumerable<TakeAttendanceDto> requests)
        {
            try
            {
                await _sessionService.TakeAttendanceBulkAsync(sessionId, requests);
                return Ok(new { Message = "Attendance saved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // [PUT] /api/session/attendance/{attendanceId}
        [HttpPut("session/attendance/{attendanceId}")]
        public async Task<IActionResult> UpdateAttendance(Guid attendanceId, [FromBody] UpdateAttendanceDto request)
        {
            try
            {
                await _sessionService.UpdateAttendanceAsync(attendanceId, request);
                return Ok(new { Message = "Attendance updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
