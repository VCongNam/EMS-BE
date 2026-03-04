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

        public async Task<Guid> CreateClassAsync(CreateClassRequest request)
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

    }
}
