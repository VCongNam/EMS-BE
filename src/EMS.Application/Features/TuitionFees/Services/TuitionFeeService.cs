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
            // --- BƯỚC 1: CÁC LỚP BẢO VỆ (VALIDATION) ---

            // 1.1. Kiểm tra quyền sở hữu
            if (!await tuitionFeeRepository.IsTeacherOwnsClassAsync(classId, teacherId))
                throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên lớp này.");

           

            // 1.3. Kiểm tra kỳ này đã phát hành chưa
            if (await tuitionFeeRepository.HasInvoicesForPeriodAsync(classId, req.PeriodMonth, req.PeriodYear))
                throw new Exception("Kỳ này đã phát hành hóa đơn rồi, không thể phát hành thêm.");

            var classObj = await tuitionFeeRepository.GetClassByIdAsync(classId);
            if (classObj == null) throw new Exception("Lớp học không tồn tại.");

            // 1.4. Kiểm tra logic theo BillingMethod (Postpaid không được gen cho tương lai/hiện tại khi chưa hết tháng)
            var now = DateTime.UtcNow;
            if (string.Equals(classObj.BillingMethod, "Postpaid", StringComparison.OrdinalIgnoreCase))
            {
                if (req.PeriodYear > now.Year || (req.PeriodYear == now.Year && req.PeriodMonth >= now.Month))
                    throw new InvalidOperationException("Lớp Thu sau chỉ được phát hành hóa đơn khi tháng học đã kết thúc để đảm bảo đủ dữ liệu điểm danh.");
            }

            // --- BƯỚC 2: CHUẨN BỊ DỮ LIỆU ---
            var students = (await tuitionFeeRepository.GetActiveStudentsInClassAsync(classId)).ToList();
            var invoices = new List<Invoice>();
            decimal currentUnitPrice = classObj.TuitionFee; // Snapshot đơn giá

            int scheduledSessions = await tuitionFeeRepository.CountScheduledSessionsAsync(classId, req.PeriodMonth, req.PeriodYear);
            var periodStart = new DateTime(req.PeriodYear, req.PeriodMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(req.PeriodYear, req.PeriodMonth, DateTime.DaysInMonth(req.PeriodYear, req.PeriodMonth), 23, 59, 59, DateTimeKind.Utc);
            var attendanceCounts = await tuitionFeeRepository.GetAttendanceCountsForClassPeriodAsync(classId, periodStart, periodEnd);

            // --- BƯỚC 3: LOGIC TÍNH TOÁN THEO LOẠI LỚP ---
            foreach (var enrollment in students)
            {
                decimal amountToPay = 0;
                string description = string.Empty;
                int sessionCount = 0;

                if (string.Equals(classObj.BillingMethod, "Prepaid", StringComparison.OrdinalIgnoreCase))
                {
                    decimal baseFee = scheduledSessions * currentUnitPrice;
                    decimal discount = enrollment.CreditBalance ?? 0;
                    amountToPay = Math.Max(0, baseFee - discount);
                    sessionCount = scheduledSessions;
                    description = $"Học phí dự kiến {scheduledSessions} buổi. Đơn giá: {currentUnitPrice:N0}đ. Cấn trừ: {discount:N0}đ.";

                    // Cập nhật CreditBalance của enrollment về 0 vì đã dùng cấn trừ
                    enrollment.CreditBalance = 0;
                }
                else // Postpaid
                {
                    attendanceCounts.TryGetValue(enrollment.StudentId, out int attended);
                    amountToPay = attended * currentUnitPrice;
                    sessionCount = attended;
                    description = $"Học phí thực tế {attended} buổi. Đơn giá: {currentUnitPrice:N0}đ.";
                }

                // Chỉ tạo hóa đơn nếu có tiền hoặc là lớp Prepaid (để lưu vết kỳ học)
                if (amountToPay > 0 || string.Equals(classObj.BillingMethod, "Prepaid", StringComparison.OrdinalIgnoreCase))
                {
                    invoices.Add(new Invoice
                    {
                        InvoiceId = Guid.NewGuid(),
                        StudentId = enrollment.StudentId,
                        ClassId = classId,
                        PeriodMonth = (short)req.PeriodMonth,
                        PeriodYear = req.PeriodYear,

                        // LƯU VẾT QUAN TRỌNG
                        UnitPrice = currentUnitPrice,
                        SessionCount = sessionCount,

                        Amount = amountToPay,
                        Description = description,
                        DueDate = req.DueDate.ToUniversalTime(),
                        Status = amountToPay == 0 ? "Paid" : "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // --- BƯỚC 4: LƯU DATABASE ---
            var persisted = await tuitionFeeRepository.AddInvoicesWithEnrollmentsAsync(invoices, students, classId, req.PeriodMonth, req.PeriodYear);
            if (!persisted) throw new Exception("Lỗi khi lưu dữ liệu hóa đơn vào hệ thống.");

            // --- BƯỚC 5: GỬI NOTIFICATION (GIỮ NGUYÊN LOGIC CŨ CỦA KHUÊ) ---
            try
            {
                var targetStudents = await _notificationService.GetStudentTargetsAsync(classId);
                foreach (var invoice in invoices)
                {
                    var target = targetStudents.FirstOrDefault(t => t.StdId == invoice.StudentId);
                    if (target != default)
                    {
                        string content = $"Hệ thống đã phát hành hóa đơn học phí tháng {invoice.PeriodMonth}/{invoice.PeriodYear}. Số tiền: {invoice.Amount:N0}đ.";
                        await _notificationService.SendNotificationAsync(target.AccId, invoice.StudentId, "Thông báo học phí", content, $"/student/invoices/{invoice.InvoiceId}", "Invoice");
                    }
                }
            }
            catch (Exception ex) { _logger.LogError($"Lỗi gửi thông báo: {ex.Message}"); }
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
                UnitPrice = i.UnitPrice,
                Description = i.Description,
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



        public async Task<List<ClassInvoiceReminderDto>> GetPendingInvoiceRemindersAsync()
        {
            var teacherId = currentUserService.UserId;
            var now = DateTime.UtcNow;

            // 1. Lấy toàn bộ lớp đang hoạt động của giáo viên
            var classes = await tuitionFeeRepository.GetActiveClassesAsync(teacherId);
            var reminders = new List<ClassInvoiceReminderDto>();

            foreach (var c in classes)
            {
                // LOGIC KỲ CẦN KIỂM TRA:
                // Lớp Prepaid: Kiểm tra tháng hiện tại (Phải thu rồi mới được học)
                // Lớp Postpaid: Kiểm tra tháng trước (Học xong rồi phải thu ngay)
                DateTime targetDate = string.Equals(c.BillingMethod, "Prepaid", StringComparison.OrdinalIgnoreCase)
                                        ? now
                                        : now.AddMonths(-1);

                int targetMonth = targetDate.Month;
                int targetYear = targetDate.Year;

                // 2. Kiểm tra xem đã phát hành hóa đơn cho kỳ này chưa
                bool hasInvoices = await tuitionFeeRepository.HasInvoicesForPeriodAsync(c.ClassId, targetMonth, targetYear);

                if (!hasInvoices)
                {
                    reminders.Add(new ClassInvoiceReminderDto
                    {
                        ClassId = c.ClassId,
                        ClassName = c.ClassName,
                        BillingMethod = c.BillingMethod,
                        TargetPeriod = $"{targetMonth:D2}/{targetYear}",
                        Priority = string.Equals(c.BillingMethod, "Postpaid", StringComparison.OrdinalIgnoreCase) ? "High" : "Medium",
                        Message = string.Equals(c.BillingMethod, "Prepaid", StringComparison.OrdinalIgnoreCase)
                            ? $"Cần phát hành hóa đơn tháng {targetMonth} (Thu trước)."
                            : $"Cần chốt sổ và thu học phí tháng {targetMonth} (Thu sau)."
                    });
                }
            }
            return reminders;
        }




        public async Task<IEnumerable<FullTransactionHistoryDto>> GetHistoryFullAsync()
        {
            var teacherId = currentUserService.UserId;
            var transactions = await tuitionFeeRepository.GetFullTransactionHistoryAsync(teacherId);

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

            // Lấy dữ liệu thô từ Repository
            var invoices = await tuitionFeeRepository.GetInvoicesByPeriodAsync(teacherId, month, year);
            var successfulTransactions = await tuitionFeeRepository.GetSuccessfulTransactionsByPeriodAsync(teacherId, month, year);

            // 1. Tổng doanh thu dự kiến (Tổng tiền của tất cả Invoice hợp lệ trong kỳ)
            var totalExpected = invoices.Sum(i => i.Amount);

            // 2. Tổng thực thu (Tổng tiền từ các giao dịch Successful)
            var totalPaid = successfulTransactions.Sum(t => t.AmountPaid);

            // 3. Biến động theo ngày (Daily Trend)
            var dailyTrend = new List<DailyRevenueDto>();
            int daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                // Cộng dồn tất cả giao dịch Successful của tất cả các lớp trong ngày này
                var dayAmount = successfulTransactions
                    .Where(t => t.PaidDate.HasValue && t.PaidDate.Value.Day == day)
                    .Sum(t => t.AmountPaid);

                dailyTrend.Add(new DailyRevenueDto
                {
                    Day = day,
                    ReceivedAmount = dayAmount
                });
            }

            // 4. Tỷ trọng theo lớp (Pie Chart) - Dựa trên số tiền thực tế thu được
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
                TotalDebt = Math.Max(0, totalExpected - totalPaid), // Tổng nợ còn lại
                DailyTrend = dailyTrend,
                ProportionByClass = proportion
            };
        }


        public async Task<IEnumerable<FullTransactionHistoryDto>> GetTransactionsByClassAsync(Guid classId)
        {
            var teacherId = currentUserService.UserId;
            var transactions = await tuitionFeeRepository.GetTransactionsByClassAsync(classId, teacherId);

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

    }
}
