using ClosedXML.Excel;
using DocumentFormat.OpenXml.VariantTypes;
using EMS.Application.Common.Helpers;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Accounts.Services
{
    public class StudentAccountService : IStudentAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICurrentUserService _currentUser;
        private static readonly Random _random = new Random();
        public StudentAccountService(IAccountRepository accountRepository, IStudentRepository studentRepository, ICurrentUserService currentUser)
        {
            _accountRepository = accountRepository;
            _studentRepository = studentRepository;
            _currentUser = currentUser;
        }
        public async Task<(Guid StudentId, string? InitialPassword, bool IsNewAccount)> CreateStudentAsync(CreateStudentDto request)
        {
            string phone = request.PhoneNumber?.Trim() ?? "";
            string fullName = request.FullName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(fullName)) throw new Exception("Tên học sinh không được để trống.");
            if (!DataValidator.IsValidPhoneNumber(phone)) throw new Exception("Số điện thoại không hợp lệ.");

            var isAccountExisted = await _accountRepository.GetByPhoneAsync(phone);
            Guid accountIdToUse;
            bool isNew = false;
            string? rawPassword = null;

            string lastFourDigits = phone.Length >= 4 ? phone.Substring(phone.Length - 4) : _random.Next(1000, 10000).ToString();
           
            //Tạo accocunt mới
            if (isAccountExisted == null)
            {
                if (string.IsNullOrWhiteSpace(request.Password) || !DataValidator.IsValidPassword(request.Password))
                    throw new Exception("Mật khẩu tạo mới không đủ độ phức tạp (8 ký tự, 1 hoa, 1 ký tự đặc biệt).");

                isNew = true;
                rawPassword = request.Password.Trim();
                var studentRole = await _accountRepository.GetRoleByNameAsync("Student");

                accountIdToUse = Guid.NewGuid();
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var accountEntity = new Account
                {
                    AccountId = accountIdToUse,
                    Email = $"user_{phone}@ems.internal",
                    FullName = $"Account{lastFourDigits}",
                    RoleId = studentRole.RoleId,
                    PasswordHash = hashedPassword,
                    PhoneNumber = phone,
                    Status = "Unverified",
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                };
                await _accountRepository.AddAsync(accountEntity);
            } else
            {
                accountIdToUse = isAccountExisted.AccountId;
            }

            //Check stuednt in Account
            var dob = DateOnly.FromDateTime(request.DOB);
            var existingStudent = await _studentRepository.IsStudentExistAsync(accountIdToUse, fullName, dob);
            if (existingStudent != null)
            {
                return (existingStudent.StudentId, null, false);
            }

            Guid newStudentId = Guid.NewGuid();
            var studentProfile = new Student
            {
                StudentId = newStudentId,
                AccountId = accountIdToUse,
                FullName = fullName,
                Dob = dob,
                Address = request.Address
            };
            await _studentRepository.AddAsync(studentProfile);
            await _studentRepository.SaveChangesAsync();
            return (newStudentId, rawPassword, isNew);
        }

        private async Task<(Guid StudentId, string? InitialPassword, bool IsNewAccount)> ProcessStudentImportAsync(
    CreateStudentDto request, Guid studentRoleId)
        {
            string phone = request.PhoneNumber?.Trim() ?? "";
            string fullName = request.FullName?.Trim() ?? "";

            var isAccountExisted = await _accountRepository.GetByPhoneAsync(phone);
            Guid accountIdToUse;
            bool isNew = false;
            string? rawPassword = null;

            if (isAccountExisted == null)
            {
                isNew = true;
                rawPassword = request.Password;

                accountIdToUse = Guid.NewGuid();
                var accountEntity = new Account
                {
                    AccountId = accountIdToUse,
                    Email = $"student_{phone}@ems.internal", 
                    FullName = fullName,
                    RoleId = studentRoleId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                    PhoneNumber = phone,
                    Status = "Unverified",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                };

                await _accountRepository.AddAsync(accountEntity);
            }
            else
            {
                accountIdToUse = isAccountExisted.AccountId;
            }

            var dob = DateOnly.FromDateTime(request.DOB);
            var existingStudent = await _studentRepository.IsStudentExistAsync(accountIdToUse, fullName, dob);

            if (existingStudent != null)
            {
                return (existingStudent.StudentId, null, false);
            }

            Guid newStudentId = Guid.NewGuid();
            var studentProfile = new Student
            {
                StudentId = newStudentId,
                AccountId = accountIdToUse,
                FullName = fullName,
                Dob = dob,
                Address = request.Address
            };

            await _studentRepository.AddAsync(studentProfile);

            return (newStudentId, rawPassword, isNew);
        }

        public async Task<ImportResultDto> ImportStudentsFromExcelAsync(IFormFile excelFile)
        {
            var studentRole = await _accountRepository.GetRoleByNameAsync("Student");
            var studentRoleId = studentRole.RoleId;
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
                    string phone = row.Cell(2).GetValue<string>()?.Trim();
                    string dob = row.Cell(3).GetString().Trim();
                    string address = row.Cell(4).GetValue<string>()?.Trim();

                    if (string.IsNullOrWhiteSpace(studentName))
                        throw new Exception("Tên học sinh không được để trống.");

                    if (string.IsNullOrWhiteSpace(phone) || !DataValidator.IsValidPhoneNumber(phone))
                        throw new Exception("Số điện thoại không được để trống và phải đúng định dạng.");

                    if (!DateTime.TryParse(dob, out DateTime birthDate))
                        throw new Exception("Ngày sinh không đúng định dạng.");


                    string studentFirstName = studentName.Split(' ').Last();
                    string unaccentedName = DataValidator.RemoveVietnameseSigns(studentFirstName);
                    string padding = unaccentedName.Length < 3 ? "Student" : "";
                    string generatedPassword = $"{padding}{unaccentedName}@{phone.Substring(Math.Max(0, phone.Length - 4))}";

                    var createStudentDto = new CreateStudentDto
                    {
                        FullName = studentName,
                        Password = generatedPassword,
                        DOB = birthDate,
                        Address = address,
                        PhoneNumber = phone,
                    };
                    var (sId, psw, isNew) = await ProcessStudentImportAsync(createStudentDto, studentRoleId);

                    var successDto = new StudentImportSuccessDto
                    {
                        StudentId = sId,
                        FullName = studentName,
                        PhoneNumber = phone,
                        Password = psw
                    };

                    if (isNew) result.NewAccounts.Add(successDto);
                    else result.ExistedAccounts.Add(successDto);
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

            if (result.SuccessCount > 0)
            {
                await _studentRepository.SaveChangesAsync();
            }

            var excelBytes = ExportImportResultToExcel(result);
            result.Base64ExcelReport = Convert.ToBase64String(excelBytes);

            return result;
        }

        public byte[] ExportImportResultToExcel(ImportResultDto result)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Ket_Qua_Import");

                var headers = new string[] { "Họ tên", "Số Điện Thoại", "Trạng thái tài khoản", "Mật Khẩu Mặc Định", "Ghi Chú/Lỗi" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                int currentRow = 2;

                foreach (var item in result.NewAccounts)
                {
                    worksheet.Cell(currentRow, 1).Value = item.FullName;
                    worksheet.Cell(currentRow, 2).Value = item.PhoneNumber;
                    worksheet.Cell(currentRow, 3).Value = "Tạo mới";
                    worksheet.Cell(currentRow, 4).Value = item.Password; // Hiển thị pass để giáo viên gửi cho học sinh
                    worksheet.Cell(currentRow, 5).Value = "Thành công";
                    worksheet.Cell(currentRow, 5).Style.Font.FontColor = XLColor.Green;
                    currentRow++;
                }

                foreach (var item in result.ExistedAccounts)
                {
                    worksheet.Cell(currentRow, 1).Value = item.FullName;
                    worksheet.Cell(currentRow, 2).Value = item.PhoneNumber;
                    worksheet.Cell(currentRow, 3).Value = "Đã có sẵn";
                    worksheet.Cell(currentRow, 4).Value = "********"; // Không hiện pass cũ vì lý do bảo mật
                    worksheet.Cell(currentRow, 5).Value = "Thành công (Dùng lại account cũ)";
                    currentRow++;
                }

                foreach (var item in result.ErrorList)
                {
                    worksheet.Cell(currentRow, 1).Value = item.StudentName;
                    worksheet.Cell(currentRow, 5).Value = $"Lỗi dòng {item.RowNumber}: {item.ErrorMessage}";
                    worksheet.Cell(currentRow, 5).Style.Font.FontColor = XLColor.Red;
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<bool> ResetStudentPasswordAsync(Guid studentId, string newPassword)
        {
            var teacherId = _currentUser.UserId;
            var currentUserRole = _currentUser.Role;
            if (currentUserRole != "Teacher") throw new Exception("Bạn phải là giáo viên để thực hiện hành động này");

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hồ sơ học sinh.");
            }
            return true;

        }
    }
}
