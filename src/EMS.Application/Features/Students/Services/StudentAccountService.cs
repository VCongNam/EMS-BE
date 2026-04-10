using ClosedXML.Excel;
using DocumentFormat.OpenXml.VariantTypes;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentAccountService : IStudentAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IStudentRepository _studentRepository;
        public StudentAccountService(IAccountRepository accountRepository, IStudentRepository studentRepository)
        {
            _accountRepository = accountRepository;
            _studentRepository = studentRepository;
        }
        public async Task<Guid> CreateStudentAsync(CreateStudentDto request)
        {
            Guid accountIdToUse;
            var isAccountExisted = await _accountRepository.GetByPhoneAsync(request.PhoneNumber);


            //Tạo accocunt mới
            if (isAccountExisted == null)
            {
                var studentRole = await _accountRepository.GetRoleByNameAsync("Student");
                accountIdToUse = Guid.NewGuid();
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var accountEntity = new Account
                {
                    AccountId = accountIdToUse,
                    RoleId = studentRole.RoleId,
                    PasswordHash = hashedPassword,
                    PhoneNumber = request.PhoneNumber,
                    Status = "Unverified",
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                };
                await _accountRepository.AddAsync(accountEntity);
            } else
            {
                accountIdToUse = isAccountExisted.AccountId;
            }

            var isDuplicate = await _studentRepository.IsStudentExistAsync(accountIdToUse, request.FullName, DateOnly.FromDateTime(request.DOB));
            if (isDuplicate)
            {
                throw new Exception("Hồ sơ học sinh đã tồn tại");
            }
            Guid newStudentId = Guid.NewGuid();
            var studentProfile = new Student
            {
                StudentId = newStudentId,
                AccountId = accountIdToUse,
                FullName = request.FullName,
                Dob = DateOnly.FromDateTime(request.DOB),
                Address = request.Address
            };
            await _studentRepository.AddAsync(studentProfile);
            return newStudentId;
        }

        public async Task<ImportResultDto> ImportStudentsFromExcelAsync(IFormFile excelFile)
        {
            var result = new ImportResultDto();
            if (excelFile == null || excelFile.Length == 0)
            {
                throw new Exception("File không được để trống.");
            }

            var extension = Path.GetExtension(excelFile.FileName).ToLower();
            if (extension != ".xlsx")
                throw new Exception("Hệ thống chỉ hỗ trợ file Excel định dạng .xlsx");
            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream);

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RowsUsed().Skip(1);

            result.TotalRows = rows.Count();
            foreach (var row in rows)
            {
                int rowNumber = row.RowNumber();
                string studentName = row.Cell(1).GetString().Trim();
                try
                {
                    string phone = row.Cell(2).GetString().Trim();
                    string dob = row.Cell(3).GetString().Trim();
                    string address = row.Cell(4).GetString().Trim();
                    if (string.IsNullOrEmpty(studentName)) throw new Exception("Tên học sinh không được để trống.");
                    if (string.IsNullOrEmpty(phone)) throw new Exception("Số điện thoại phụ huynh bắt buộc nhập.");
                    if (!DateTime.TryParse(dob, out DateTime birthDate)) throw new Exception("Ngày sinh không đúng định dạng (VD: 01/12/2010).");

                    var createStudentDto = new CreateStudentDto
                    {
                        FullName = studentName,
                        Password = "123456",
                        DOB = birthDate,
                        Address = address,
                        PhoneNumber = phone,
                    };
                    await CreateStudentAsync(createStudentDto);
                    result.SuccessCount++;
                } catch (Exception ex)
                {
                    result.FailedCount++;
                    result.ErrorList.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        StudentName = string.IsNullOrEmpty(studentName) ? "Không xác định" : studentName,
                        ErrorMessage = ex.Message
                    });
                }
            }
            return result;
        }
    }
}
