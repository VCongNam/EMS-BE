using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.EMS.Domain.Entities;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Assignment> Assignments { get; set; }

    public virtual DbSet<AssignmentAttachment> AssignmentAttachments { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AuditLogEntry> AuditLogEntries { get; set; }

    public virtual DbSet<Bucket> Buckets { get; set; }

    public virtual DbSet<BucketsAnalytic> BucketsAnalytics { get; set; }

    public virtual DbSet<BucketsVector> BucketsVectors { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassEnrollment> ClassEnrollments { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }

    public virtual DbSet<ClassTum> ClassTa { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CustomOauthProvider> CustomOauthProviders { get; set; }

    public virtual DbSet<FlowState> FlowStates { get; set; }

    public virtual DbSet<GradeCategory> GradeCategories { get; set; }

    public virtual DbSet<Identity> Identities { get; set; }

    public virtual DbSet<Instance> Instances { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<LearningMaterial> LearningMaterials { get; set; }

    public virtual DbSet<MaterialAttachment> MaterialAttachments { get; set; }

    public virtual DbSet<MfaAmrClaim> MfaAmrClaims { get; set; }

    public virtual DbSet<MfaChallenge> MfaChallenges { get; set; }

    public virtual DbSet<MfaFactor> MfaFactors { get; set; }

    public virtual DbSet<Migration> Migrations { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OauthAuthorization> OauthAuthorizations { get; set; }

    public virtual DbSet<OauthClient> OauthClients { get; set; }

    public virtual DbSet<OauthClientState> OauthClientStates { get; set; }

    public virtual DbSet<OauthConsent> OauthConsents { get; set; }

    public virtual DbSet<Object> Objects { get; set; }

    public virtual DbSet<OneTimeToken> OneTimeTokens { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<PostAttachment> PostAttachments { get; set; }

    public virtual DbSet<ProgressReport> ProgressReports { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<S3MultipartUpload> S3MultipartUploads { get; set; }

    public virtual DbSet<S3MultipartUploadsPart> S3MultipartUploadsParts { get; set; }

    public virtual DbSet<SamlProvider> SamlProviders { get; set; }

    public virtual DbSet<SamlRelayState> SamlRelayStates { get; set; }

    public virtual DbSet<SchemaMigration> SchemaMigrations { get; set; }

    public virtual DbSet<SchemaMigration1> SchemaMigrations1 { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Session1> Sessions1 { get; set; }

    public virtual DbSet<SsoDomain> SsoDomains { get; set; }

    public virtual DbSet<SsoProvider> SsoProviders { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<SubmissionAttachment> SubmissionAttachments { get; set; }

    public virtual DbSet<SubmissionFeedback> SubmissionFeedbacks { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeachingAssistant> TeachingAssistants { get; set; }

    public virtual DbSet<TeachingAssistantTask> TeachingAssistantTasks { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VectorIndex> VectorIndexes { get; set; }

    public virtual DbSet<WebauthnChallenge> WebauthnChallenges { get; set; }

    public virtual DbSet<WebauthnCredential> WebauthnCredentials { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=aws-1-ap-southeast-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.qsckptkshhighxnxrgzh;Password=F5GZXoEZuaJjpvS6;SSL Mode=Require;Trust Server Certificate=true");

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
            entity.Property(e => e.AllowLateSubmission).HasDefaultValue(true);
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DueDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.GradeCategoryId).HasColumnName("GradeCategoryID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Isgraded)
                .HasDefaultValue(false)
                .HasColumnName("isgraded");
            entity.Property(e => e.Isoffline)
                .HasDefaultValue(false)
                .HasColumnName("isoffline");
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
                .HasConstraintName("Assignment_GradeCategoryID_fkey");
        });

        modelBuilder.Entity<AssignmentAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("assignmentattachment_pkey");

            entity.ToTable("AssignmentAttachment");

            entity.Property(e => e.AttachmentId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("AttachmentID");
            entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileType).HasMaxLength(100);
            entity.Property(e => e.FileUrl).HasColumnName("FileURL");

            entity.HasOne(d => d.Assignment).WithMany(p => p.AssignmentAttachments)
                .HasForeignKey(d => d.AssignmentId)
                .HasConstraintName("AssignmentAttachment_AssignmentId_fkey");
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

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_log_entries_pkey");

            entity.ToTable("audit_log_entries", "auth", tb => tb.HasComment("Auth: Audit trail for user actions."));

            entity.HasIndex(e => e.InstanceId, "audit_logs_instance_id_idx");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.InstanceId).HasColumnName("instance_id");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(64)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("ip_address");
            entity.Property(e => e.Payload)
                .HasColumnType("json")
                .HasColumnName("payload");
        });

        modelBuilder.Entity<Bucket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("buckets_pkey");

            entity.ToTable("buckets", "storage");

            entity.HasIndex(e => e.Name, "bname").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AllowedMimeTypes).HasColumnName("allowed_mime_types");
            entity.Property(e => e.AvifAutodetection)
                .HasDefaultValue(false)
                .HasColumnName("avif_autodetection");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FileSizeLimit).HasColumnName("file_size_limit");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Owner)
                .HasComment("Field is deprecated, use owner_id instead")
                .HasColumnName("owner");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Public)
                .HasDefaultValue(false)
                .HasColumnName("public");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<BucketsAnalytic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("buckets_analytics_pkey");

            entity.ToTable("buckets_analytics", "storage");

            entity.HasIndex(e => e.Name, "buckets_analytics_unique_name_idx")
                .IsUnique()
                .HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Format)
                .HasDefaultValueSql("'ICEBERG'::text")
                .HasColumnName("format");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<BucketsVector>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("buckets_vectors_pkey");

            entity.ToTable("buckets_vectors", "storage");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("Class_pkey");

            entity.ToTable("Class");

            entity.Property(e => e.ClassId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("ClassID");
            entity.Property(e => e.BillingCycle)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Monthly'::character varying");
            entity.Property(e => e.BillingMethod)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Postpaid'::character varying");
            entity.Property(e => e.ClassName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PaymentDeadlineDays).HasDefaultValue(5);
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
            entity.Property(e => e.CreditBalance)
                .HasPrecision(12, 2)
                .HasDefaultValueSql("0");
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

        modelBuilder.Entity<CustomOauthProvider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("custom_oauth_providers_pkey");

            entity.ToTable("custom_oauth_providers", "auth");

            entity.HasIndex(e => e.CreatedAt, "custom_oauth_providers_created_at_idx");

            entity.HasIndex(e => e.Enabled, "custom_oauth_providers_enabled_idx");

            entity.HasIndex(e => e.Identifier, "custom_oauth_providers_identifier_idx");

            entity.HasIndex(e => e.Identifier, "custom_oauth_providers_identifier_key").IsUnique();

            entity.HasIndex(e => e.ProviderType, "custom_oauth_providers_provider_type_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AcceptableClientIds)
                .HasDefaultValueSql("'{}'::text[]")
                .HasColumnName("acceptable_client_ids");
            entity.Property(e => e.AttributeMapping)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("attribute_mapping");
            entity.Property(e => e.AuthorizationParams)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("authorization_params");
            entity.Property(e => e.AuthorizationUrl).HasColumnName("authorization_url");
            entity.Property(e => e.CachedDiscovery)
                .HasColumnType("jsonb")
                .HasColumnName("cached_discovery");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.ClientSecret).HasColumnName("client_secret");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscoveryCachedAt).HasColumnName("discovery_cached_at");
            entity.Property(e => e.DiscoveryUrl).HasColumnName("discovery_url");
            entity.Property(e => e.EmailOptional)
                .HasDefaultValue(false)
                .HasColumnName("email_optional");
            entity.Property(e => e.Enabled)
                .HasDefaultValue(true)
                .HasColumnName("enabled");
            entity.Property(e => e.Identifier).HasColumnName("identifier");
            entity.Property(e => e.Issuer).HasColumnName("issuer");
            entity.Property(e => e.JwksUri).HasColumnName("jwks_uri");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.PkceEnabled)
                .HasDefaultValue(true)
                .HasColumnName("pkce_enabled");
            entity.Property(e => e.ProviderType).HasColumnName("provider_type");
            entity.Property(e => e.Scopes)
                .HasDefaultValueSql("'{}'::text[]")
                .HasColumnName("scopes");
            entity.Property(e => e.SkipNonceCheck)
                .HasDefaultValue(false)
                .HasColumnName("skip_nonce_check");
            entity.Property(e => e.TokenUrl).HasColumnName("token_url");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserinfoUrl).HasColumnName("userinfo_url");
        });

        modelBuilder.Entity<FlowState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("flow_state_pkey");

            entity.ToTable("flow_state", "auth", tb => tb.HasComment("Stores metadata for all OAuth/SSO login flows"));

            entity.HasIndex(e => e.CreatedAt, "flow_state_created_at_idx").IsDescending();

            entity.HasIndex(e => e.AuthCode, "idx_auth_code");

            entity.HasIndex(e => new { e.UserId, e.AuthenticationMethod }, "idx_user_id_auth_method");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AuthCode).HasColumnName("auth_code");
            entity.Property(e => e.AuthCodeIssuedAt).HasColumnName("auth_code_issued_at");
            entity.Property(e => e.AuthenticationMethod).HasColumnName("authentication_method");
            entity.Property(e => e.CodeChallenge).HasColumnName("code_challenge");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.EmailOptional)
                .HasDefaultValue(false)
                .HasColumnName("email_optional");
            entity.Property(e => e.InviteToken).HasColumnName("invite_token");
            entity.Property(e => e.LinkingTargetId).HasColumnName("linking_target_id");
            entity.Property(e => e.OauthClientStateId).HasColumnName("oauth_client_state_id");
            entity.Property(e => e.ProviderAccessToken).HasColumnName("provider_access_token");
            entity.Property(e => e.ProviderRefreshToken).HasColumnName("provider_refresh_token");
            entity.Property(e => e.ProviderType).HasColumnName("provider_type");
            entity.Property(e => e.Referrer).HasColumnName("referrer");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
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

        modelBuilder.Entity<Identity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("identities_pkey");

            entity.ToTable("identities", "auth", tb => tb.HasComment("Auth: Stores identities associated to a user."));

            entity.HasIndex(e => e.Email, "identities_email_idx").HasOperators(new[] { "text_pattern_ops" });

            entity.HasIndex(e => new { e.ProviderId, e.Provider }, "identities_provider_id_provider_unique").IsUnique();

            entity.HasIndex(e => e.UserId, "identities_user_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasComputedColumnSql("lower((identity_data ->> 'email'::text))", true)
                .HasComment("Auth: Email is a generated column that references the optional email property in the identity_data")
                .HasColumnName("email");
            entity.Property(e => e.IdentityData)
                .HasColumnType("jsonb")
                .HasColumnName("identity_data");
            entity.Property(e => e.LastSignInAt).HasColumnName("last_sign_in_at");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Identities)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("identities_user_id_fkey");
        });

        modelBuilder.Entity<Instance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("instances_pkey");

            entity.ToTable("instances", "auth", tb => tb.HasComment("Auth: Manages users across multiple sites."));

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.RawBaseConfig).HasColumnName("raw_base_config");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Uuid).HasColumnName("uuid");
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
            entity.Property(e => e.SessionCount).HasDefaultValue(0);
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

            entity.HasIndex(e => e.AuthorId, "idx_learningmaterial_authorid");

            entity.Property(e => e.MaterialId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("MaterialID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Author).WithMany(p => p.LearningMaterials)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("LearningMaterial_AuthorId_fkey");

            entity.HasOne(d => d.Class).WithMany(p => p.LearningMaterials)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("LearningMaterial_ClassID_fkey");
        });

        modelBuilder.Entity<MaterialAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("MaterialAttachment_pkey");

            entity.ToTable("MaterialAttachment");

            entity.HasIndex(e => e.MaterialId, "idx_materialattach_materialid");

            entity.Property(e => e.AttachmentId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileType).HasMaxLength(100);

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialAttachments)
                .HasForeignKey(d => d.MaterialId)
                .HasConstraintName("MaterialAttachment_MaterialId_fkey");
        });

        modelBuilder.Entity<MfaAmrClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("amr_id_pk");

            entity.ToTable("mfa_amr_claims", "auth", tb => tb.HasComment("auth: stores authenticator method reference claims for multi factor authentication"));

            entity.HasIndex(e => new { e.SessionId, e.AuthenticationMethod }, "mfa_amr_claims_session_id_authentication_method_pkey").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AuthenticationMethod).HasColumnName("authentication_method");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Session).WithMany(p => p.MfaAmrClaims)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("mfa_amr_claims_session_id_fkey");
        });

        modelBuilder.Entity<MfaChallenge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mfa_challenges_pkey");

            entity.ToTable("mfa_challenges", "auth", tb => tb.HasComment("auth: stores metadata about challenge requests made"));

            entity.HasIndex(e => e.CreatedAt, "mfa_challenge_created_at_idx").IsDescending();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.FactorId).HasColumnName("factor_id");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.OtpCode).HasColumnName("otp_code");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.WebAuthnSessionData)
                .HasColumnType("jsonb")
                .HasColumnName("web_authn_session_data");

            entity.HasOne(d => d.Factor).WithMany(p => p.MfaChallenges)
                .HasForeignKey(d => d.FactorId)
                .HasConstraintName("mfa_challenges_auth_factor_id_fkey");
        });

        modelBuilder.Entity<MfaFactor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mfa_factors_pkey");

            entity.ToTable("mfa_factors", "auth", tb => tb.HasComment("auth: stores metadata about factors"));

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "factor_id_created_at_idx");

            entity.HasIndex(e => e.LastChallengedAt, "mfa_factors_last_challenged_at_key").IsUnique();

            entity.HasIndex(e => new { e.FriendlyName, e.UserId }, "mfa_factors_user_friendly_name_unique")
                .IsUnique()
                .HasFilter("(TRIM(BOTH FROM friendly_name) <> ''::text)");

            entity.HasIndex(e => e.UserId, "mfa_factors_user_id_idx");

            entity.HasIndex(e => new { e.UserId, e.Phone }, "unique_phone_factor_per_user").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.FriendlyName).HasColumnName("friendly_name");
            entity.Property(e => e.LastChallengedAt).HasColumnName("last_challenged_at");
            entity.Property(e => e.LastWebauthnChallengeData)
                .HasComment("Stores the latest WebAuthn challenge data including attestation/assertion for customer verification")
                .HasColumnType("jsonb")
                .HasColumnName("last_webauthn_challenge_data");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Secret).HasColumnName("secret");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WebAuthnAaguid).HasColumnName("web_authn_aaguid");
            entity.Property(e => e.WebAuthnCredential)
                .HasColumnType("jsonb")
                .HasColumnName("web_authn_credential");

            entity.HasOne(d => d.User).WithMany(p => p.MfaFactors)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("mfa_factors_user_id_fkey");
        });

        modelBuilder.Entity<Migration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("migrations_pkey");

            entity.ToTable("migrations", "storage");

            entity.HasIndex(e => e.Name, "migrations_name_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ExecutedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("executed_at");
            entity.Property(e => e.Hash)
                .HasMaxLength(40)
                .HasColumnName("hash");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
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
            entity.Property(e => e.Type).HasColumnType("character varying");

            entity.HasOne(d => d.Account).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Notification_AccountID_fkey");
        });

        modelBuilder.Entity<OauthAuthorization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("oauth_authorizations_pkey");

            entity.ToTable("oauth_authorizations", "auth");

            entity.HasIndex(e => e.ExpiresAt, "oauth_auth_pending_exp_idx").HasFilter("(status = 'pending'::auth.oauth_authorization_status)");

            entity.HasIndex(e => e.AuthorizationCode, "oauth_authorizations_authorization_code_key").IsUnique();

            entity.HasIndex(e => e.AuthorizationId, "oauth_authorizations_authorization_id_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.AuthorizationCode).HasColumnName("authorization_code");
            entity.Property(e => e.AuthorizationId).HasColumnName("authorization_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CodeChallenge).HasColumnName("code_challenge");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("(now() + '00:03:00'::interval)")
                .HasColumnName("expires_at");
            entity.Property(e => e.Nonce).HasColumnName("nonce");
            entity.Property(e => e.RedirectUri).HasColumnName("redirect_uri");
            entity.Property(e => e.Resource).HasColumnName("resource");
            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Client).WithMany(p => p.OauthAuthorizations)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("oauth_authorizations_client_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.OauthAuthorizations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("oauth_authorizations_user_id_fkey");
        });

        modelBuilder.Entity<OauthClient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("oauth_clients_pkey");

            entity.ToTable("oauth_clients", "auth");

            entity.HasIndex(e => e.DeletedAt, "oauth_clients_deleted_at_idx");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ClientName).HasColumnName("client_name");
            entity.Property(e => e.ClientSecretHash).HasColumnName("client_secret_hash");
            entity.Property(e => e.ClientUri).HasColumnName("client_uri");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.GrantTypes).HasColumnName("grant_types");
            entity.Property(e => e.LogoUri).HasColumnName("logo_uri");
            entity.Property(e => e.RedirectUris).HasColumnName("redirect_uris");
            entity.Property(e => e.TokenEndpointAuthMethod).HasColumnName("token_endpoint_auth_method");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<OauthClientState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("oauth_client_states_pkey");

            entity.ToTable("oauth_client_states", "auth", tb => tb.HasComment("Stores OAuth states for third-party provider authentication flows where Supabase acts as the OAuth client."));

            entity.HasIndex(e => e.CreatedAt, "idx_oauth_client_states_created_at");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CodeVerifier).HasColumnName("code_verifier");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ProviderType).HasColumnName("provider_type");
        });

        modelBuilder.Entity<OauthConsent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("oauth_consents_pkey");

            entity.ToTable("oauth_consents", "auth");

            entity.HasIndex(e => e.ClientId, "oauth_consents_active_client_idx").HasFilter("(revoked_at IS NULL)");

            entity.HasIndex(e => new { e.UserId, e.ClientId }, "oauth_consents_active_user_client_idx").HasFilter("(revoked_at IS NULL)");

            entity.HasIndex(e => new { e.UserId, e.ClientId }, "oauth_consents_user_client_unique").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.GrantedAt }, "oauth_consents_user_order_idx").IsDescending(false, true);

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.GrantedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("granted_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.Scopes).HasColumnName("scopes");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Client).WithMany(p => p.OauthConsents)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("oauth_consents_client_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.OauthConsents)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("oauth_consents_user_id_fkey");
        });

        modelBuilder.Entity<Object>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("objects_pkey");

            entity.ToTable("objects", "storage");

            entity.HasIndex(e => new { e.BucketId, e.Name }, "bucketid_objname").IsUnique();

            entity.HasIndex(e => new { e.BucketId, e.Name }, "idx_objects_bucket_id_name").UseCollation(new[] { null, "C" });

            entity.HasIndex(e => e.Name, "name_prefix_search").HasOperators(new[] { "text_pattern_ops" });

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BucketId).HasColumnName("bucket_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LastAccessedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_accessed_at");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Owner)
                .HasComment("Field is deprecated, use owner_id instead")
                .HasColumnName("owner");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.PathTokens)
                .HasComputedColumnSql("string_to_array(name, '/'::text)", true)
                .HasColumnName("path_tokens");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserMetadata)
                .HasColumnType("jsonb")
                .HasColumnName("user_metadata");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.Bucket).WithMany(p => p.Objects)
                .HasForeignKey(d => d.BucketId)
                .HasConstraintName("objects_bucketId_fkey");
        });

        modelBuilder.Entity<OneTimeToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("one_time_tokens_pkey");

            entity.ToTable("one_time_tokens", "auth");

            entity.HasIndex(e => e.RelatesTo, "one_time_tokens_relates_to_hash_idx").HasMethod("hash");

            entity.HasIndex(e => e.TokenHash, "one_time_tokens_token_hash_hash_idx").HasMethod("hash");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.RelatesTo).HasColumnName("relates_to");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.OneTimeTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("one_time_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("Post_pkey");

            entity.ToTable("Post");

            entity.Property(e => e.PostId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("PostID");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(255);
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

        modelBuilder.Entity<PostAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PostAttachment_pkey");

            entity.ToTable("PostAttachment");

            entity.Property(e => e.AttachmentId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("AttachmentID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FileType).HasMaxLength(100);
            entity.Property(e => e.FileUrl).HasColumnName("FileURL");
            entity.Property(e => e.PostId).HasColumnName("PostID");

            entity.HasOne(d => d.Post).WithMany(p => p.PostAttachments)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("PostAttachment_PostID_fkey");
        });

        modelBuilder.Entity<ProgressReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("ProgressReport_pkey");

            entity.ToTable("ProgressReport");

            entity.Property(e => e.ReportId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("ReportID");
            entity.Property(e => e.AttendanceRate).HasPrecision(5, 2);
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Gpa)
                .HasPrecision(5, 2)
                .HasColumnName("GPA");
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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens", "auth", tb => tb.HasComment("Auth: Store of tokens used to refresh JWT tokens once they expire."));

            entity.HasIndex(e => e.InstanceId, "refresh_tokens_instance_id_idx");

            entity.HasIndex(e => new { e.InstanceId, e.UserId }, "refresh_tokens_instance_id_user_id_idx");

            entity.HasIndex(e => e.Parent, "refresh_tokens_parent_idx");

            entity.HasIndex(e => new { e.SessionId, e.Revoked }, "refresh_tokens_session_id_revoked_idx");

            entity.HasIndex(e => e.Token, "refresh_tokens_token_unique").IsUnique();

            entity.HasIndex(e => e.UpdatedAt, "refresh_tokens_updated_at_idx").IsDescending();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.InstanceId).HasColumnName("instance_id");
            entity.Property(e => e.Parent)
                .HasMaxLength(255)
                .HasColumnName("parent");
            entity.Property(e => e.Revoked).HasColumnName("revoked");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Token)
                .HasMaxLength(255)
                .HasColumnName("token");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Session).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("refresh_tokens_session_id_fkey");
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

        modelBuilder.Entity<S3MultipartUpload>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("s3_multipart_uploads_pkey");

            entity.ToTable("s3_multipart_uploads", "storage");

            entity.HasIndex(e => new { e.BucketId, e.Key, e.CreatedAt }, "idx_multipart_uploads_list").UseCollation(new[] { null, "C", null });

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BucketId).HasColumnName("bucket_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.InProgressSize)
                .HasDefaultValue(0L)
                .HasColumnName("in_progress_size");
            entity.Property(e => e.Key)
                .UseCollation("C")
                .HasColumnName("key");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.UploadSignature).HasColumnName("upload_signature");
            entity.Property(e => e.UserMetadata)
                .HasColumnType("jsonb")
                .HasColumnName("user_metadata");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.Bucket).WithMany(p => p.S3MultipartUploads)
                .HasForeignKey(d => d.BucketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("s3_multipart_uploads_bucket_id_fkey");
        });

        modelBuilder.Entity<S3MultipartUploadsPart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("s3_multipart_uploads_parts_pkey");

            entity.ToTable("s3_multipart_uploads_parts", "storage");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BucketId).HasColumnName("bucket_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Etag).HasColumnName("etag");
            entity.Property(e => e.Key)
                .UseCollation("C")
                .HasColumnName("key");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.PartNumber).HasColumnName("part_number");
            entity.Property(e => e.Size)
                .HasDefaultValue(0L)
                .HasColumnName("size");
            entity.Property(e => e.UploadId).HasColumnName("upload_id");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.Bucket).WithMany(p => p.S3MultipartUploadsParts)
                .HasForeignKey(d => d.BucketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("s3_multipart_uploads_parts_bucket_id_fkey");

            entity.HasOne(d => d.Upload).WithMany(p => p.S3MultipartUploadsParts)
                .HasForeignKey(d => d.UploadId)
                .HasConstraintName("s3_multipart_uploads_parts_upload_id_fkey");
        });

        modelBuilder.Entity<SamlProvider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("saml_providers_pkey");

            entity.ToTable("saml_providers", "auth", tb => tb.HasComment("Auth: Manages SAML Identity Provider connections."));

            entity.HasIndex(e => e.EntityId, "saml_providers_entity_id_key").IsUnique();

            entity.HasIndex(e => e.SsoProviderId, "saml_providers_sso_provider_id_idx");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AttributeMapping)
                .HasColumnType("jsonb")
                .HasColumnName("attribute_mapping");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.MetadataUrl).HasColumnName("metadata_url");
            entity.Property(e => e.MetadataXml).HasColumnName("metadata_xml");
            entity.Property(e => e.NameIdFormat).HasColumnName("name_id_format");
            entity.Property(e => e.SsoProviderId).HasColumnName("sso_provider_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.SsoProvider).WithMany(p => p.SamlProviders)
                .HasForeignKey(d => d.SsoProviderId)
                .HasConstraintName("saml_providers_sso_provider_id_fkey");
        });

        modelBuilder.Entity<SamlRelayState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("saml_relay_states_pkey");

            entity.ToTable("saml_relay_states", "auth", tb => tb.HasComment("Auth: Contains SAML Relay State information for each Service Provider initiated login."));

            entity.HasIndex(e => e.CreatedAt, "saml_relay_states_created_at_idx").IsDescending();

            entity.HasIndex(e => e.ForEmail, "saml_relay_states_for_email_idx");

            entity.HasIndex(e => e.SsoProviderId, "saml_relay_states_sso_provider_id_idx");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.FlowStateId).HasColumnName("flow_state_id");
            entity.Property(e => e.ForEmail).HasColumnName("for_email");
            entity.Property(e => e.RedirectTo).HasColumnName("redirect_to");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.SsoProviderId).HasColumnName("sso_provider_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.FlowState).WithMany(p => p.SamlRelayStates)
                .HasForeignKey(d => d.FlowStateId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("saml_relay_states_flow_state_id_fkey");

            entity.HasOne(d => d.SsoProvider).WithMany(p => p.SamlRelayStates)
                .HasForeignKey(d => d.SsoProviderId)
                .HasConstraintName("saml_relay_states_sso_provider_id_fkey");
        });

        modelBuilder.Entity<SchemaMigration>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("schema_migrations_pkey");

            entity.ToTable("schema_migrations", "auth", tb => tb.HasComment("Auth: Manages updates to the auth system."));

            entity.Property(e => e.Version)
                .HasMaxLength(255)
                .HasColumnName("version");
        });

        modelBuilder.Entity<SchemaMigration1>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("schema_migrations_pkey");

            entity.ToTable("schema_migrations", "realtime");

            entity.Property(e => e.Version)
                .ValueGeneratedNever()
                .HasColumnName("version");
            entity.Property(e => e.InsertedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("inserted_at");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sessions_pkey");

            entity.ToTable("sessions", "auth", tb => tb.HasComment("Auth: Stores session data associated to a user."));

            entity.HasIndex(e => e.NotAfter, "sessions_not_after_idx").IsDescending();

            entity.HasIndex(e => e.OauthClientId, "sessions_oauth_client_id_idx");

            entity.HasIndex(e => e.UserId, "sessions_user_id_idx");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "user_id_created_at_idx");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.FactorId).HasColumnName("factor_id");
            entity.Property(e => e.Ip).HasColumnName("ip");
            entity.Property(e => e.NotAfter)
                .HasComment("Auth: Not after is a nullable column that contains a timestamp after which the session should be regarded as expired.")
                .HasColumnName("not_after");
            entity.Property(e => e.OauthClientId).HasColumnName("oauth_client_id");
            entity.Property(e => e.RefreshTokenCounter)
                .HasComment("Holds the ID (counter) of the last issued refresh token.")
                .HasColumnName("refresh_token_counter");
            entity.Property(e => e.RefreshTokenHmacKey)
                .HasComment("Holds a HMAC-SHA256 key used to sign refresh tokens for this session.")
                .HasColumnName("refresh_token_hmac_key");
            entity.Property(e => e.RefreshedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("refreshed_at");
            entity.Property(e => e.Scopes).HasColumnName("scopes");
            entity.Property(e => e.Tag).HasColumnName("tag");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.OauthClient).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.OauthClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sessions_oauth_client_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("sessions_user_id_fkey");
        });

        modelBuilder.Entity<Session1>(entity =>
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
            entity.Property(e => e.Topic).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Class).WithMany(p => p.Session1s)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Session_ClassID_fkey");
        });

        modelBuilder.Entity<SsoDomain>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sso_domains_pkey");

            entity.ToTable("sso_domains", "auth", tb => tb.HasComment("Auth: Manages SSO email address domain mapping to an SSO Identity Provider."));

            entity.HasIndex(e => e.SsoProviderId, "sso_domains_sso_provider_id_idx");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Domain).HasColumnName("domain");
            entity.Property(e => e.SsoProviderId).HasColumnName("sso_provider_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.SsoProvider).WithMany(p => p.SsoDomains)
                .HasForeignKey(d => d.SsoProviderId)
                .HasConstraintName("sso_domains_sso_provider_id_fkey");
        });

        modelBuilder.Entity<SsoProvider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sso_providers_pkey");

            entity.ToTable("sso_providers", "auth", tb => tb.HasComment("Auth: Manages SSO identity provider information; see saml_providers for SAML."));

            entity.HasIndex(e => e.ResourceId, "sso_providers_resource_id_pattern_idx").HasOperators(new[] { "text_pattern_ops" });

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Disabled).HasColumnName("disabled");
            entity.Property(e => e.ResourceId)
                .HasComment("Auth: Uniquely identifies a SSO provider according to a user-chosen resource ID (case insensitive), useful in infrastructure as code.")
                .HasColumnName("resource_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
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

        modelBuilder.Entity<SubmissionAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("SubmissionAttachment_pkey");

            entity.ToTable("SubmissionAttachment");

            entity.Property(e => e.AttachmentId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("AttachmentID");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.FileName).HasColumnType("character varying");
            entity.Property(e => e.FileType).HasColumnType("character varying");
            entity.Property(e => e.FileUrl).HasColumnName("FileURL");
            entity.Property(e => e.SubmissionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("SubmissionID");

            entity.HasOne(d => d.Submission).WithMany(p => p.SubmissionAttachments)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("SubmissionAttachment_SubmissionID_fkey");
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

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_subscription");

            entity.ToTable("subscription", "realtime");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ActionFilter)
                .HasDefaultValueSql("'*'::text")
                .HasColumnName("action_filter");
            entity.Property(e => e.Claims)
                .HasColumnType("jsonb")
                .HasColumnName("claims");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("timezone('utc'::text, now())")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
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
            entity.Property(e => e.ClassTaid).HasColumnName("ClassTAID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.DueDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Todo'::character varying");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Type).HasColumnType("character varying");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.ClassTa).WithMany(p => p.TeachingAssistantTasks)
                .HasForeignKey(d => d.ClassTaid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TeachingAssistantTask_ClassTAID_fkey");
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

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("Transaction_ApprovedBy_fkey");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Transaction_InvoiceID_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", "auth", tb => tb.HasComment("Auth: Stores user login data within a secure schema."));

            entity.HasIndex(e => e.ConfirmationToken, "confirmation_token_idx")
                .IsUnique()
                .HasFilter("((confirmation_token)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.EmailChangeTokenCurrent, "email_change_token_current_idx")
                .IsUnique()
                .HasFilter("((email_change_token_current)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.EmailChangeTokenNew, "email_change_token_new_idx")
                .IsUnique()
                .HasFilter("((email_change_token_new)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.ReauthenticationToken, "reauthentication_token_idx")
                .IsUnique()
                .HasFilter("((reauthentication_token)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.RecoveryToken, "recovery_token_idx")
                .IsUnique()
                .HasFilter("((recovery_token)::text !~ '^[0-9 ]*$'::text)");

            entity.HasIndex(e => e.Email, "users_email_partial_key")
                .IsUnique()
                .HasFilter("(is_sso_user = false)");

            entity.HasIndex(e => e.InstanceId, "users_instance_id_idx");

            entity.HasIndex(e => e.IsAnonymous, "users_is_anonymous_idx");

            entity.HasIndex(e => e.Phone, "users_phone_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Aud)
                .HasMaxLength(255)
                .HasColumnName("aud");
            entity.Property(e => e.BannedUntil).HasColumnName("banned_until");
            entity.Property(e => e.ConfirmationSentAt).HasColumnName("confirmation_sent_at");
            entity.Property(e => e.ConfirmationToken)
                .HasMaxLength(255)
                .HasColumnName("confirmation_token");
            entity.Property(e => e.ConfirmedAt)
                .HasComputedColumnSql("LEAST(email_confirmed_at, phone_confirmed_at)", true)
                .HasColumnName("confirmed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailChange)
                .HasMaxLength(255)
                .HasColumnName("email_change");
            entity.Property(e => e.EmailChangeConfirmStatus)
                .HasDefaultValue((short)0)
                .HasColumnName("email_change_confirm_status");
            entity.Property(e => e.EmailChangeSentAt).HasColumnName("email_change_sent_at");
            entity.Property(e => e.EmailChangeTokenCurrent)
                .HasMaxLength(255)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("email_change_token_current");
            entity.Property(e => e.EmailChangeTokenNew)
                .HasMaxLength(255)
                .HasColumnName("email_change_token_new");
            entity.Property(e => e.EmailConfirmedAt).HasColumnName("email_confirmed_at");
            entity.Property(e => e.EncryptedPassword)
                .HasMaxLength(255)
                .HasColumnName("encrypted_password");
            entity.Property(e => e.InstanceId).HasColumnName("instance_id");
            entity.Property(e => e.InvitedAt).HasColumnName("invited_at");
            entity.Property(e => e.IsAnonymous)
                .HasDefaultValue(false)
                .HasColumnName("is_anonymous");
            entity.Property(e => e.IsSsoUser)
                .HasDefaultValue(false)
                .HasComment("Auth: Set this column to true when the account comes from SSO. These accounts can have duplicate emails.")
                .HasColumnName("is_sso_user");
            entity.Property(e => e.IsSuperAdmin).HasColumnName("is_super_admin");
            entity.Property(e => e.LastSignInAt).HasColumnName("last_sign_in_at");
            entity.Property(e => e.Phone)
                .HasDefaultValueSql("NULL::character varying")
                .HasColumnName("phone");
            entity.Property(e => e.PhoneChange)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("phone_change");
            entity.Property(e => e.PhoneChangeSentAt).HasColumnName("phone_change_sent_at");
            entity.Property(e => e.PhoneChangeToken)
                .HasMaxLength(255)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("phone_change_token");
            entity.Property(e => e.PhoneConfirmedAt).HasColumnName("phone_confirmed_at");
            entity.Property(e => e.RawAppMetaData)
                .HasColumnType("jsonb")
                .HasColumnName("raw_app_meta_data");
            entity.Property(e => e.RawUserMetaData)
                .HasColumnType("jsonb")
                .HasColumnName("raw_user_meta_data");
            entity.Property(e => e.ReauthenticationSentAt).HasColumnName("reauthentication_sent_at");
            entity.Property(e => e.ReauthenticationToken)
                .HasMaxLength(255)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("reauthentication_token");
            entity.Property(e => e.RecoverySentAt).HasColumnName("recovery_sent_at");
            entity.Property(e => e.RecoveryToken)
                .HasMaxLength(255)
                .HasColumnName("recovery_token");
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<VectorIndex>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vector_indexes_pkey");

            entity.ToTable("vector_indexes", "storage");

            entity.HasIndex(e => new { e.Name, e.BucketId }, "vector_indexes_name_bucket_id_idx")
                .IsUnique()
                .UseCollation(new[] { "C", null });

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BucketId).HasColumnName("bucket_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DataType).HasColumnName("data_type");
            entity.Property(e => e.Dimension).HasColumnName("dimension");
            entity.Property(e => e.DistanceMetric).HasColumnName("distance_metric");
            entity.Property(e => e.MetadataConfiguration)
                .HasColumnType("jsonb")
                .HasColumnName("metadata_configuration");
            entity.Property(e => e.Name)
                .UseCollation("C")
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Bucket).WithMany(p => p.VectorIndices)
                .HasForeignKey(d => d.BucketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vector_indexes_bucket_id_fkey");
        });

        modelBuilder.Entity<WebauthnChallenge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("webauthn_challenges_pkey");

            entity.ToTable("webauthn_challenges", "auth");

            entity.HasIndex(e => e.ExpiresAt, "webauthn_challenges_expires_at_idx");

            entity.HasIndex(e => e.UserId, "webauthn_challenges_user_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChallengeType).HasColumnName("challenge_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.SessionData)
                .HasColumnType("jsonb")
                .HasColumnName("session_data");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.WebauthnChallenges)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("webauthn_challenges_user_id_fkey");
        });

        modelBuilder.Entity<WebauthnCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("webauthn_credentials_pkey");

            entity.ToTable("webauthn_credentials", "auth");

            entity.HasIndex(e => e.CredentialId, "webauthn_credentials_credential_id_key").IsUnique();

            entity.HasIndex(e => e.UserId, "webauthn_credentials_user_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Aaguid).HasColumnName("aaguid");
            entity.Property(e => e.AttestationType)
                .HasDefaultValueSql("''::text")
                .HasColumnName("attestation_type");
            entity.Property(e => e.BackedUp)
                .HasDefaultValue(false)
                .HasColumnName("backed_up");
            entity.Property(e => e.BackupEligible)
                .HasDefaultValue(false)
                .HasColumnName("backup_eligible");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.FriendlyName)
                .HasDefaultValueSql("''::text")
                .HasColumnName("friendly_name");
            entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
            entity.Property(e => e.PublicKey).HasColumnName("public_key");
            entity.Property(e => e.SignCount)
                .HasDefaultValue(0L)
                .HasColumnName("sign_count");
            entity.Property(e => e.Transports)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("transports");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.WebauthnCredentials)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("webauthn_credentials_user_id_fkey");
        });
        modelBuilder.HasSequence<int>("seq_schema_version", "graphql").IsCyclic();

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
