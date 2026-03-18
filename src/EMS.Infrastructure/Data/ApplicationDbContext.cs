using System;
using System.Collections.Generic;
using EMS.Domain.Entities;
using EMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Assignment> Assignments { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassEnrollment> ClassEnrollments { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }

    public virtual DbSet<ClassTum> ClassTa { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<GradeCategory> GradeCategories { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<LearningMaterial> LearningMaterials { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<ProgressReport> ProgressReports { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<SubmissionFeedback> SubmissionFeedbacks { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeachingAssistant> TeachingAssistants { get; set; }

    public virtual DbSet<TeachingAssistantTask> TeachingAssistantTasks { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("Account_pkey");

            entity.ToTable("Account");

            entity.HasIndex(e => e.Email, "Account_Email_key").IsUnique();

            entity.Property(e => e.AccountId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("AccountID");
            entity.Property(e => e.AvatarUrl).HasColumnName("AvatarURL");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.ResetPasswordToken).HasMaxLength(255);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Active'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.VerificationToken).HasMaxLength(255);

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Account_RoleID_fkey");
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("Assignment_pkey");

            entity.ToTable("Assignment");

            entity.Property(e => e.AssignmentId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("AssignmentID");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DueDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.GradeCategoryId).HasColumnName("GradeCategoryID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Published'::character varying");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Author).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Assignment_AuthorID_fkey");

            entity.HasOne(d => d.Class).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Assignment_ClassID_fkey");

            entity.HasOne(d => d.GradeCategory).WithMany(p => p.Assignments)
                .HasForeignKey(d => d.GradeCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Assignment_GradeCategoryID_fkey");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("Attendance_pkey");

            entity.ToTable("Attendance");

            entity.Property(e => e.AttendanceId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("AttendanceID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.IsExcused).HasDefaultValue(false);
            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("Attendance_InvoiceID_fkey");

            entity.HasOne(d => d.Session).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Attendance_SessionID_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Attendance_StudentID_fkey");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("Class_pkey");

            entity.ToTable("Class");

            entity.Property(e => e.ClassId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("ClassID");
            entity.Property(e => e.ClassName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Room).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Ongoing'::character varying");
            entity.Property(e => e.SubjectId).HasColumnName("SubjectID");
            entity.Property(e => e.TeacherId).HasColumnName("TeacherID");
            entity.Property(e => e.TuitionFee).HasPrecision(12, 2);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("class_subjectid_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Class_TeacherID_fkey");
        });

        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("ClassEnrollment_pkey");

            entity.ToTable("ClassEnrollment");

            entity.Property(e => e.EnrollmentId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("EnrollmentID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.EnrolledDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Active'::character varying");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassEnrollments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ClassEnrollment_ClassID_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.ClassEnrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ClassEnrollment_StudentID_fkey");
        });

        modelBuilder.Entity<ClassSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("ClassSchedule_pkey");

            entity.ToTable("ClassSchedule");

            entity.Property(e => e.ScheduleId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("ScheduleID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSchedules)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ClassSchedule_ClassID_fkey");
        });

        modelBuilder.Entity<ClassTum>(entity =>
        {
            entity.HasKey(e => e.ClassTaid).HasName("ClassTA_pkey");

            entity.ToTable("ClassTA");

            entity.Property(e => e.ClassTaid)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("ClassTAID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Permission).HasMaxLength(100);
            entity.Property(e => e.SalaryPerSession)
                .HasPrecision(12, 2)
                .HasDefaultValueSql("0");
            entity.Property(e => e.Taid).HasColumnName("TAID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassTa)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ClassTA_ClassID_fkey");

            entity.HasOne(d => d.Ta).WithMany(p => p.ClassTa)
                .HasForeignKey(d => d.Taid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ClassTA_TAID_fkey");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("Comment_pkey");

            entity.ToTable("Comment");

            entity.Property(e => e.CommentId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("CommentID");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Author).WithMany(p => p.Comments)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Comment_AuthorID_fkey");

            entity.HasOne(d => d.Post).WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Comment_PostID_fkey");
        });

        modelBuilder.Entity<GradeCategory>(entity =>
        {
            entity.HasKey(e => e.GradeCategoryId).HasName("GradeCategory_pkey");

            entity.ToTable("GradeCategory");

            entity.Property(e => e.GradeCategoryId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("GradeCategoryID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Weight).HasPrecision(5, 2);

            entity.HasOne(d => d.Class).WithMany(p => p.GradeCategories)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("GradeCategory_ClassID_fkey");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("Invoice_pkey");

            entity.ToTable("Invoice");

            entity.Property(e => e.InvoiceId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("InvoiceID");
            entity.Property(e => e.Amount).HasPrecision(12, 2);
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DueDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pending'::character varying");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Class).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Invoice_ClassID_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Invoice_StudentID_fkey");
        });

        modelBuilder.Entity<LearningMaterial>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("LearningMaterial_pkey");

            entity.ToTable("LearningMaterial");

            entity.Property(e => e.MaterialId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("MaterialID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.FileUrl).HasColumnName("FileURL");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Class).WithMany(p => p.LearningMaterials)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("LearningMaterial_ClassID_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("Notification_pkey");

            entity.ToTable("Notification");

            entity.Property(e => e.NotificationId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("NotificationID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Account).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Notification_AccountID_fkey");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("Post_pkey");

            entity.ToTable("Post");

            entity.Property(e => e.PostId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("PostID");
            entity.Property(e => e.AttachmentUrl).HasColumnName("AttachmentURL");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Author).WithMany(p => p.Posts)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Post_AuthorID_fkey");

            entity.HasOne(d => d.Class).WithMany(p => p.Posts)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Post_ClassID_fkey");
        });

        modelBuilder.Entity<ProgressReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("ProgressReport_pkey");

            entity.ToTable("ProgressReport");

            entity.Property(e => e.ReportId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("ReportID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Draft'::character varying");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.TeacherId).HasColumnName("TeacherID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Class).WithMany(p => p.ProgressReports)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ProgressReport_ClassID_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.ProgressReports)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ProgressReport_StudentID_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.ProgressReports)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ProgressReport_TeacherID_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("Role_pkey");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "Role_RoleName_key").IsUnique();

            entity.Property(e => e.RoleId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("Session_pkey");

            entity.ToTable("Session");

            entity.Property(e => e.SessionId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("SessionID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Scheduled'::character varying");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Class).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Session_ClassID_fkey");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("Student_pkey");

            entity.ToTable("Student");

            entity.Property(e => e.StudentId)
                .ValueGeneratedNever()
                .HasColumnName("StudentID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.ParentEmail).HasMaxLength(255);
            entity.Property(e => e.ParentName).HasMaxLength(100);
            entity.Property(e => e.ParentPhone).HasMaxLength(15);

            entity.HasOne(d => d.StudentNavigation).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Student_StudentID_fkey");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("subject_pkey");

            entity.ToTable("Subject");

            entity.Property(e => e.SubjectId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("SubjectID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.SubjectName).HasMaxLength(100);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("Submission_pkey");

            entity.ToTable("Submission");

            entity.Property(e => e.SubmissionId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("SubmissionID");
            entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
            entity.Property(e => e.FileUrl).HasColumnName("FileURL");
            entity.Property(e => e.Grade).HasPrecision(5, 2);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Submitted'::character varying");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Assignment).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Submission_AssignmentID_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Submission_StudentID_fkey");
        });

        modelBuilder.Entity<SubmissionFeedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("SubmissionFeedback_pkey");

            entity.ToTable("SubmissionFeedback");

            entity.Property(e => e.FeedbackId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("FeedbackID");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Author).WithMany(p => p.SubmissionFeedbacks)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("SubmissionFeedback_AuthorID_fkey");

            entity.HasOne(d => d.Submission).WithMany(p => p.SubmissionFeedbacks)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("SubmissionFeedback_SubmissionID_fkey");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("SystemLog_pkey");

            entity.ToTable("SystemLog");

            entity.Property(e => e.LogId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("LogID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ActionType).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(45)
                .HasColumnName("IPAddress");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.TableName).HasMaxLength(50);

            entity.HasOne(d => d.Account).WithMany(p => p.SystemLogs)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("SystemLog_AccountID_fkey");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId).HasName("Teacher_pkey");

            entity.ToTable("Teacher");

            entity.Property(e => e.TeacherId)
                .ValueGeneratedNever()
                .HasColumnName("TeacherID");
            entity.Property(e => e.BankAccount).HasMaxLength(50);
            entity.Property(e => e.BankAccountName).HasMaxLength(100);
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.Specialization).HasMaxLength(100);

            entity.HasOne(d => d.TeacherNavigation).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Teacher_TeacherID_fkey");
        });

        modelBuilder.Entity<TeachingAssistant>(entity =>
        {
            entity.HasKey(e => e.Taid).HasName("TeachingAssistant_pkey");

            entity.ToTable("TeachingAssistant");

            entity.Property(e => e.Taid)
                .ValueGeneratedNever()
                .HasColumnName("TAID");
            entity.Property(e => e.BankAccount).HasMaxLength(50);
            entity.Property(e => e.BankAccountName).HasMaxLength(100);
            entity.Property(e => e.BankName).HasMaxLength(100);

            entity.HasOne(d => d.Ta).WithOne(p => p.TeachingAssistant)
                .HasForeignKey<TeachingAssistant>(d => d.Taid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TeachingAssistant_TAID_fkey");
        });

        modelBuilder.Entity<TeachingAssistantTask>(entity =>
        {
            entity.HasKey(e => e.TataskId).HasName("TeachingAssistantTask_pkey");

            entity.ToTable("TeachingAssistantTask");

            entity.Property(e => e.TataskId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("TATaskID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(c => c.Type).HasColumnName("Type").HasMaxLength(255);
            entity.Property(e => e.DueDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Todo'::character varying");
            entity.Property(e => e.ClassTaId).HasColumnName("ClassTAID");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.ClassTa).WithMany(p => p.TeachingAssistantTasks)
                .HasForeignKey(d => d.ClassTaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TeachingAssistantTask_TAID_fkey");

        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("Transaction_pkey");

            entity.ToTable("Transaction");

            entity.Property(e => e.TransactionId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("TransactionID");
            entity.Property(e => e.AmountPaid).HasPrecision(12, 2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PaidDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.ProofImageUrl).HasColumnName("ProofImageURL");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pending'::character varying");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Transaction_InvoiceID_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
