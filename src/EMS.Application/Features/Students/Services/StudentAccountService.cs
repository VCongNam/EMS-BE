using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
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
    }
}
