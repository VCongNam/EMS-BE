using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentTuitionService : IStudentTuitionService
    {
        private readonly ITuitionRepository _tuitionRepository;
        private readonly ICurrentUserService _currentUserService;
        public StudentTuitionService(ITuitionRepository tuitionRepository, ICurrentUserService currentUserService)
        {
            _tuitionRepository = tuitionRepository;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResult<TuitionDto>> GetMyTuitionAsync(TuitionFilter filter)
        {
            Guid studentId = _currentUserService.UserId;

            var (tuples, totalCount) = await _tuitionRepository.GetStudentInvoicesAsync(studentId, filter.Page, filter.Size, filter.ClassID);
            var items = tuples.Select(t =>
            {
                var invoice = t.Invoice;
                var transaction = t.LatestTransaction;

                string status;
                bool canPay = false;

                if (invoice.Status == "Paid")
                {
                    status = "Đã nộp";
                }
                else if (transaction != null && transaction.Status == "Pending")
                {
                    status = "Chờ xác nhận";
                }
                else if (invoice.DueDate < DateTime.Now)
                {
                    status = "Quá hạn";
                    canPay = true;
                }
                else
                {
                    status = "Chưa nộp";
                    canPay = true;
                }
                return new TuitionDto
                {
                    InvoiceId = invoice.InvoiceId,
                    ClassId = invoice.ClassId,
                    ClassName = invoice.Class?.ClassName ?? "Unknown",
                    Period = $"Tháng {invoice.PeriodMonth}/{invoice.PeriodYear}", // Ghép tháng năm cho UI dễ nhìn
                    Amount = invoice.Amount,
                    DueDate = invoice.DueDate,
                    DisplayStatus = status,
                    CanPay = canPay
                };
            }).ToList();

            return new PagedResult<TuitionDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size),
                CurrentPage = filter.Page
            };
        }

        public async Task<TuitionInvoiceDetailDto> GetTuitionInvoiceDetailAsync(Guid invoiceId)
        {
            Guid studentId = _currentUserService.UserId;
            var (invoice, transaction, attendances) = await _tuitionRepository.GetInvoiceDetailAsync(invoiceId, studentId);
            if (invoice == null) throw new KeyNotFoundException("Không tìm thấy hóa đơn này!");
            string statusDisplay;
            bool canPay = false;

            if (invoice.Status == "Paid")
            {
                statusDisplay = "Đã hoàn thành";
            }
            else if (transaction != null && transaction.Status == "Pending")
            {
                statusDisplay = "Đang chờ giáo viên xác nhận";
            }
            else if (invoice.DueDate < DateTime.UtcNow)
            {
                statusDisplay = "Quá hạn";
                canPay = true;
            }
            else
            {
                statusDisplay = "Chưa nộp";
                canPay = true;
            }
            return new TuitionInvoiceDetailDto
            {
                InvoiceID = invoice.InvoiceId,
                Title = $"Học phí lớp {invoice.Class?.ClassName} - Tháng {invoice.PeriodMonth}/{invoice.PeriodYear}",
                Period = $"Tháng {invoice.PeriodMonth}/{invoice.PeriodYear}",
                DueDate = invoice.DueDate,
                UnitPrice = invoice.Class?.TuitionFee ?? 0,
                TotalSessions = attendances.Count,

                TotalAmount = invoice.Amount,
                StatusDisplay = statusDisplay,
                CanPay = canPay,

                BilledSessions = attendances.Select(a => new BilledSessionDto
                {
                    Date = (DateOnly)(a.Session?.Date),
                    Title = a.Session?.Title ?? "Buổi học",
                    AttendanceStatus = a.Status == "Present" ? "Có mặt" : (a.IsExcused == true ? "Vắng có phép" : "Vắng không phép")
                }).ToList()
            };
        }
    }
}
