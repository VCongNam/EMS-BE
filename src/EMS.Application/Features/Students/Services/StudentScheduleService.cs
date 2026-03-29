using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentScheduleService : IStudentScheduleService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ICurrentUserService _currentUser;

        public StudentScheduleService(ISessionRepository sesionRepository, ICurrentUserService currentUser)
        {
            _sessionRepository = sesionRepository;
            _currentUser = currentUser;
        }

        public async Task<List<StudentScheduleDto>> GetMySchedulesAsync(ScheduleFilter filter)
        {
            Guid studentId = _currentUser.UserId;

            if (filter.FromDate > filter.ToDate)
            {
                throw new ArgumentException("Ngày bắt đầu phải trước ngày kết thúc!");
            }
            var tuples = await _sessionRepository.GetStudentSchedulesAsync(
                studentId, filter.FromDate, filter.ToDate, filter.ClassID);
            var result = tuples.Select(t =>
            {
                var session = t.Session;
                var attendance = t.Attendance;

                string status;
                if(session.Date > DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    status = "Sắp diễn ra";
                } else
                {
                    if(attendance != null)
                    {
                        status = attendance.Status switch
                        {
                            "Present" => "Có mặt",
                            "Absent" => "Vắng mặt",
                            "Excused" => "Vắng có phép",
                            _ => "Không xác định"
                        };
                    } 
                    else
                    {
                        status = "Chưa điểm danh";
                    }
                }
                return new StudentScheduleDto
                {
                    SessionID = session.SessionId,
                    ClassID = session.ClassId,
                    ClassName = session.Class?.ClassName ?? "Lớp học",
                    Date = session.Date,
                    MeetingLink = session.MeetingLink,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    Status = status,
                };
            }).ToList();

            return result;
        }
    }
}
