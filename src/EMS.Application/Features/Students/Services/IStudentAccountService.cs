using EMS.Application.Features.Students.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public interface IStudentAccountService
    {
        Task<Guid> CreateStudentAsync(CreateStudentDto request);
        Task<ImportResultDto> ImportStudentsFromExcelAsync(IFormFile excelFile);
    }
}
