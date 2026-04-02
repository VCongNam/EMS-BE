using EMS.Application.Features.Financials.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Financials.Services
{
    public interface IFinancialService
    {
        Task<PaymentReportDto> GetOverallPaymentReportAsync();
        Task<ClassFinancialDetailDto> GetClassFinancialDetailAsync(Guid classId);
    }
}
