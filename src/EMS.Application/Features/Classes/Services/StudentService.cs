using EMS.Application.Features.Classes.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Classes.Services
{
    public class StudentService : IStudentService
    {
        public readonly IAccountRepository _accountRepository;

        public StudentService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }
        public async Task<Guid> CreateStudentAsync(CreateStudentDto request)
        {
            var existingAccount = await _accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null) throw new Exception("Email đã được sử dụng!");

            var studentRole = await _accountRepository.GetRoleByNameAsync("Student");
            Guid newAccountId = Guid.NewGuid();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var accountEntity = new Account
            {
                AccountId = newAccountId,
                RoleId = studentRole.RoleId,
                Email = request.Email, // Have to hash
                PasswordHash = hashedPassword, // have to hash
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.Now,

                Student = new Student
                {
                    StudentId = newAccountId,
                    ParentName = request.ParentName,
                    ParentPhone = request.ParentPhone,
                    ParentEmail = request.ParentEmail,
                    Address = request.Address,
                    Dob = DateOnly.FromDateTime(request.DOB),
                }
            };
             
            await _accountRepository.AddAsync(accountEntity);
            return newAccountId;
        }
    }
}
