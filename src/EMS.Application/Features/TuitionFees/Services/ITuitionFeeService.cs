using EMS.Application.Features.TuitionFees.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Services
{
    public interface ITuitionFeeService
    {
        Task UpdateTuitionFeeAsync(Guid classId, UpdateTuitionFeeDto request);
        Task UpdateTuitionDeadlineAsync(Guid classId, UpdateTuitionFeeDeadlineDto request);

        Task<bool> ReviewTransactionAsync(Guid transactionId, ReviewTransactionDto request);
    }
}
