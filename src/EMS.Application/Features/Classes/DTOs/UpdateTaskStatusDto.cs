using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class UpdateTaskStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class ReviewTaskDto
    {
        public bool IsApproved { get; set; } // true: Duyệt, false: Từ chối
        public string? Feedback { get; set; } // Nhận xét lý do từ chối (nếu có)
    }
}
