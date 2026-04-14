using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.DTOs
{
    public class AssignMultipleStudentsDto
    {
        public List<Guid> StudentIds { get; set; } = new List<Guid>();
    }

    public class AssignMultipleResultDto
    {
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; } // Tổng số thêm mới + khôi phục
        public int ExistedCount { get; set; } // Tổng số đã có sẵn trong lớp
        public List<StudentAssignDetailDto> Details { get; set; } = new List<StudentAssignDetailDto>();
        public List<Guid> NonExistentStudentIds { get; set; } = new();
    }

    public class StudentAssignDetailDto
    {
        public Guid StudentId { get; set; }
        public string Status { get; set; } // Các trạng thái: "Added", "Restored", "AlreadyExists"
        public string Message { get; set; } = string.Empty;
    }
}
