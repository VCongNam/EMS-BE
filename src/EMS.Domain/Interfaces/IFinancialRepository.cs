using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface IFinancialRepository
    {
        Task<Class?> GetClassInfoAsync(Guid classId);
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<IEnumerable<Invoice>> GetInvoicesByClassIdAsync(Guid classId);
        Task<IEnumerable<Transaction>> GetRecentTransactionsAsync(int count);
    }
}
