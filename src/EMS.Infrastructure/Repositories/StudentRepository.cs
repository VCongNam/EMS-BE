using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly ApplicationDbContext _context;
        public StudentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Student student)
        {
            await _context.Students.AddAsync(student);
        }

        public async Task<Student?> GetByIdAsync(Guid studentId)
        {
            return await _context.Students
                .Include(x => x.Account)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task<Student?> IsStudentExistAsync(Guid accountId, string name, DateOnly dob)
        {
            return await _context.Students
                .AsNoTracking() 
                .FirstOrDefaultAsync(s => s.AccountId == accountId &&
                    s.FullName.ToLower().Equals(name.ToLower()) &&
                    s.Dob == dob);
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
