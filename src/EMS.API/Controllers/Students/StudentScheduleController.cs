using EMS.Application.Features.Students.DTOs;
using EMS.Application.Features.Students.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.Students
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentScheduleController : ControllerBase
    {
        private readonly IStudentScheduleService _scheduleService;

        public StudentScheduleController(IStudentScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }


        // API Get Schedule: filter has classId,
        // if null return all schedule,
        // if not null return schedule of specific class
        [HttpGet]
        public async Task<IActionResult> GetMySchedules([FromQuery] ScheduleFilter filter)
        {
            var result = await _scheduleService.GetMySchedulesAsync(filter);

            return Ok(new
            {
                Message = "Lấy lịch học thành công",
                Data = result
            });
        }
    }
}
