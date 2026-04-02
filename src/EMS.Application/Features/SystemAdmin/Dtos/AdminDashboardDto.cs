using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.SystemAdmin.Dtos
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTAs { get; set; }
        public int TotalActiveClasses { get; set; }
        public int NewRegistrationsThisMonth { get; set; }
    }
}
