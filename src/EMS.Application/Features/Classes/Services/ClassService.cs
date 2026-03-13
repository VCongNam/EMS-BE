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
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepository;

        public ClassService(IClassRepository classRepository)
        {
            _classRepository = classRepository;
        }

        public async Task<Guid> CreateClassAsync(CreateClassDto request)
        {
            var newClass = new Class
            {
                ClassId = Guid.NewGuid(),
                TeacherId = request.TeacherId,
                ClassName = request.ClassName,
                Room = request.Room,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TuitionFee = request.TuitionFee,
                Status = "Scheduled",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _classRepository.AddAsync(newClass);

            return newClass.ClassId;
        }
        public async Task<IEnumerable<ClassMemberResponse>> GetClassMembersAsync(Guid classId)
        {
            var enrollments = await _classRepository.GetClassMemberAsync(classId);


            var memberList = enrollments.Select(ce => new ClassMemberResponse
            {
                StudentID = ce.StudentID,
                FullName = ce.Student.Account.FullName,
                Email = ce.Student.Account.Email,
                ParentName = ce.Student.ParentName,
                ParentPhone = ce.Student.ParentPhone,
                EnrolledDate = ce.EnrolledDate,
                Status = ce.Status
            }).ToList();

            return memberList;
        }

        public async Task<bool> AssignStudentAsync(Guid classId, AssignStudentRequest request)
        {
            bool isEnrolled = await _classRepository.IsStudentAlreadyEnrolledAsync(classId, request.StudentID);
            if (isEnrolled)
            {
                throw new Exception("Student is assigned to this class");
            }
            var newEnrollment = new ClassEnrollment
            {
                EnrollmentID = Guid.NewGuid(),
                ClassID = classId,
                StudentID = request.StudentID,
                EnrolledDate = DateTime.UtcNow,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            await _classRepository.AddEnrollmentAsync(newEnrollment);
            return true;
        }
        public async Task<IEnumerable<ClassSummaryDto>> GetTeacherDashboardAsync(Guid teacherId)
        {
            var classes = await _classRepository.GetClassesByTeacherIdAsync(teacherId);

            var result = classes.Select(c => new ClassSummaryDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                Room = c.Room,
                Status = c.Status,
                StartDate = c.StartDate
            });

            return result;
        }

        public async Task<ClassDetailDto> GetClassDetailAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);

            if (classroom == null)
            {
                throw new Exception($"Class with ID {classId} not found.");
            }

            return new ClassDetailDto
            {
                ClassId = classroom.ClassId,
                TeacherId = classroom.TeacherId,
                ClassName = classroom.ClassName,
                Room = classroom.Room,
                TuitionFee = classroom.TuitionFee,
                StartDate = classroom.StartDate,
                EndDate = classroom.EndDate,
                Status = classroom.Status,
                CreatedAt = (DateTime)classroom.CreatedAt
            };
        }

       // Update Class
        public async Task UpdateClassAsync(Guid classId, UpdateClassDto request)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null)
            {
                throw new Exception($"Class with ID {classId} not found.");
            }

            classroom.ClassName = request.ClassName;
            classroom.Room = request.Room;
            classroom.StartDate = request.StartDate;
            classroom.EndDate = request.EndDate;
            classroom.TuitionFee = request.TuitionFee;

            classroom.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateAsync(classroom);
        }

        //  Archive Class 
        public async Task ArchiveClassAsync(Guid classId)
        {
            var classroom = await _classRepository.GetByIdAsync(classId);
            if (classroom == null)
            {
                throw new Exception($"Class with ID {classId} not found.");
            }

            classroom.Status = "Archived";
            classroom.UpdatedAt = DateTime.UtcNow;

            await _classRepository.UpdateAsync(classroom);
        }

    }
}
