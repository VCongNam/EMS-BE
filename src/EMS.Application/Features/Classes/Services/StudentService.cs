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
        public async Task<Guid> CreateStudentAsync(CreateStudentRequest request)
        {
            Guid newAccountId = Guid.NewGuid();

            var accountEntity = new Account
            {
                AccountID = newAccountId,
                RoleID = request.RoleID,
                Email = request.Email, // Have to hash
                PasswordHash = request.Password, // have to hash
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,

                Student = new Student
                {
                    StudentID = newAccountId,
                    ParentName = request.ParentName,
                    ParentPhone = request.ParentPhone,
                    ParentEmail = request.ParentEmail,
                    Address = request.Address,
                    DOB = request.DOB,
                }
            };
             
            await _accountRepository.CreatStudentAccountAsync(accountEntity);
            return newAccountId;
        }
    }
}
