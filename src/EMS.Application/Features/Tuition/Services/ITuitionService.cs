using EMS.Application.Features.Tuition.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tuition.Services
{
    public interface ITuitionService
    {
        Task<bool> ReviewTransactionAsync(Guid transactionId, ReviewTransactionDto request);
    }
}
