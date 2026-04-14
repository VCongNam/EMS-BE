using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.SystemAdmin.Dtos;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Services
{
    public class SystemAdminService : ISystemAdminService
    {
        private readonly ISystemAdminRepository adminRepository;

        public SystemAdminService(ISystemAdminRepository repository)
        {
            this.adminRepository = repository;
        }

        public async Task<AdminDashboardDto> GetSystemDashboardAsync(DashboardFilterDto filter)
        {
            var end = filter.EndDate ?? DateTime.UtcNow;
            var start = filter.StartDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            if (start > end) { var temp = start; start = end; end = temp; }

            // 1. Chỉ số tuyệt đối (Vĩ mô toàn hệ thống)
            var teachers = await adminRepository.CountAccountsByRoleAsync("Teacher");
            var students = await adminRepository.CountAccountsByRoleAsync("Student");
            var tas = await adminRepository.CountAccountsByRoleAsync("TeachingAssistant"); // Hoặc Role tương ứng của TA

            var dashboard = new AdminDashboardDto
            {
                TotalUsers = teachers + students + tas,
                TotalTeachers = teachers,
                TotalStudents = students,
                TotalActiveClasses = await adminRepository.CountOngoingClassesAsync()
            };

            // 2. Chỉ số theo giai đoạn
            var accountsInPeriod = await adminRepository.GetAccountsInPeriodAsync(start, end);
            dashboard.NewRegistrationsInPeriod = accountsInPeriod.Count();

            var posts = await adminRepository.GetPostsInPeriodAsync(start, end);
            var assignments = await adminRepository.GetAssignmentsInPeriodAsync(start, end);
            var sessions = await adminRepository.GetSessionsInPeriodAsync(start, end);
            dashboard.EngagementInPeriod = posts.Count() + assignments.Count() + sessions.Count();

            // 3. Biểu đồ User Growth
            dashboard.UserGrowthChart = accountsInPeriod
                .Where(a => a.CreatedAt.HasValue)
                .GroupBy(a => a.CreatedAt!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new ChartDataDto
                {
                    Label = g.Key.ToString("dd/MM/yyyy"),
                    Value1 = g.Count(x => x.Role?.RoleName == "Teacher"),
                    Value2 = g.Count(x => x.Role?.RoleName == "Student")
                }).ToList();

            // 4. Biểu đồ System Usage (Tương tác)
            var allActivityDates = posts.Select(p => p.CreatedAt?.Date)
                .Concat(assignments.Select(a => a.CreatedAt?.Date))
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .Distinct()
                .OrderBy(d => d);

            foreach (var date in allActivityDates)
            {
                dashboard.SystemUsageChart.Add(new ChartDataDto
                {
                    Label = date.ToString("dd/MM/yyyy"),
                    Value1 = posts.Count(p => p.CreatedAt?.Date == date),
                    Value2 = assignments.Count(a => a.CreatedAt?.Date == date)
                });
            }

            return dashboard;
        }

        public async Task<IEnumerable<TeacherGridDto>> GetTeachersGridAsync(string? searchTerm, string? statusFilter)
        {
            var teachers = await adminRepository.GetAllTeachersGridAsync(searchTerm, statusFilter);

            return teachers.Select(t => new TeacherGridDto
            {
                TeacherId = t.TeacherId,
                AvatarUrl = t.TeacherNavigation?.AvatarUrl,
                FullName = t.TeacherNavigation?.FullName ?? "Unknown",
                PhoneNumber = t.TeacherNavigation?.PhoneNumber ?? string.Empty,
                Status = t.TeacherNavigation?.Status ?? "Active",
                JoinedDate = t.TeacherNavigation?.CreatedAt,

                ActiveClassesCount = t.Classes.Count,
                TotalStudentsCount = t.Classes.SelectMany(c => c.ClassEnrollments).Count()
            });
        }

        public async Task<TeacherDetailDto> GetTeacherDetailAsync(Guid teacherId)
        {
            var t = await adminRepository.GetTeacherByIdAsync(teacherId);
            if (t == null) throw new Exception("Không tìm thấy thông tin Giáo viên.");

            return new TeacherDetailDto
            {
                TeacherId = t.TeacherId,
                FullName = t.TeacherNavigation?.FullName ?? "Unknown",
                Email = t.TeacherNavigation?.Email ?? string.Empty,
                PhoneNumber = t.TeacherNavigation?.PhoneNumber,
                Specialization = t.Specialization,

                CurrentClasses = t.Classes.Select(c => new TeacherClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    StudentCount = c.ClassEnrollments.Count,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };
        }
    }
}
