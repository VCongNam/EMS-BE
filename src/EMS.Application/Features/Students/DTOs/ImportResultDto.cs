using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class ImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<CreateStudentDto> SuccessList { get; set; } = new List<CreateStudentDto>();
        public List<ImportErrorDto> ErrorList { get; set; } = new List<ImportErrorDto>();
    }

    public class ImportErrorDto
    {
        public int RowNumber { get; set; }     
        public string StudentName { get; set; } 
        public string ErrorMessage { get; set; }
    }
}
