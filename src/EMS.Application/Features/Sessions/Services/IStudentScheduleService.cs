using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Sessions.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Sessions.Services
{
    public interface IStudentScheduleService
    {
        Task<List<StudentScheduleDto>> GetStudentSchedulesAsync(ScheduleFilter filter);
    }
}
