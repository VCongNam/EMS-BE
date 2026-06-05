using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Sessions.DTOs;
using EMS.Domain.Interfaces;

namespace EMS.Application.Features.Sessions.Services
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

        public async Task<List<StudentScheduleDto>> GetStudentSchedulesAsync(ScheduleFilter filter)
        {
            Guid studentId = _currentUser.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

            if (filter.FromDate > filter.ToDate)
            {
                throw new BadRequestException("Ngày bắt đầu phải trước ngày kết thúc.");
            }

            var now = DateTime.UtcNow.AddHours(7);
            var nowDate = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            var tuples = await _sessionRepository.GetStudentSchedulesAsync(
                studentId, filter.FromDate, filter.ToDate, filter.ClassID);

            var result = tuples.Select(t =>
            {
                var session = t.Session;
                var attendance = t.Attendance;

                string status;
                if (session.Date > nowDate)
                {
                    status = "Sắp diễn ra";
                }
                else if (session.Date < nowDate)
                {
                    status = "Đã kết thúc";
                }
                else
                {
                    if (nowTime < session.StartTime)
                    {
                        status = "Sắp diễn ra";
                    }
                    else if (nowTime >= session.StartTime && nowTime <= session.EndTime)
                    {
                        status = "Đang diễn ra";
                    }
                    else
                    {
                        status = "Đã kết thúc";
                    }
                }

                string attendanceStatus;
                if (attendance != null)
                {
                    attendanceStatus = attendance.Status switch
                    {
                        "Present" => "Có mặt",
                        "Absent" when attendance.IsExcused == true => "Vắng có phép",
                        "Absent" => "Vắng mặt",
                        _ => "Chưa điểm danh"
                    };
                }
                else
                {
                    attendanceStatus = (status == "Đang diễn ra" || status == "Sắp diễn ra")
                        ? "N/A"
                        : "Chưa điểm danh";
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
                    AttendanceStatus = attendanceStatus
                };
            }).ToList();

            return result;
        }
    }
}
