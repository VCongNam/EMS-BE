using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Dtos
{
    public class TuitionDashboardDto
    {
        // 3 Thẻ Card tổng quát
        public decimal TotalExpected { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDebt { get; set; }

        // Dữ liệu biểu đồ Tỷ trọng (Pie Chart) - Lấy theo tháng chọn
        public List<ClassRevenueDto> ProportionByClass { get; set; } = new();

        // Dữ liệu biểu đồ Xu hướng (Line Chart) - Lấy 6 tháng gần nhất
        public List<DailyRevenueDto> DailyTrend { get; set; } = new();
    }
}
