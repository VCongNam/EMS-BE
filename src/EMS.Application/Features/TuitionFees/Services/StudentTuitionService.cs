using EMS.Application.Common.DTOs;
using EMS.Application.Common.Helpers;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Assignments.DTOs;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.TuitionFees.Services
{
    public class StudentTuitionService : IStudentTuitionService
    {
        private readonly ITuitionFeeRepository _tuitionRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IVietQRService _vietQRService;
        private readonly ISupabaseStorageService _supabaseStorageService;
        private readonly INotificationService _notificationService;
        private readonly IClassRepository _classRepository;
        private readonly ILogger<StudentTuitionService> _logger;
        public StudentTuitionService(
            ITuitionFeeRepository tuitionRepository, 
            ICurrentUserService currentUserService, 
            IVietQRService vietQRService, 
            ISupabaseStorageService supabaseStorageService, 
            INotificationService notificationService, 
            IClassRepository classRepository,
            ILogger<StudentTuitionService> logger)
        {
            _tuitionRepository = tuitionRepository;
            _currentUserService = currentUserService;
            _vietQRService = vietQRService;
            _supabaseStorageService = supabaseStorageService;
            _notificationService = notificationService;
            _classRepository = classRepository;
            _logger = logger;
        }

        public async Task<PagedResult<StudentTuitionDto>> GetMyTuitionAsync(TuitionFilter filter)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
           
            if (filter.ClassID.HasValue)
            {
                bool isEnrolled = await _classRepository.IsStudentAlreadyEnrolledAsync(filter.ClassID.Value, studentId);
                if (!isEnrolled) throw new UnauthorizedAccessException("Bạn không có quyền truy cập lớp này.");
            }

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
                return new StudentTuitionDto
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

            return new PagedResult<StudentTuitionDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Size),
                CurrentPage = filter.Page
            };
        }

        public async Task<StudentTuitionInvoiceDetailDto> GetTuitionInvoiceDetailAsync(Guid invoiceId)
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
            return new StudentTuitionInvoiceDetailDto
            {
                InvoiceID = invoice.InvoiceId,
                Title = $"Học phí lớp {invoice.Class?.ClassName} - Tháng {invoice.PeriodMonth}/{invoice.PeriodYear}",
                Period = $"Tháng {invoice.PeriodMonth}/{invoice.PeriodYear}",
                DueDate = invoice.DueDate,
                UnitPrice = invoice.Class?.TuitionFee ?? 0,
                TotalSessions = (int)invoice.SessionCount,
                TotalAmount = invoice.Amount,
                StatusDisplay = statusDisplay,
                CanPay = canPay,
            };
        }

        public async Task<PaymentQrDto> GetPaymentQrCodeAsync(Guid invoiceId)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            var invoice = await _tuitionRepository.GetInvoiceWithTeacherBankInfoAsync(invoiceId, studentId);
            if (invoice == null) throw new KeyNotFoundException("Không tìm thấy hóa đơn!");
            var isEnrolled = await _classRepository.IsStudentAlreadyEnrolledAsync(invoice.ClassId, studentId);
            if (!isEnrolled) throw new Exception("Bạn không thuộc lớp có hóa đơn này");

            if (invoice.Status == "Paid") throw new InvalidOperationException("Hóa đơn này đã được thanh toán!");

            var teacher = invoice.Class?.Teacher;
            if (teacher == null || string.IsNullOrEmpty(teacher.BankAccount) && string.IsNullOrEmpty(teacher.BankName))
            {
                throw new Exception("Giáo viên chưa cập nhật thông tin tài khoản ngân hàng. Vui lòng liên hệ giáo viên.");
            }
            string shortInvoiceId = invoice.InvoiceId.ToString().Substring(0, 6).ToUpper();
            string transferContent = $"HOC PHI LOP {invoice.Class?.ClassName} THANG {invoice.PeriodMonth}/{invoice.PeriodYear}";

            var qrRequest = new VietQRRequest
            {
                BankId = teacher.BankName,
                AccountNo = teacher.BankAccount,
                AccountName = teacher.BankAccountName ?? "GIAO VIEN",
                Amount = invoice.Amount,
                Content = transferContent
            };

            string qrBase64 = await _vietQRService.GenerateQRCodeAsync(qrRequest);
            if (qrBase64 != null)
            {
                return new PaymentQrDto
                {
                    QrCodeBase64 = qrBase64,
                    BankName = teacher.BankName,
                    AccountNo = teacher.BankAccount,
                    AccountName = teacher.BankAccountName,
                    Amount = invoice.Amount,
                    TransferContent = transferContent
                };
            } else
            {
                throw new Exception("Tạo Qr thất bại. Vui lòng thử lại sau");
            }
        }

        public async Task<bool> UploadPaymentProofAsync(Guid invoiceId, ProofUploadDto request)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");

            var invoice = await _tuitionRepository.GetInvoiceDetailAsync(invoiceId, studentId);
            if (invoice.Invoice == null) throw new KeyNotFoundException("Không tìm thấy hóa đơn!");
            if (invoice.Invoice.Status == "Paid") throw new InvalidOperationException("Hóa đơn đã được thanh toán!");
            var existingTransaction = await _tuitionRepository.GetTransactionStudentAndInvoiceId(invoiceId, studentId);
            

            if (existingTransaction != null)
            {
                if (existingTransaction.Status == "Pending")
                {
                    throw new InvalidOperationException("Bạn đã nộp minh chứng rồi. Vui lòng chờ giáo viên xác nhận!");
                }
            }
            

            DataValidator.ValidateFile(request.ProofImage);
            string imageUrl = await _supabaseStorageService.UploadFileAsync(request.ProofImage, "tuition-proofs");
            bool isReupload = false;
            if (existingTransaction != null && existingTransaction.Status == "Failed")
            {
                var oldProofUrl = existingTransaction.ProofImageUrl;
                // Nộp lại: Cập nhật Transaction cũ thành Pending
                existingTransaction.ProofImageUrl = imageUrl;
                existingTransaction.Status = "Pending";
                existingTransaction.UpdatedAt = DateTime.UtcNow;

                await _tuitionRepository.UpdateTransactionAsync(existingTransaction);
                if (!string.IsNullOrEmpty(oldProofUrl))
                {
                    try
                    {
                        await _supabaseStorageService.DeleteFileByUrlAsync(oldProofUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Không thể xóa ảnh minh chứng cũ: {oldProofUrl}");
                    }
                }
                isReupload = true;
            }
            else
            {
                // Nộp lần đầu: Tạo Transaction mới
                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    AmountPaid = invoice.Invoice.Amount,
                    PaymentMethod = "Bank Transfer",
                    ProofImageUrl = imageUrl,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                await _tuitionRepository.AddTransactionAsync(transaction);
            }

            //Notification
            var invoiceInfo = await _tuitionRepository.GetInvoicesWithClassAsync(invoiceId);
            if (invoiceInfo.Class != null)
            {
                string title = isReupload ? "Nộp lại minh chứng học phí" : "Giao dịch học phí mới";
                await _notificationService.SendNotificationAsync(
                    targetAccountId: invoiceInfo.Class.TeacherId,
                    studentId: null,
                    title: title,
                    content: $"Học sinh lớp {invoiceInfo.Class.ClassName} đã nộp minh chứng học phí tháng {invoiceInfo.PeriodMonth}/{invoiceInfo.PeriodYear}",
                    actionUrl: $"/tuition/reports/{invoiceInfo.ClassId}/transactions",
                    type: "Invoice");
            }
            return true;
        }

        public async Task<List<StudentTransactionViewDto>> GetMyTransactionsAsync(Guid? classId)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            var transactions = await _tuitionRepository.GetTransactionsByStudentIdAsync(studentId, classId);
            if (transactions == null)
                throw new Exception("Bạn chưa có giao dịch nào!");
            var result = transactions.Select(t => new StudentTransactionViewDto
            {
                TransactionId = t.TransactionId,
                InvoiceId = t.InvoiceId,
                InvoiceContent = !string.IsNullOrWhiteSpace(t.Invoice.Description)
                             ? t.Invoice.Description
                             : $"Học phí tháng {t.Invoice.PeriodMonth}/{t.Invoice.PeriodYear}",
                AmountPaid = t.AmountPaid,
                PaidDate = t.PaidDate,
                Status = t.Status
            }).ToList();
            return result;
        }

        public async Task<StudentTransactionDetailDto?> GetTransactionByIdAsync(Guid transactionId)
        {
            Guid studentId = _currentUserService.StudentId ?? throw new UnauthorizedAccessException("Student ID is missing.");
            var transaction = await _tuitionRepository.GetTransactionDetailAsync(transactionId, studentId);
            if (transaction == null)
            {
                throw new Exception("Không tìm thấy giao dịch");
            }
            return new StudentTransactionDetailDto
            {
                TransactionId = transaction.TransactionId,
                InvoiceContent = !string.IsNullOrWhiteSpace(transaction.Invoice.Description)
                            ? transaction.Invoice.Description
                            : $"Học phí tháng {transaction.Invoice.PeriodMonth}/{transaction.Invoice.PeriodYear}",
                AmountPaid = transaction.AmountPaid,
                PaymentMethod = transaction.PaymentMethod,
                ProofImageURL = transaction.ProofImageUrl,
                PaidDate = transaction.PaidDate,
                Status = transaction.Status
            };
        }
    }
}
