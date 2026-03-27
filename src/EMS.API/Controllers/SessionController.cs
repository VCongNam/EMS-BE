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
