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
                .Where(t => t.ClassTaId == classTaId)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }
    }
}
