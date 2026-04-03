using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Interfaces
{
    public interface ITuitionRepository
    {
        Task<(List<(Invoice Invoice, Transaction? LatestTransaction)> Items, int TotalCount)> GetStudentInvoicesAsync(
            Guid studentId, int page, int size, Guid? classId);
        Task<(Invoice? Invoice, Transaction? LatestTransaction, List<Attendance> Attendances)> GetInvoiceDetailAsync(Guid invoiceId, Guid studentId);
    }
}
