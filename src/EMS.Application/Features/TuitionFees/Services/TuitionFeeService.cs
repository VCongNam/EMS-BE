using EMS.Application.Features.TuitionFees.Dtos;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
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

        public TuitionFeeService(ITuitionFeeRepository tuitionFeeRepository)
        {
            this.tuitionFeeRepository = tuitionFeeRepository;
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
                    StudentCount = c.ClassEnrollments.Count
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
                StudentName = t.Invoice!.Student!.StudentNavigation!.FullName,
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
                return;
            }

            Invoice? inv = null;

            if (isApproved)
            {
                t.Status = "Completed";
                t.ApprovedBy = approverId;
                inv = t.Invoice;

                var totalPaid = await tuitionFeeRepository.GetTotalPaidAmountAsync(inv!.InvoiceId) + t.AmountPaid;
                inv.Status = totalPaid >= inv.Amount ? "Paid" : "Partial";
            }
            else
            {
                t.Status = "Rejected";
                t.ApprovedBy = approverId;
                t.Note = note;
            }

            await tuitionFeeRepository.UpdateTransactionStatusAsync(t, inv);
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
                    StudentName = i.Student!.StudentNavigation!.FullName,
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

        public async Task<bool> ReviewTransactionAsync(Guid transactionId, ReviewTransactionDto request)
        {
            var transaction = await tuitionFeeRepository.GetTransactionWithInvoiceAsync(transactionId);
            if (transaction == null) throw new KeyNotFoundException("Giao dịch không tồn tại.");
            if (transaction.Status != "Pending") throw new Exception("Giao dịch này đã được xử lý trước đó.");
            Invoice? invoiceToUpdate = null;

            if (request.IsApproved)
            {
                transaction.Status = "Approved";

                if (transaction.Invoice != null)
                {
                    invoiceToUpdate = transaction.Invoice;
                    invoiceToUpdate.Status = "Paid";
                    invoiceToUpdate.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                transaction.Status = "Rejected";
            }

            transaction.UpdatedAt = DateTime.UtcNow;
            return await tuitionFeeRepository.UpdateTransactionStatusAsync(transaction, invoiceToUpdate);
        }
    }
}
