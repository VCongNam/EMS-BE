using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EMS.Application.Features.Students.Services
{
    public class StudentService : IStudentService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IClassRepository _classRepository;

        public StudentService(IAccountRepository accountRepository, ICurrentUserService currentUser, IClassRepository classRepository)
        {
            _accountRepository = accountRepository;
            _currentUser = currentUser;
            _classRepository = classRepository;
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

        public async Task<PagedResult<EnrolledClassDto>> GetMyClassesAsync(EnrolledClassFilter filter)
        {
            Guid studentId = _currentUser.UserId;

            var (entities, totalCount) = await _classRepository.GetClassByStudentIdAsync(studentId, filter.Page, filter.Size, filter.Status);
            var responseItems = entities.Select(ce => new EnrolledClassDto
            {
                ClassID = ce.ClassId,
                ClassName = ce.Class?.ClassName ?? "N/A",
                StartDate = (DateOnly)(ce.Class?.StartDate),
                EndDate = (DateOnly)(ce.Class?.EndDate),
                TeacherName = ce.Class?.Teacher.TeacherNavigation.FullName,
                EnrollmentStatus = ce.Status,
                EnrolledDate = (DateOnly)ce.EnrolledDate,
            }).ToList();
            return new PagedResult<EnrolledClassDto>
            {
                Items = responseItems,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size)
            };
        }
    }
}
