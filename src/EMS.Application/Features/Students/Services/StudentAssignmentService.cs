using EMS.Application.Common.Interfaces;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentAssignmentService : IStudentAssignmentService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IAssignmentRepository _assignmentRepository;
        public StudentAssignmentService(ICurrentUserService currentUser, IAssignmentRepository assignmentRepository)
        {
            _currentUser = currentUser;
            _assignmentRepository = assignmentRepository;
        }

    }
}
