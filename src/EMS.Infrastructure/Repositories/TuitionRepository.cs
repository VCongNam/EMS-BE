using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Repositories
{
    public class TuitionRepository : ITuitionRepository
    {
        private readonly ApplicationDbContext _context;
        public TuitionRepository(ApplicationDbContext context) 
        {
            _context = context;
        }

    }
}
