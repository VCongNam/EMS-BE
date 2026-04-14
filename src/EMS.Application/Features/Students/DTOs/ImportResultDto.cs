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
        public List<StudentImportSuccessDto> NewAccounts { get; set; } = new();
        public List<StudentImportSuccessDto> ExistedAccounts { get; set; } = new();
        public List<ImportErrorDto> ErrorList { get; set; } = new();

        public string? Base64ExcelReport { get; set; }
    }

    public class StudentImportSuccessDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ImportErrorDto
    {
        public int RowNumber { get; set; }     
        public string StudentName { get; set; } 
        public string ErrorMessage { get; set; }
    }
}
