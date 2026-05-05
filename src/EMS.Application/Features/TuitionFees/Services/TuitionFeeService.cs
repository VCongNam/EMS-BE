using DocumentFormat.OpenXml.VariantTypes;
using EMS.Application.Common.Exceptions;
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

        public async Task<List<InvoicePreviewDto>> GetInvoicesPreviewAsync(Guid classId, int month, int year)
        {
            var teacherId = currentUserService.UserId;

            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            if (await tuitionFeeRepository.HasInvoicesForPeriodAsync(classId, month, year))
                throw new BadRequestException($"Kỳ {month}/{year} đã phát hành hóa đơn rồi.");

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classObj == null) throw new NotFoundException("Lớp học không tồn tại.");


            if (year < classObj.StartDate.Year || (year == classObj.StartDate.Year && month < classObj.StartDate.Month))
            {
                throw new BadRequestException($"Lớp học này bắt đầu từ tháng {classObj.StartDate.Month}/{classObj.StartDate.Year}. Không thể phát hành hóa đơn cho kỳ {month}/{year}.");
            }

            if (classObj.EndDate != null)
            {
                var endYear = classObj.EndDate.Year;
                var endMonth = classObj.EndDate.Month;

                if (year > endYear || (year == endYear && month > endMonth))
                {
                    throw new BadRequestException($"Lớp học này đã kết thúc vào tháng {endMonth}/{endYear}. Không thể phát hành hóa đơn cho kỳ {month}/{year}.");
                }
            }

            TimeZoneInfo vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);

            DateTime firstDayOfNextMonth = new DateTime(year, month, 1).AddMonths(1);

            if (nowVn < firstDayOfNextMonth)
            {
                throw new BadRequestException($"Chưa thể phát hành hóa đơn kỳ {month}/{year}. " +
                    $"Vui lòng đợi đến ngày 01/{firstDayOfNextMonth.Month}/{firstDayOfNextMonth.Year} khi kỳ học kết thúc.");
            }


            var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

            var studentsToBill = await tuitionFeeRepository.GetStudentsForBillingAsync(classId, month, year);
            var attendanceDetails = await tuitionFeeRepository.GetDetailedAttendanceCountsAsync(classId, periodStart, periodEnd);
            int totalSessions = await tuitionFeeRepository.CountScheduledSessionsAsync(classId, month, year);

            var previews = new List<InvoicePreviewDto>();
            decimal unitPrice = classObj.TuitionFee;

            foreach (var enrollment in studentsToBill)
            {
                var stats = attendanceDetails.TryGetValue(enrollment.StudentId, out var detail)
                            ? detail
                            : (Attended: 0, Excused: 0, Unexcused: 0);

                decimal amount = stats.Attended * unitPrice;

                previews.Add(new InvoicePreviewDto
                {
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student?.FullName ?? "Unknown",
                    StudentStatus = enrollment.Status ?? "Active",
                    EnrollmentDate = enrollment.CreatedAt,
                    TotalSessionsInMonth = totalSessions,
                    AttendedSessions = stats.Attended,
                    ExcusedAbsences = stats.Excused,
                    UnexcusedAbsences = stats.Unexcused,

                    UnitPrice = unitPrice,
                    Amount = amount
                });
            }

            return previews.OrderBy(p => p.StudentStatus == "Active" ? 0 : 1).ThenBy(p => p.StudentName).ToList();
        }

        public async Task ConfirmAndGenerateInvoicesAsync(Guid classId, ConfirmInvoicesDto dto)
        {
            var teacherId = currentUserService.UserId;

            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            if (await tuitionFeeRepository.HasInvoicesForPeriodAsync(classId, dto.PeriodMonth, dto.PeriodYear))
                throw new BadRequestException("Kỳ này đã phát hành hóa đơn rồi, không thể phát hành thêm.");

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            var invoices = new List<Invoice>();
            decimal unitPrice = classObj.TuitionFee;

            foreach (var item in dto.Invoices)
            {
                decimal amount = item.AttendedSessions * unitPrice;
                if (amount == 0 && item.AttendedSessions == 0) continue;

                string description = $"Học phí tháng {dto.PeriodMonth}/{dto.PeriodYear}. Thực tế học: {item.AttendedSessions} buổi.";

                invoices.Add(new Invoice
                {
                    InvoiceId = Guid.NewGuid(),
                    StudentId = item.StudentId,
                    ClassId = classId,
                    PeriodMonth = (short)dto.PeriodMonth,
                    PeriodYear = dto.PeriodYear,
                    UnitPrice = unitPrice,
                    SessionCount = item.AttendedSessions,
                    Amount = amount,
                    Description = description,
                    DueDate = dto.DueDate.ToUniversalTime(),
                    Status = amount == 0 ? "Paid" : "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!invoices.Any()) throw new BadRequestException("Không có hóa đơn nào hợp lệ để tạo.");

            var persisted = await tuitionFeeRepository.AddInvoicesWithEnrollmentsAsync(invoices, null, classId, dto.PeriodMonth, dto.PeriodYear);
            if (!persisted) throw new BadRequestException("Lỗi khi lưu dữ liệu hóa đơn vào hệ thống.");

            try
            {
                var targetStudents = await _notificationService.GetStudentTargetsAsync(classId);
                foreach (var invoice in invoices.Where(i => i.Amount > 0))
                {
                    var target = targetStudents.FirstOrDefault(t => t.StdId == invoice.StudentId);
                    if (target != default)
                    {
                        string content = $"Đã phát hành hóa đơn tháng {invoice.PeriodMonth}/{invoice.PeriodYear}. Số tiền cần nộp: {invoice.Amount:N0}đ.";
                        await _notificationService.SendNotificationAsync(target.AccId, invoice.StudentId, "Thông báo học phí", content, $"/student/classes/{target.StdId}/tuition", "Invoice");
                    }
                }
            }
            catch (Exception ex) { _logger.LogError($"Lỗi gửi thông báo: {ex.Message}"); }
        }


        public async Task<InvoicePreviewDto> GetStudentFinalInvoicePreviewAsync(Guid classId, Guid studentId, int month, int year)
        {
            var teacherId = currentUserService.UserId;

            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            var existingInvoices = await tuitionFeeRepository.GetStudentInvoicesAsync(studentId, 1, 10, classId);
            if (existingInvoices.Items.Any(i => i.Invoice.PeriodMonth == month && i.Invoice.PeriodYear == year && i.Invoice.IsDeleted != true))
                throw new BadRequestException($"Học sinh này đã được phát hành hóa đơn kỳ {month}/{year} rồi.");

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classObj == null) throw new NotFoundException("Lớp học không tồn tại.");

            if (year < classObj.StartDate.Year || (year == classObj.StartDate.Year && month < classObj.StartDate.Month))
                throw new BadRequestException("Lớp chưa bắt đầu vào thời điểm này.");

            var studentsToBill = await tuitionFeeRepository.GetStudentsForBillingAsync(classId, month, year);
            var targetStudent = studentsToBill.FirstOrDefault(s => s.StudentId == studentId);

            if (targetStudent == null)
                throw new NotFoundException("Học sinh không tồn tại trong danh sách thu tiền của tháng này.");

            var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);
            var attendanceDetails = await tuitionFeeRepository.GetDetailedAttendanceCountsAsync(classId, periodStart, periodEnd);
            int totalSessions = await tuitionFeeRepository.CountScheduledSessionsAsync(classId, month, year);

            var stats = attendanceDetails.TryGetValue(studentId, out var detail) ? detail : (Attended: 0, Excused: 0, Unexcused: 0);

            return new InvoicePreviewDto
            {
                StudentId = targetStudent.StudentId,
                StudentName = targetStudent.Student?.FullName ?? "Unknown",
                StudentStatus = targetStudent.Status ?? "Active",
                EnrollmentDate = targetStudent.CreatedAt,
                TotalSessionsInMonth = totalSessions,
                AttendedSessions = stats.Attended,
                ExcusedAbsences = stats.Excused,
                UnexcusedAbsences = stats.Unexcused,
                UnitPrice = classObj.TuitionFee,
                Amount = stats.Attended * classObj.TuitionFee
            };
        }

        public async Task ConfirmStudentFinalInvoiceAsync(Guid classId, Guid studentId, ConfirmSingleInvoiceDto dto)
        {
            var teacherId = currentUserService.UserId;

            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

            var existingInvoices = await tuitionFeeRepository.GetInvoicesByClassAndPeriodAsync(classId, dto.PeriodMonth, dto.PeriodYear);
            if (existingInvoices.Any(i => i.StudentId == studentId && i.IsDeleted != true))
                throw new BadRequestException($"Học sinh này đã được tất toán hóa đơn kỳ {dto.PeriodMonth}/{dto.PeriodYear} rồi.");

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classObj == null) throw new NotFoundException("Lớp học không tồn tại.");

            decimal unitPrice = classObj.TuitionFee;
            decimal amount = dto.AttendedSessions * unitPrice;

            string description = $"Tất toán học phí tháng {dto.PeriodMonth}/{dto.PeriodYear} (Nghỉ ngang). Thực tế học: {dto.AttendedSessions} buổi.";

            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                StudentId = studentId,
                ClassId = classId,
                PeriodMonth = (short)dto.PeriodMonth,
                PeriodYear = dto.PeriodYear,
                UnitPrice = unitPrice,
                SessionCount = dto.AttendedSessions,
                Amount = amount,
                Description = description,
                DueDate = dto.DueDate.ToUniversalTime(),
                Status = amount == 0 ? "Paid" : "Pending",
                CreatedAt = DateTime.UtcNow
            };


            await tuitionFeeRepository.AddInvoicesAsync(new List<Invoice> { invoice });

            try
            {
                var targetStudents = await _notificationService.GetStudentTargetsAsync(classId);
                var target = targetStudents.FirstOrDefault(t => t.StdId == studentId);

                if (target != default && amount > 0)
                {
                    string content = $"Đã phát hành hóa đơn tất toán tháng {dto.PeriodMonth}/{dto.PeriodYear}. Số tiền cần nộp: {amount:N0}đ.";
                    await _notificationService.SendNotificationAsync(target.AccId, studentId, "Thông báo học phí", content, $"/student/classes/{classId}/tuition", "Invoice");
                }
            }
            catch (Exception ex) { _logger.LogError($"Lỗi gửi thông báo tất toán: {ex.Message}"); }
        }

        public async Task<IEnumerable<ClassTuitionReportDto>> GetClassesOverviewAsync(int month, int year)
        {
            var teacherId = currentUserService.UserId;
            var classes = await tuitionFeeRepository.GetClassesWithDataAsync(teacherId, month, year);
            var report = new List<ClassTuitionReportDto>();

            foreach (var c in classes)
            {
                var invs = c.Invoices.ToList();
                var exp = invs.Where(i => i.Status != "Cancelled").Sum(i => i.Amount);
                var act = invs.Where(i => i.Status == "Paid").Sum(i => i.Amount);

                var item = new ClassTuitionReportDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    BillingMethod = "Postpaid",
                    TuitionFee = c.TuitionFee,
                    StudentCount = c.ClassEnrollments.Count,
                    CollectionRate = exp > 0 ? (double)Math.Round((act / exp) * 100, 2) : 0
                };

                bool isIssued = invs.Any();

                if (isIssued)
                {
                    item.ConditionCode = "ISSUED";
                    item.StatusMessage = "Đã phát hành";
                    item.IsIssuable = false;
                }
                else
                {
                    bool isDone = await tuitionFeeRepository.CheckAllSessionsAttendedAsync(c.ClassId, month, year);
                    item.ConditionCode = isDone ? "READY" : "INCOMPLETE";
                    item.StatusMessage = isDone ? "Đã điểm danh đủ" : "Chưa đủ điểm danh";
                    item.IsIssuable = isDone;
                }

                report.Add(item);
            }
            return report;
        }

        public async Task ExtendInvoiceDueDateAsync(Guid invoiceId, int additionalDays)
        {
            var teacherId = currentUserService.UserId;
            var invoice = await tuitionFeeRepository.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) throw new NotFoundException("Không tìm thấy hóa đơn.");
            if (invoice.Class.TeacherId != teacherId) throw new UnauthorizedAccessException("Không có quyền gia hạn.");

            invoice.DueDate = invoice.DueDate.AddDays(additionalDays);
            invoice.UpdatedAt = DateTime.UtcNow;
            await tuitionFeeRepository.UpdateInvoiceAsync(invoice);
        }

        public async Task ExtendClassInvoicesDueDateAsync(Guid classId, ExtendClassInvoicesDto request)
        {
            var teacherId = currentUserService.UserId;
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Không có quyền thao tác trên lớp này.");

            var invoices = await tuitionFeeRepository.GetInvoicesByClassAndPeriodAsync(classId, request.PeriodMonth, request.PeriodYear);
            if (invoices == null || !invoices.Any()) throw new NotFoundException("Không tìm thấy hóa đơn nào.");

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

        public async Task<IEnumerable<PendingTransactionDto>> GetPendingTransactionsAsync()
        {
            var teacherId = currentUserService.UserId;
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

        public async Task ReviewTransactionAsync(Guid transId, bool isApproved, string? note)
        {
            var approverId = currentUserService.UserId;
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

            try
            {
                var targetAccountId = await _notificationService.GetAccountIdByStudentIdAsync(t.Invoice.StudentId);
                if (targetAccountId.HasValue)
                {
                    string title = isApproved ? "Thanh toán thành công" : "Giao dịch bị từ chối";
                    string content = isApproved ? $"Giao dịch cho lớp {t.Invoice.Class.ClassName} đã xác nhận." : $"Giao dịch bị từ chối. Lý do: {note}";
                    await _notificationService.SendNotificationAsync(targetAccountId.Value, t.Invoice.StudentId, title, content, $"/student/classes/{t.Invoice.ClassId}/tuition", "Invoice");
                }
            }
            catch (Exception ex) { _logger.LogError($"Lỗi gửi thông báo duyệt: {ex.Message}"); }
        }

        public async Task UndoTransactionAsync(Guid transactionId)
        {
            var teacherId = currentUserService.UserId;
            var trans = await tuitionFeeRepository.GetTransactionWithInvoiceAsync(transactionId);
            if (trans == null) throw new KeyNotFoundException("Giao dịch không tồn tại.");

            trans.Status = "Pending";
            trans.ApprovedBy = null;
            var inv = trans.Invoice;
            inv.Status = inv.DueDate < DateTime.UtcNow ? "Overdue" : "Pending";
            inv.Description += " | [Hoàn tác xử lý]";

            await tuitionFeeRepository.UpdateTransactionStatusAsync(trans, inv);
        }

        public async Task<IEnumerable<TransactionHistoryDto>> GetTransactionHistoryAsync(DateTime? from, DateTime? to)
        {
            var teacherId = currentUserService.UserId;
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
            var invoices = await tuitionFeeRepository.GetInvoicesByFilterAsync(teacherId, classId, month, year);

            return invoices.Select(i =>
            {
                decimal amount = (decimal)((i.UnitPrice ?? 0) * i.SessionCount);

                return new GlobalInvoiceRecordDto
                {
                    InvoiceId = i.InvoiceId,
                    ClassId = i.ClassId,
                    ClassName = i.Class?.ClassName ?? "N/A",
                    BillingMethod = i.Class?.BillingMethod ?? "N/A",
                    UnitPrice = i.UnitPrice,
                    Description = i.Description,
                    StudentId = i.StudentId,
                    StudentName = i.Student?.FullName ?? "N/A",
                    AvatarUrl = i.Student?.Account?.AvatarUrl,
                    SessionCount = (int)i.SessionCount,
                    TotalAmount = i.Amount,

                    PaidAmount = i.Transactions?.Sum(t => t.AmountPaid) ?? 0m,
                    DueDate = i.DueDate,
                    Status = i.Status,
                    PeriodMonth = i.PeriodMonth,
                    PeriodYear = i.PeriodYear
                };
            }).ToList();
        }

        public async Task<IEnumerable<ClassFeeConfigDto>> GetClassFeeConfigsAsync()
        {
            var teacherId = currentUserService.UserId;
            var classes = await tuitionFeeRepository.GetTeacherClassesConfigAsync(teacherId);

            return classes.Select(c => new ClassFeeConfigDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                BillingMethod = "Postpaid",
                TuitionFee = c.TuitionFee,
                PaymentDeadlineDays = (int)c.PaymentDeadlineDays
            }).ToList();
        }

        public async Task UpdateClassFeeAsync(Guid classId, UpdateClassFeeConfigDto dto)
        {
            var teacherId = currentUserService.UserId;

            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền sửa cấu hình lớp này.");

            await tuitionFeeRepository.UpdateClassFeeConfigAsync(classId, "Postpaid", dto.TuitionFee, dto.PaymentDeadlineDays);
        }

        public async Task<ClassFeeConfigDto> GetClassFeeConfigAsync(Guid classId)
        {
            var teacherId = currentUserService.UserId;
            var c = await tuitionFeeRepository.GetClassConfigByIdAsync(classId, teacherId);

            if (c == null) throw new NotFoundException("Không tìm thấy lớp học hoặc bạn không có quyền truy cập.");

            return new ClassFeeConfigDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                BillingMethod = "Postpaid",
                TuitionFee = c.TuitionFee,
                PaymentDeadlineDays = (int)c.PaymentDeadlineDays
            };
        }

        public async Task ExtendInvoiceAsync(Guid invoiceId, ExtendInvoiceDto dto)
        {
            if (dto.AdditionalDays <= 0) throw new InvalidOperationException("Số ngày gia hạn phải lớn hơn 0.");
            var teacherId = currentUserService.UserId;
            await tuitionFeeRepository.ExtendInvoiceDueDateAsync(invoiceId, dto.AdditionalDays, teacherId);
        }

        public async Task ExtendClassInvoicesAsync(Guid classId, ExtendClassInvoicesDto dto)
        {
            if (dto.AdditionalDays <= 0) throw new InvalidOperationException("Số ngày gia hạn phải lớn hơn 0.");
            var teacherId = currentUserService.UserId;

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

        public Task<IEnumerable<Class>> GetClassesOverviewEntitiesAsync(Guid teacherId, int month, int year)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ClassInvoiceReminderDto>> GetPendingInvoiceRemindersAsync(int month, int year)
        {
            var teacherId = currentUserService.UserId;
            TimeZoneInfo localZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime nowFull = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localZone);
            DateOnly today = DateOnly.FromDateTime(nowFull);

            var classesInPeriod = await tuitionFeeRepository.GetClassesActiveInPeriodAsync(teacherId, month, year);
            var reminders = new List<ClassInvoiceReminderDto>();

            foreach (var c in classesInPeriod)
            {
                bool hasInvoices = await tuitionFeeRepository.HasInvoicesForPeriodAsync(c.ClassId, month, year);

                if (!hasInvoices)
                {
                    DateOnly periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

                    bool needsReminder = false;
                    string priority = "Medium";

                    if (today > periodEnd)
                    {
                        needsReminder = true;
                        priority = "High";
                    }
                    else if (today == periodEnd)
                    {
                        needsReminder = true;
                        priority = "Medium";
                    }

                    if (needsReminder)
                    {
                        reminders.Add(new ClassInvoiceReminderDto
                        {
                            ClassId = c.ClassId,
                            ClassName = c.ClassName,
                            BillingMethod = "Postpaid",
                            TargetPeriod = $"{month:D2}/{year}",
                            Priority = priority,
                            Message = priority == "High" ? $"Quá hạn chốt sổ! Cần phát hành hóa đơn kỳ {month}/{year}" : $"Hôm nay là ngày chốt sổ kỳ {month}/{year}"
                        });
                    }
                }
            }
            return reminders;
        }

        public async Task<IEnumerable<FullTransactionHistoryDto>> GetHistoryFullAsync(int month, int year)
        {
            var teacherId = currentUserService.UserId;
            var transactions = await tuitionFeeRepository.GetFullTransactionHistoryAsync(teacherId,month,year);

            return transactions.Select(t => new FullTransactionHistoryDto
            {
                TransactionId = t.TransactionId,
                AmountPaid = t.AmountPaid,
                PaidDate = t.PaidDate,
                PaymentMethod = t.PaymentMethod ?? "Chuyển khoản",
                Status = t.Status ?? "Pending",
                ProofImageUrl = t.ProofImageUrl,
                CreatedAt = t.CreatedAt ?? DateTime.Now,
                InvoiceId = t.InvoiceId,
                InvoiceTotalAmount = t.Invoice?.Amount ?? 0,
                PeriodMonth = t.Invoice?.PeriodMonth ?? 0,
                PeriodYear = t.Invoice?.PeriodYear ?? 0,
                InvoiceDescription = t.Invoice?.Description,
                InvoiceUnitPrice = t.Invoice?.UnitPrice ?? 0,
                InvoiceSessionCount = t.Invoice?.SessionCount ?? 0,
                StudentId = t.Invoice?.StudentId ?? Guid.Empty,
                StudentName = t.Invoice?.Student?.FullName ?? "N/A",
                ClassId = t.Invoice?.ClassId ?? Guid.Empty,
                ClassName = t.Invoice?.Class?.ClassName ?? "N/A"
            }).ToList();
        }

        public async Task<TuitionDashboardDto> GetDashboardDataAsync(int month, int year)
        {
            var teacherId = currentUserService.UserId;
            var invoices = await tuitionFeeRepository.GetInvoicesByPeriodAsync(teacherId, month, year);
            var successfulTransactions = await tuitionFeeRepository.GetSuccessfulTransactionsByPeriodAsync(teacherId, month, year);

            var totalExpected = invoices.Sum(i => i.Amount);
            var totalPaid = successfulTransactions.Sum(t => t.AmountPaid);

            var dailyTrend = new List<DailyRevenueDto>();
            int daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var dayAmount = successfulTransactions
                    .Where(t => t.PaidDate.HasValue && t.PaidDate.Value.Day == day)
                    .Sum(t => t.AmountPaid);

                dailyTrend.Add(new DailyRevenueDto { Day = day, ReceivedAmount = dayAmount });
            }

            var proportion = successfulTransactions
                .GroupBy(t => t.Invoice.Class.ClassName)
                .Select(g => new ClassRevenueDto
                {
                    ClassName = g.Key,
                    Revenue = g.Sum(t => t.AmountPaid)
                }).ToList();

            return new TuitionDashboardDto
            {
                TotalExpected = totalExpected,
                TotalPaid = totalPaid,
                TotalDebt = Math.Max(0, totalExpected - totalPaid),
                DailyTrend = dailyTrend,
                ProportionByClass = proportion
            };
        }

        public async Task<IEnumerable<FullTransactionHistoryDto>> GetTransactionsByClassAsync(Guid classId, int month, int year)
        {
            var teacherId = currentUserService.UserId;

            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền xem dữ liệu của lớp học này.");

            var transactions = await tuitionFeeRepository.GetTransactionsByClassAsync(classId, teacherId, month, year);

            return transactions.Select(t => new FullTransactionHistoryDto
            {
                TransactionId = t.TransactionId,
                AmountPaid = t.AmountPaid,
                PaidDate = t.PaidDate,
                PaymentMethod = t.PaymentMethod ?? "Chuyển khoản",
                Status = t.Status ?? "Pending",
                ProofImageUrl = t.ProofImageUrl,
                CreatedAt = t.CreatedAt ?? DateTime.Now,
                InvoiceId = t.InvoiceId,
                InvoiceTotalAmount = t.Invoice?.Amount ?? 0,
                PeriodMonth = t.Invoice?.PeriodMonth ?? 0,
                PeriodYear = t.Invoice?.PeriodYear ?? 0,
                InvoiceDescription = t.Invoice?.Description,
                InvoiceUnitPrice = t.Invoice?.UnitPrice ?? 0,
                InvoiceSessionCount = t.Invoice?.SessionCount ?? 0,
                StudentId = t.Invoice?.StudentId ?? Guid.Empty,
                StudentName = t.Invoice?.Student?.FullName ?? "N/A",
                ClassId = t.Invoice?.ClassId ?? Guid.Empty,
                ClassName = t.Invoice?.Class?.ClassName ?? "N/A"
            }).ToList();
        }

        public async Task<IEnumerable<FullTransactionHistoryDto>> GetClassTransactionsByPeriodAsync(Guid classId, int month, int year)
        {
            var teacherId = currentUserService.UserId;
            var transactions = await tuitionFeeRepository.GetTransactionsByClassAndPeriodAsync(classId, teacherId, month, year);

            return transactions.Select(t => new FullTransactionHistoryDto
            {
                TransactionId = t.TransactionId,
                AmountPaid = t.AmountPaid,
                PaidDate = t.PaidDate,
                PaymentMethod = t.PaymentMethod ?? "Chuyển khoản",
                Status = t.Status ?? "Pending",
                ProofImageUrl = t.ProofImageUrl,
                CreatedAt = t.CreatedAt ?? DateTime.Now,

                InvoiceId = t.InvoiceId,
                InvoiceTotalAmount = t.Invoice?.Amount ?? 0,
                PeriodMonth = t.Invoice?.PeriodMonth ?? 0,
                PeriodYear = t.Invoice?.PeriodYear ?? 0,
                InvoiceDescription = t.Invoice?.Description,
                InvoiceUnitPrice = t.Invoice?.UnitPrice ?? 0,
                InvoiceSessionCount = t.Invoice?.SessionCount ?? 0,

                StudentId = t.Invoice?.StudentId ?? Guid.Empty,
                StudentName = t.Invoice?.Student?.FullName ?? "N/A",

                ClassId = t.Invoice?.ClassId ?? Guid.Empty,
                ClassName = t.Invoice?.Class?.ClassName ?? "N/A"
            }).ToList();
        }

        public async Task<IEnumerable<FullTransactionHistoryDto>> GetStudentTransactionsAsync(Guid studentId, Guid? classId = null)
        {
            var teacherId = currentUserService.UserId;

            // Lấy toàn bộ lịch sử không phân biệt thời gian
            var transactions = await tuitionFeeRepository.GetTransactionsByStudentIdAsync(studentId, classId);

            return transactions.Select(t => new FullTransactionHistoryDto
            {
                TransactionId = t.TransactionId,
                AmountPaid = t.AmountPaid,
                PaidDate = t.PaidDate,
                PaymentMethod = t.PaymentMethod ?? "Chuyển khoản",
                Status = t.Status ?? "Pending",
                ProofImageUrl = t.ProofImageUrl,
                CreatedAt = t.CreatedAt ?? DateTime.Now,
                InvoiceId = t.InvoiceId,
                InvoiceTotalAmount = t.Invoice?.Amount ?? 0,
                PeriodMonth = t.Invoice?.PeriodMonth ?? 0,
                PeriodYear = t.Invoice?.PeriodYear ?? 0,
                InvoiceDescription = t.Invoice?.Description,
                InvoiceUnitPrice = t.Invoice?.UnitPrice ?? 0,
                InvoiceSessionCount = t.Invoice?.SessionCount ?? 0,
                StudentId = studentId,
                StudentName = t.Invoice?.Student?.FullName ?? "N/A",
                ClassId = t.Invoice?.ClassId ?? Guid.Empty,
                ClassName = t.Invoice?.Class?.ClassName ?? "N/A"
            }).ToList();
        }

    }


}