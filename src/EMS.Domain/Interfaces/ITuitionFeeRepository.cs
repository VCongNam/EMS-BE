using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ITuitionFeeRepository
    {
        Task<Class?> GetClassByIdAsync(Guid classId);
        Task UpdateClassAsync(Class classEntity);

        Task<IEnumerable<Invoice>> GetInvoicesByClassAndPeriodAsync(Guid classId, int month, int year);
        Task UpdateInvoicesAsync(IEnumerable<Invoice> invoices);
    }
}
