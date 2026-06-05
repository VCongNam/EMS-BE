using EMS.Application.Features.Accounts.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Services
{
    public interface IStudentAccountService
    {
        Task<(Guid StudentId, string? InitialPassword, bool IsNewAccount)> CreateStudentAsync(CreateStudentDto request);
        Task<ImportResultDto> ImportStudentsFromExcelAsync(IFormFile excelFile);

        byte[] ExportImportResultToExcel(ImportResultDto result);

        Task<bool> ResetStudentPasswordAsync(Guid studentId, string newPassword);
    }
}
