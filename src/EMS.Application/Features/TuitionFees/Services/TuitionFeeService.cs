using EMS.Application.Common.Interfaces;
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
    public class TuitionFeeService : ITuitionFeeService
    {
        private readonly ITuitionFeeRepository tuitionFeeRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TuitionFeeService> _logger;
        private readonly ICurrentUserService currentUserService;

        public TuitionFeeService(
            ITuitionFeeRepository tuitionFeeRepository,
            INotificationService notificationService,
            ILogger<TuitionFeeService> logger, ICurrentUserService currentUserService)
        {
            this.tuitionFeeRepository = tuitionFeeRepository;
            _notificationService = notificationService;
            _logger = logger;
            this.currentUserService = currentUserService;
        }


        // =========================================================
        // 🎯 MÀN 2: QUẢN LÝ LỚP & HÓA ĐƠN (Class Hub)
        // =========================================================

        // --- Tab Danh sách & Chi tiết ---
        public async Task<(IEnumerable<ClassInvoiceItemDto> Items, int TotalCount)> GetClassInvoicesForPeriodAsync(
            Guid classId, int month, int year, Guid teacherId, int page, int size, string? status = null, Guid? studentId = null)
        {
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền xem hóa đơn của lớp này.");

            var (items, total) = await tuitionFeeRepository.GetInvoicesByClassAndPeriodPagedAsync(classId, month, year, page, size, status, studentId);

            var pageItems = items.Select(i => new ClassInvoiceItemDto
            {
                InvoiceId = i.InvoiceId,
                StudentId = i.StudentId,
                StudentName = i.Student?.FullName ?? "Unknown",
                SessionCount = i.SessionCount ?? 0,
                TotalAmount = i.Amount,
                PaidAmount = i.Transactions?.Sum(t => t.AmountPaid) ?? 0m,
                Status = i.Status ?? "",
                DueDate = i.DueDate,
                ProofImageUrl = i.Transactions?.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.ProofImageUrl
            }).ToList();

            return (pageItems, total);
        }




        // --- Tab Cấu hình & Phát hành ---
        public async Task<IEnumerable<TuitionFeeConfigDto>> GetTuitionFeeConfigsAsync(Guid teacherId)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var classes = await tuitionFeeRepository.GetClassesWithStudentsByTeacherAsync(teacherId);
            var result = new List<TuitionFeeConfigDto>();

            foreach (var c in classes)
            {
                bool isGenerated = await tuitionFeeRepository.HasInvoicesForPeriodAsync(c.ClassId, currentMonth, currentYear);
                result.Add(new TuitionFeeConfigDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    IsInvoiceGeneratedThisMonth = isGenerated,
                    PricePerSession = c.TuitionFee,
                    BillingMethod = c.BillingMethod ?? "Postpaid",
                    StudentCount = c.ClassEnrollments.Count,
                    PaymentDeadlineDays = c.PaymentDeadlineDays ?? 0
                });
            }
            return result;
        }



        public async Task GenerateInvoicesForClassAsync(Guid classId, GenerateInvoiceDto req, Guid teacherId)
        {
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            if (await tuitionFeeRepository.HasInvoicesForPeriodAsync(classId, req.PeriodMonth, req.PeriodYear))
                throw new Exception("Kỳ này đã phát hành hóa đơn.");

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classObj == null) throw new Exception("Lớp học không tồn tại.");

            var students = (await tuitionFeeRepository.GetActiveStudentsInClassAsync(classId)).ToList();
            var invoices = new List<Invoice>();
            int scheduledSessions = await tuitionFeeRepository.CountScheduledSessionsAsync(classId, req.PeriodMonth, req.PeriodYear);

            var periodStart = new DateTime(req.PeriodYear, req.PeriodMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(req.PeriodYear, req.PeriodMonth, DateTime.DaysInMonth(req.PeriodYear, req.PeriodMonth), 23, 59, 59, DateTimeKind.Utc);
            var attendanceCounts = await tuitionFeeRepository.GetAttendanceCountsForClassPeriodAsync(classId, periodStart, periodEnd);

            foreach (var enrollment in students)
            {
                decimal amountToPay = 0;
                string description = string.Empty;
                int sessionCount = 0;

                if (string.Equals(classObj.BillingMethod, "Prepaid", StringComparison.OrdinalIgnoreCase))
                {
                    decimal baseFee = scheduledSessions * classObj.TuitionFee;
                    decimal discount = enrollment.CreditBalance ?? 0;
                    amountToPay = Math.Max(0, baseFee - discount);
                    sessionCount = scheduledSessions;
                    description = $"Học phí dự kiến {scheduledSessions} buổi. Cấn trừ {discount:N0}đ.";
                    enrollment.CreditBalance = 0;
                }
                else
                {
                    attendanceCounts.TryGetValue(enrollment.StudentId, out int attended);
                    amountToPay = attended * classObj.TuitionFee;
                    sessionCount = attended;
                    description = $"Học phí thực tế {attended} buổi.";
                }

                if (amountToPay > 0 || string.Equals(classObj.BillingMethod, "Prepaid", StringComparison.OrdinalIgnoreCase))
                {
                    invoices.Add(new Invoice
                    {
                        InvoiceId = Guid.NewGuid(),
                        StudentId = enrollment.StudentId,
                        ClassId = classId,
                        PeriodMonth = (short)req.PeriodMonth,
                        PeriodYear = req.PeriodYear,
                        SessionCount = sessionCount,
                        Amount = amountToPay,
                        Description = description,
                        DueDate = req.DueDate.ToUniversalTime(),
                        Status = amountToPay == 0 ? "Paid" : "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await tuitionFeeRepository.AddInvoicesAsync(invoices);

            // Gửi Notification sau khi tạo
            try
            {
                var targetStudents = await _notificationService.GetStudentTargetsAsync(classId);
                foreach (var invoice in invoices)
                {
                    var target = targetStudents.FirstOrDefault(t => t.StdId == invoice.StudentId);
                    if (target != default)
                    {
                        string billingType = classObj.BillingMethod == "Prepaid" ? "thu trước" : "thu sau";
                        string content = $"Hệ thống đã phát hành hóa đơn học phí tháng {invoice.PeriodMonth}/{invoice.PeriodYear}. Số tiền: {invoice.Amount:N0}đ.";
                        await _notificationService.SendNotificationAsync(target.AccId, invoice.StudentId, "Thông báo học phí", content, $"/student/invoices/{invoice.InvoiceId}", "Invoice");
                    }
                }
            }
            catch (Exception ex) { _logger.LogError($"Lỗi gửi thông báo: {ex.Message}"); }

            var persisted = await tuitionFeeRepository.AddInvoicesWithEnrollmentsAsync(invoices, students, classId, req.PeriodMonth, req.PeriodYear);
            if (!persisted) throw new Exception("Phát hành hóa đơn thất bại hoặc đã có dữ liệu trước đó.");
        }

        public async Task ReconcilePrepaidClassAsync(Guid classId, int month, int year, Guid teacherId)
        {
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classObj == null || classObj.BillingMethod != "Prepaid") throw new Exception("Chỉ áp dụng cho lớp Thu trước.");

            var enrollments = await tuitionFeeRepository.GetActiveStudentsInClassAsync(classId);
            foreach (var enrollment in enrollments)
            {
                int excusedAbsences = await tuitionFeeRepository.CountExcusedAbsencesAsync(enrollment.StudentId, classId, month, year);
                decimal refundAmount = excusedAbsences * classObj.TuitionFee;
                enrollment.CreditBalance = (enrollment.CreditBalance ?? 0) + refundAmount;
            }
            await tuitionFeeRepository.UpdateClassEnrollmentsAsync(enrollments);
        }

        // --- Thao tác trên Hóa đơn ---
        public async Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays, Guid teacherId)
        {
            var invoice = await tuitionFeeRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) throw new Exception("Không tìm thấy hóa đơn.");
            if (invoice.Class.TeacherId != teacherId) throw new UnauthorizedAccessException("Không có quyền gia hạn.");

            invoice.DueDate = invoice.DueDate.AddDays(additionalDays);
            invoice.UpdatedAt = DateTime.UtcNow;
            await tuitionFeeRepository.UpdateInvoiceAsync(invoice);
        }

        public async Task ExtendClassInvoicesDueDateAsync(Guid classId, ExtendClassInvoicesDto request, Guid teacherId)
        {
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Không có quyền thao tác trên lớp này.");

            var invoices = await tuitionFeeRepository.GetInvoicesByClassAndPeriodAsync(classId, request.PeriodMonth, request.PeriodYear);
            if (invoices == null || !invoices.Any()) throw new Exception("Không tìm thấy hóa đơn nào.");

            foreach (var invoice in invoices)
            {
                if (invoice.Status != "Paid")
                {
                    invoice.DueDate = invoice.DueDate.AddDays(request.AdditionalDays);
                    invoice.UpdatedAt = DateTime.UtcNow;
                }
            }
            await tuitionFeeRepository.UpdateInvoicesAsync(invoices);
        }




        // =========================================================
        // 🔍 MÀN 3: DUYỆT THANH TOÁN THỦ CÔNG (Queue & History)
        // =========================================================

        public async Task<IEnumerable<PendingTransactionDto>> GetPendingTransactionsAsync(Guid teacherId)
        {
            var ts = await tuitionFeeRepository.GetPendingTransactionsByTeacherAsync(teacherId);
            return ts.Select(t => new PendingTransactionDto
            {
                TransactionId = t.TransactionId,
                AmountPaid = t.AmountPaid,
                StudentName = t.Invoice!.Student!.FullName,
                ClassName = t.Invoice.Class.ClassName,
                ProofImageURL = t.ProofImageUrl,
                PaidDate = t.PaidDate ?? DateTime.UtcNow
            });
        }

        public async Task ReviewTransactionAsync(Guid transId, bool isApproved, Guid approverId, string? note)
        {
            var t = await tuitionFeeRepository.GetTransactionWithInvoiceAsync(transId);
            if (t == null) throw new KeyNotFoundException("Không tìm thấy giao dịch này.");
            if (t.Status != "Pending") throw new InvalidOperationException("Giao dịch này đã được xử lý trước đó.");

            var inv = t.Invoice;
            if (isApproved)
            {
                if (t.AmountPaid < inv.Amount)
                    throw new InvalidOperationException($"Số tiền nộp {t.AmountPaid:N0}đ không đủ so với hóa đơn {inv.Amount:N0}đ.");

                t.Status = "Successful";
                inv.Status = "Paid";
                inv.Description += $" | [Duyệt tay {DateTime.Now:dd/MM}]";
            }
            else
            {
                t.Status = "Failed";
                t.Note = note;
                inv.Description += $" | [Từ chối {DateTime.Now:dd/MM}]: {note}";
            }

            t.ApprovedBy = approverId;
            t.UpdatedAt = DateTime.UtcNow;
            await tuitionFeeRepository.UpdateTransactionStatusAsync(t, inv);

            // Notification gửi cho học sinh về kết quả duyệt
            try
            {
                var targetAccountId = await _notificationService.GetAccountIdByStudentIdAsync(t.Invoice.StudentId);
                if (targetAccountId.HasValue)
                {
                    string title = isApproved ? "Thanh toán thành công" : "Giao dịch bị từ chối";
                    string content = isApproved ? $"Giao dịch cho lớp {t.Invoice.Class.ClassName} đã xác nhận." : $"Giao dịch bị từ chối. Lý do: {note}";
                    await _notificationService.SendNotificationAsync(targetAccountId.Value, t.Invoice.StudentId, title, content, $"/student/tuition", "Invoice");
                }
            }
            catch (Exception ex) { _logger.LogError($"Lỗi gửi thông báo duyệt: {ex.Message}"); }
        }

        public async Task UndoTransactionAsync(Guid transactionId, Guid teacherId)
        {
            var trans = await tuitionFeeRepository.GetTransactionWithInvoiceAsync(transactionId);
            if (trans == null) throw new KeyNotFoundException("Giao dịch không tồn tại.");

            trans.Status = "Pending";
            trans.ApprovedBy = null;
            var inv = trans.Invoice;
            inv.Status = inv.DueDate < DateTime.UtcNow ? "Overdue" : "Pending";
            inv.Description += " | [Hoàn tác xử lý]";

            await tuitionFeeRepository.UpdateTransactionStatusAsync(trans, inv);
        }

        public async Task<IEnumerable<TransactionHistoryDto>> GetTransactionHistoryAsync(Guid teacherId, DateTime? from, DateTime? to)
        {
            var ts = await tuitionFeeRepository.GetTransactionHistoryByTeacherAsync(teacherId, from, to);
            return ts.Select(t => new TransactionHistoryDto
            {
                TransactionId = t.TransactionId,
                InvoiceId = t.InvoiceId,
                StudentName = t.Invoice.Student.FullName,
                ClassName = t.Invoice.Class.ClassName,
                Amount = t.AmountPaid,
                ProofImageUrl = t.ProofImageUrl,
                Status = t.Status,
                ReviewerNote = t.Note,
                ProcessedAt = t.UpdatedAt,
                CreatedAt = (DateTime)t.CreatedAt
            });
        }



        public async Task<IEnumerable<GlobalInvoiceRecordDto>> GetInvoicesListAsync(Guid? classId, int month, int year)
        {
            var teacherId = currentUserService.UserId;

            // 1. Lấy thực thể Invoice từ Repository
            var invoices = await tuitionFeeRepository.GetInvoicesByFilterAsync(teacherId, classId, month, year);

            // 2. Map sang DTO
            return invoices.Select(i => new GlobalInvoiceRecordDto
            {
                InvoiceId = i.InvoiceId,
                ClassId = i.ClassId,
                ClassName = i.Class?.ClassName ?? "N/A",
                BillingMethod = i.Class?.BillingMethod ?? "Postpaid",

                StudentId = i.StudentId,
                StudentName = i.Student?.FullName ?? "N/A",
                AvatarUrl = i.Student?.Account?.AvatarUrl,

                SessionCount = (int)i.SessionCount,
                TotalAmount = i.Amount,
                // Cộng tổng tiền từ các giao dịch đã Include ở Repo
                PaidAmount = i.Transactions?.Sum(t => t.AmountPaid) ?? 0m,

                DueDate = i.DueDate,
                Status = i.Status,
                PeriodMonth = i.PeriodMonth,
                PeriodYear = i.PeriodYear
            }).ToList();
        }



        public async Task<IEnumerable<ClassFeeConfigDto>> GetClassFeeConfigsAsync()
        {
            var teacherId = currentUserService.UserId; // Tự động lấy ID người đang đăng nhập

            var classes = await tuitionFeeRepository.GetTeacherClassesConfigAsync(teacherId);

            return classes.Select(c => new ClassFeeConfigDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                BillingMethod = c.BillingMethod ?? "Postpaid",
                TuitionFee = c.TuitionFee,
                PaymentDeadlineDays = (int)c.PaymentDeadlineDays
            }).ToList();
        }

        public async Task UpdateClassFeeAsync(Guid classId, UpdateClassFeeConfigDto dto)
        {
            var teacherId = currentUserService.UserId;

            // Chốt chặn bảo mật
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền sửa cấu hình lớp này.");

            // Validate dữ liệu đầu vào
            if (dto.TuitionFee < 0) throw new InvalidOperationException("Học phí không được âm.");
            if (dto.PaymentDeadlineDays <= 0) throw new InvalidOperationException("Hạn nộp phải lớn hơn 0.");
            if (dto.BillingMethod != "Prepaid" && dto.BillingMethod != "Postpaid")
                throw new InvalidOperationException("Hình thức thu không hợp lệ.");

            await tuitionFeeRepository.UpdateClassFeeConfigAsync(classId, dto.BillingMethod, dto.TuitionFee, dto.PaymentDeadlineDays);
        }

        public async Task<ClassFeeConfigDto> GetClassFeeConfigAsync(Guid classId)
        {
            var teacherId = currentUserService.UserId;

            // Lấy dữ liệu từ Repo (đã bao gồm chốt chặn bảo mật teacherId)
            var c = await tuitionFeeRepository.GetClassConfigByIdAsync(classId, teacherId);

            if (c == null)
            {
                throw new KeyNotFoundException("Không tìm thấy lớp học hoặc bạn không có quyền truy cập.");
            }

            // Map sang DTO (Lưu ý không dùng ?? cho TuitionFee và PaymentDeadlineDays như đã sửa lỗi trước đó)
            return new ClassFeeConfigDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                BillingMethod = c.BillingMethod ?? "Postpaid",
                TuitionFee = c.TuitionFee,
                PaymentDeadlineDays = (int)c.PaymentDeadlineDays
            };
        }

        public async Task ExtendInvoiceAsync(Guid invoiceId, ExtendInvoiceDto dto)
        {
            if (dto.AdditionalDays <= 0)
                throw new InvalidOperationException("Số ngày gia hạn phải lớn hơn 0.");

            var teacherId = currentUserService.UserId;

            // Logic bảo mật đã được check bên trong hàm Repo
            await tuitionFeeRepository.ExtendInvoiceDueDateAsync(invoiceId, dto.AdditionalDays, teacherId);
        }

        public async Task ExtendClassInvoicesAsync(Guid classId, ExtendClassInvoicesDto dto)
        {
            if (dto.AdditionalDays <= 0)
                throw new InvalidOperationException("Số ngày gia hạn phải lớn hơn 0.");

            var teacherId = currentUserService.UserId;

            // Chốt chặn bảo mật cho toàn lớp
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            await tuitionFeeRepository.ExtendClassInvoicesDueDateAsync(classId, dto.PeriodMonth, dto.PeriodYear, dto.AdditionalDays);
        }


        public async Task<TuitionSummaryDto> GetTuitionSummaryAsync(Guid? classId, int month, int year)
        {
            var teacherId = currentUserService.UserId;
            var invoices = await tuitionFeeRepository.GetInvoicesByPeriodAsync(teacherId, classId, month, year);

            var expected = invoices.Where(i => i.Status != "Cancelled").Sum(i => i.Amount);
            var actual = invoices.Where(i => i.Status == "Paid").Sum(i => i.Amount);

            return new TuitionSummaryDto
            {
                ExpectedRevenue = expected,
                ActualRevenue = actual,
                DebtAmount = expected - actual
            };
        }

        public async Task<IEnumerable<ClassTuitionReportDto>> GetClassesOverviewAsync(int month, int year)
        {
            var teacherId = currentUserService.UserId;
            var classes = await tuitionFeeRepository.GetClassesWithDataAsync(teacherId, month, year);

            return classes.Select(c => {
                var invs = c.Invoices.ToList();
                var exp = invs.Where(i => i.Status != "Cancelled").Sum(i => i.Amount);
                var act = invs.Where(i => i.Status == "Paid").Sum(i => i.Amount);

                return new ClassTuitionReportDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    BillingMethod = c.BillingMethod ?? "Postpaid",
                    TuitionFee = c.TuitionFee,
                    StudentCount = c.ClassEnrollments.Count,
                    CollectionRate = exp > 0 ? (double)Math.Round((act / exp) * 100, 2) : 0
                };
            }).ToList();
        }

        public Task<IEnumerable<Class>> GetClassesOverviewEntitiesAsync(Guid teacherId, int month, int year)
        {
            throw new NotImplementedException();
        }
    }
}
