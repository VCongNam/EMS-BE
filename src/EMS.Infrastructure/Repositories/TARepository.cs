using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class TARepository : ITARepository
    {
        private readonly ApplicationDbContext _context;
        public TARepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<TeachingAssistantTask> CreateTaskAsync(TeachingAssistantTask task)
        {
            await _context.TeachingAssistantTasks.AddAsync(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<IEnumerable<TeachingAssistantTask>> GetTasksByClassTAIdAsync(Guid classTaId)
        {
            return await _context.TeachingAssistantTasks
                .Where(t => t.ClassTaid == classTaId)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<TeachingAssistant> GetTAByEmailAsync(string email)
        {
            var ta = await _context.TeachingAssistants
                .Include(ta => ta.Ta)
                .Where(ta => ta.Ta.IsDeleted == false)
                .FirstOrDefaultAsync(ta => ta.Ta.Email == email);
            return ta;
        }

        public async Task<IEnumerable<ClassTum>> GetTAsByTeacherIdAsync(Guid teacherId)
        {
            var result = await _context.ClassTa
                .Include(ct => ct.Class)
                .Include(ct => ct.Ta)
                    .ThenInclude(ta => ta.Ta)
                .Where(ct => ct.Class.TeacherId == teacherId)
                .ToListAsync();
            return result;
        public async Task<IEnumerable<TeachingAssistantTask>> GetTasksByTAIdAsync(Guid taId)
        {
            return await _context.TeachingAssistantTasks
                .Include(t => t.ClassTa) // Join sang bảng trung gian ClassTum
                .Where(t => t.ClassTa.Taid == taId) // Lọc theo TAID
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
