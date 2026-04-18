using EMS.Application.Features.Reports.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.Services
{
    public interface ITeacherReportService
    {
        Task<TeacherGrowthReportResponse> GetGrowthReportAsync(DateTime startDate, DateTime endDate);
    }
}
