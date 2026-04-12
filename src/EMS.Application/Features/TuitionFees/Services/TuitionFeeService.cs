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
        private readonly INotificationService _notificationService; // Thêm vào
        private readonly ILogger<TuitionFeeService> _logger;

        public TuitionFeeService(
            ITuitionFeeRepository tuitionFeeRepository,
            INotificationService notificationService,
        ILogger<TuitionFeeService> logger)
        {
            this.tuitionFeeRepository = tuitionFeeRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IEnumerable<TuitionFeeConfigDto>> GetTuitionFeeConfigsAsync(Guid teacherId)
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            // Chỉ lấy danh sách lớp của chính giáo viên này
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

        public async Task UpdateTuitionFeeAsync(Guid classId, UpdateTuitionFeeDto req, Guid teacherId)
        {
            // BẢO MẬT: Kiểm tra quyền sở hữu lớp
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thay đổi học phí của lớp này.");
            }

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);

            if (classObj == null)
            {
                throw new Exception("Lớp học không tồn tại.");
            }

            int currentMonth = DateTime.UtcNow.Month;
            int currentYear = DateTime.UtcNow.Year;

            bool hasStartedThisMonth = await tuitionFeeRepository.HasAttendanceInMonthAsync(classId, currentMonth, currentYear);
            bool hasGeneratedInvoices = await tuitionFeeRepository.HasInvoicesForPeriodAsync(classId, currentMonth, currentYear);

            // Chống đổi giá giữa chừng
            if (hasStartedThisMonth && !hasGeneratedInvoices)
            {
                throw new InvalidOperationException(
                    $"Lớp học đã bắt đầu diễn ra trong tháng {currentMonth}/{currentYear} nhưng chưa được chốt hóa đơn. " +
                    $"Vui lòng đợi đến khi phát hành xong hóa đơn của tháng này, sau đó mới cập nhật giá."
                );
            }

            classObj.TuitionFee = req.TuitionFee;
            classObj.BillingMethod = req.BillingMethod;
            classObj.PaymentDeadlineDays = req.PaymentDeadlineDays;
            classObj.UpdatedAt = DateTime.UtcNow;

            await tuitionFeeRepository.UpdateClassAsync(classObj);
        }

        public async Task GenerateInvoicesForClassAsync(Guid classId, GenerateInvoiceDto req, Guid teacherId)
        {
            // BẢO MẬT: Kiểm tra quyền sở hữu lớp
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");
            }

            if (await tuitionFeeRepository.HasInvoicesForPeriodAsync(classId, req.PeriodMonth, req.PeriodYear))
            {
                throw new Exception("Kỳ này đã phát hành hóa đơn.");
            }

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classObj == null)
            {
                throw new Exception("Lớp học không tồn tại.");
            }

            var students = await tuitionFeeRepository.GetActiveStudentsInClassAsync(classId);
            var invoices = new List<Invoice>();
            int scheduledSessions = await tuitionFeeRepository.CountScheduledSessionsAsync(classId, req.PeriodMonth, req.PeriodYear);

            foreach (var enrollment in students)
            {
                decimal amountToPay = 0;
                string description = "";
                int sessionCount = 0;

                if (classObj.BillingMethod == "Prepaid")
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
                    int attended = await tuitionFeeRepository.CountStudentAttendanceAsync(enrollment.StudentId, classId, req.PeriodMonth, req.PeriodYear);

                    amountToPay = attended * classObj.TuitionFee;
                    sessionCount = attended;
                    description = $"Học phí thực tế {attended} buổi.";
                }

                if (amountToPay > 0 || classObj.BillingMethod == "Prepaid")
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

            if (classObj.BillingMethod == "Prepaid")
            {
                await tuitionFeeRepository.UpdateClassEnrollmentsAsync(students);
            }
        }

        public async Task ReconcilePrepaidClassAsync(Guid classId, int month, int year, Guid teacherId)
        {
            // BẢO MẬT: Kiểm tra quyền sở hữu lớp
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");
            }

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);

            if (classObj == null || classObj.BillingMethod != "Prepaid")
            {
                throw new Exception("Chỉ áp dụng cho lớp Thu trước.");
            }

            var enrollments = await tuitionFeeRepository.GetActiveStudentsInClassAsync(classId);

            foreach (var enrollment in enrollments)
            {
                int excusedAbsences = await tuitionFeeRepository.CountExcusedAbsencesAsync(enrollment.StudentId, classId, month, year);
                decimal refundAmount = excusedAbsences * classObj.TuitionFee;

                enrollment.CreditBalance = (enrollment.CreditBalance ?? 0) + refundAmount;
            }

            await tuitionFeeRepository.UpdateClassEnrollmentsAsync(enrollments);
        }

        public async Task<IEnumerable<PendingTransactionDto>> GetPendingTransactionsAsync(Guid teacherId)
        {
            // Lấy Bill thuộc về lớp của Teacher này
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

            if (t == null || t.Status != "Pending")
            {
                throw new Exception("Giao dịch không tồn tại hoặc không ở trạng thái chờ xử lý.");
            }

            Invoice? inv = null;

            if (isApproved)
            {
                t.Status = "Completed";
                t.ApprovedBy = approverId;
                t.PaidDate = t.PaidDate ?? DateTime.UtcNow;
                t.UpdatedAt = DateTime.UtcNow;

                inv = t.Invoice;

                var totalPaid = await tuitionFeeRepository.GetTotalPaidAmountAsync(inv!.InvoiceId) + t.AmountPaid;
                inv.Status = totalPaid >= inv.Amount ? "Paid" : "Partial";
                inv.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                t.Status = "Rejected";
                t.ApprovedBy = approverId;
                t.Note = note;
                t.UpdatedAt = DateTime.UtcNow;
            }

            await tuitionFeeRepository.UpdateTransactionStatusAsync(t, inv);

            //Notification
            string title = isApproved ? "Thanh toán thành công" : "Giao dịch bị từ chối";
            string statusText = isApproved ? "đã được xác nhận" : "bị từ chối";
            try
            {
                if (t.Invoice != null)
                {
                    var targetAccountId = await _notificationService.GetAccountIdByStudentIdAsync(t.Invoice.StudentId);
                    var studentId = t.Invoice.StudentId;
                    var className = t.Invoice.Class?.ClassName ?? "Lớp học";

                    string content = isApproved
                        ? $"Giao dịch {t.AmountPaid:N0}đ cho lớp {className} {statusText}. Cảm ơn bạn!"
                        : $"Giao dịch {t.AmountPaid:N0}đ cho lớp {className} {statusText}. Lý do: {note ?? "Thông tin không khớp"}.";

                    if (targetAccountId.HasValue)
                    {
                        await _notificationService.SendNotificationAsync(
                            targetAccountId: targetAccountId.Value,
                            studentId: studentId,
                            title: title,
                            content: content,
                            actionUrl: $"/student/invoices/{t.InvoiceId}",
                            type: "Invoice"
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Không tìm thấy accountId cho student {StudentId} khi gửi thông báo giao dịch.", studentId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi gửi thông báo duyệt học phí: {ex.Message}");
            }
        }

        public async Task<ClassFinancialDetailDto> GetClassFinancialDetailAsync(Guid classId, int m, int y, Guid teacherId)
        {
            // BẢO MẬT: Kiểm tra quyền sở hữu lớp
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem báo cáo của lớp này.");
            }

            var c = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (c == null) throw new Exception("Lớp học không tồn tại.");

            var invs = await tuitionFeeRepository.GetClassInvoicesAsync(classId, m, y);
                
            return new ClassFinancialDetailDto
            {
                ClassId = classId,
                ClassName = c.ClassName,
                BillingMethod = c.BillingMethod ?? "Postpaid",
                Students = invs.Select(i => new StudentInvoiceItemDto
                {
                    StudentId = i.StudentId,
                    StudentName = i.Student!.FullName,
                    AttendedSessions = i.SessionCount,
                    TotalAmount = i.Amount,
                    PaidAmount = i.Transactions.Sum(t => t.AmountPaid),
                    Status = i.Status
                }).ToList()
            };
        }

        public async Task<OverallReportDto> GetOverallReportAsync(Guid teacherId)
        {
            return new OverallReportDto
            {
                TotalCollected = await tuitionFeeRepository.GetTotalRevenueByTeacherAsync(teacherId),
                TotalPaidInvoices = await tuitionFeeRepository.CountInvoicesByStatusForTeacherAsync("Paid", teacherId),
                TotalPendingInvoices = await tuitionFeeRepository.CountInvoicesByStatusForTeacherAsync("Pending", teacherId)
                                     + await tuitionFeeRepository.CountInvoicesByStatusForTeacherAsync("Partial", teacherId)
            };
        }
        public async Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays, Guid teacherId)
        {
            var invoice = await tuitionFeeRepository.GetInvoiceByIdAsync(invoiceId);

            if (invoice == null)
            {
                throw new Exception("Không tìm thấy hóa đơn.");
            }

            if (invoice.Class.TeacherId != teacherId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền gia hạn hóa đơn này.");
            }

            invoice.DueDate = invoice.DueDate.AddDays(additionalDays);
            invoice.UpdatedAt = DateTime.UtcNow;

            await tuitionFeeRepository.UpdateInvoiceAsync(invoice);
        }

        // Nhận TUPLE từ Repo và map sang DTO
        public async Task<IEnumerable<ClassFinancialSummaryDto>> GetClassFinancialSummariesAsync(Guid teacherId)
        {
            var rawData = await tuitionFeeRepository.GetClassFinancialSummariesAsync(teacherId);

            return rawData.Select(x => new ClassFinancialSummaryDto
            {
                ClassId = x.ClassId,
                ClassName = x.ClassName,
                StudentCount = x.StudentCount,
                ExpectedRevenue = x.ExpectedRevenue,
                ActualRevenue = x.ActualRevenue,
                DebtAmount = Math.Max(0, x.ExpectedRevenue - x.ActualRevenue),
                CollectionRate = x.ExpectedRevenue > 0 ? (double)(x.ActualRevenue / x.ExpectedRevenue) * 100 : 0
            }).ToList();
        }

        // Nhận TUPLE từ Repo và map sang DTO
        public async Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(Guid teacherId)
        {
            var rawSummaries = await tuitionFeeRepository.GetClassFinancialSummariesAsync(teacherId);
            var totalRevenue = rawSummaries.Sum(s => s.ActualRevenue);
            var activeClassCount = rawSummaries.Count();

            var rawTrends = await tuitionFeeRepository.GetRevenueTrendAsync(teacherId, 6);

            var trends = rawTrends.Select(t => new RevenueTrendDto { MonthLabel = t.MonthLabel, Revenue = t.Revenue }).ToList();

            var revenueByClasses = rawSummaries
                .Select(s => new ClassRevenueDistributionDto { ClassName = s.ClassName, Revenue = s.ActualRevenue })
                .Where(r => r.Revenue > 0)
                .OrderByDescending(r => r.Revenue)
                .ToList();

            return new DashboardAnalyticsDto
            {
                TotalRevenue = totalRevenue,
                TotalStudents = await tuitionFeeRepository.GetTotalActiveStudentsByTeacherAsync(teacherId),
                AverageRevenuePerClass = activeClassCount > 0 ? totalRevenue / activeClassCount : 0,
                QuarterlyTarget = 1000000000,
                RevenueTrends = trends,
                RevenueByClasses = revenueByClasses
            };
        }
        public async Task ExtendClassInvoicesDueDateAsync(Guid classId, ExtendClassInvoicesDto request, Guid teacherId)
        {
            // BẢO MẬT: Kiểm tra quyền sở hữu lớp
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");
            }

            // Lấy toàn bộ hóa đơn của lớp trong kỳ đó
            var invoices = await tuitionFeeRepository.GetInvoicesByClassAndPeriodAsync(classId, request.PeriodMonth, request.PeriodYear);

            if (invoices == null || !invoices.Any())
            {
                throw new Exception($"Không tìm thấy hóa đơn nào cho lớp này trong kỳ {request.PeriodMonth}/{request.PeriodYear}.");
            }

            // Quét qua các hóa đơn và cộng thêm ngày
            foreach (var invoice in invoices)
            {
                // Chỉ gia hạn cho những hóa đơn đang nợ (Pending hoặc Partial)
                if (invoice.Status != "Paid")
                {
                    invoice.DueDate = invoice.DueDate.AddDays(request.AdditionalDays);
                    invoice.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Lưu đồng loạt xuống Database
            await tuitionFeeRepository.UpdateInvoicesAsync(invoices);
        }
    }
}
