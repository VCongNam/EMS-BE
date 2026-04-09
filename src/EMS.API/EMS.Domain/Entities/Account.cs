using System;
using System.Collections.Generic;

namespace EMS.API.EMS.Domain.Entities;

public partial class Account
{
    public Guid AccountId { get; set; }

    public Guid RoleId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? VerificationToken { get; set; }

    public DateTime? VerificationTokenExpiresAt { get; set; }

    public string? ResetPasswordToken { get; set; }

    public DateTime? ResetPasswordTokenExpiresAt { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<LearningMaterial> LearningMaterials { get; set; } = new List<LearningMaterial>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual Role Role { get; set; } = null!;

    public virtual Student? Student { get; set; }

    public virtual ICollection<SubmissionFeedback> SubmissionFeedbacks { get; set; } = new List<SubmissionFeedback>();

    public virtual ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();

    public virtual Teacher? Teacher { get; set; }

    public virtual TeachingAssistant? TeachingAssistant { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
