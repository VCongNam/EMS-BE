using EMS.Application.Common.DTOs;
using EMS.Application.Common.Helpers;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Students.DTOs;
using EMS.Domain.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.Services
{
    public class StudentTuitionService : IStudentTuitionService
    {
        private readonly ITuitionFeeRepository _tuitionRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IVietQRService _vietQRService;
        private readonly ISupabaseStorageService _supabaseStorageService;
        public StudentTuitionService(ITuitionFeeRepository tuitionRepository, ICurrentUserService currentUserService, IVietQRService vietQRService, ISupabaseStorageService supabaseStorageService)
        {
            _tuitionRepository = tuitionRepository;
            _currentUserService = currentUserService;
            _vietQRService = vietQRService;
            _supabaseStorageService = supabaseStorageService;
        }

        public async Task<PagedResult<TuitionDto>> GetMyTuitionAsync(TuitionFilter filter)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

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
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
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

        public async Task<PaymentQrDto> GetPaymentQrCodeAsync(Guid invoiceId)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            var invoice = await _tuitionRepository.GetInvoiceWithTeacherBankInfoAsync(invoiceId, studentId);
            if (invoice == null) throw new KeyNotFoundException("Không tìm thấy hóa đơn!");
            if (invoice.Status == "Paid") throw new InvalidOperationException("Hóa đơn này đã được thanh toán!");

            var teacher = invoice.Class?.Teacher;
            if (teacher == null || string.IsNullOrEmpty(teacher.BankAccount))
            {
                throw new Exception("Giáo viên chưa cập nhật thông tin tài khoản ngân hàng. Vui lòng liên hệ giáo viên.");
            }
            string shortInvoiceId = invoice.InvoiceId.ToString().Substring(0, 6).ToUpper();
            string transferContent = $"HOC PHI LOP {invoice.Class?.ClassName} {shortInvoiceId}";

            var qrRequest = new VietQRRequest
            {
                BankId = teacher.BankName,
                AccountNo = teacher.BankAccount,
                AccountName = teacher.BankAccountName ?? "GIAO VIEN",
                Amount = invoice.Amount,
                Content = transferContent
            };

            string qrBase64 = await _vietQRService.GenerateQRCodeAsync(qrRequest);
            return new PaymentQrDto
            {
                QrCodeBase64 = qrBase64,
                BankName = teacher.BankName,
                AccountNo = teacher.BankAccount,
                AccountName = teacher.BankAccountName,
                Amount = invoice.Amount,
                TransferContent = transferContent
            };
        }

        public async Task<bool> UploadPaymentProofAsync(Guid invoiceId, ProofUploadDto request)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

            var invoice = await _tuitionRepository.GetInvoiceDetailAsync(invoiceId, studentId);
            if (invoice.Invoice == null) throw new KeyNotFoundException("Không tìm thấy hóa đơn!");
            if (invoice.Invoice.Status == "Paid") throw new InvalidOperationException("Hóa đơn đã được thanh toán!");
            bool isPending = await _tuitionRepository.HasPendingTransactionAsync(invoiceId);
            if (isPending)
            {
                throw new InvalidOperationException("Bạn đã nộp minh chứng rồi. Vui lòng chờ giáo viên xác nhận!");
            }

            DataValidator.ValidateFile(request.ProofImage);
            string imageUrl = await _supabaseStorageService.UploadFileAsync(request.ProofImage, "tuition-proofs");
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                InvoiceId = invoiceId,
                AmountPaid = invoice.Invoice.Amount, // Mặc định lấy đúng số tiền của hóa đơn
                PaymentMethod = "Bank Transfer", // Phương thức: Chuyển khoản
                ProofImageUrl = imageUrl,
                Status = "Pending", // Trạng thái: Đang chờ duyệt
                CreatedAt = DateTime.UtcNow
            };

            await _tuitionRepository.AddTransactionAsync(transaction);

            return true;
        }
    }
}
