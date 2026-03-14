using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Data.Configurations
{
    public class AccountConfiguration
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Account");

            builder.HasKey(a => a.AccountId);

            builder.Property(a => a.AccountId).HasColumnName("AccountID");
            builder.Property(a => a.RoleId).HasColumnName("RoleID").IsRequired();
            builder.Property(a => a.Email).HasColumnName("Email").IsRequired().HasMaxLength(255);
            builder.Property(a => a.PasswordHash).HasColumnName("PasswordHash").IsRequired();
            builder.Property(a => a.FullName).HasColumnName("FullName").IsRequired().HasMaxLength(255);
            builder.Property(a => a.PhoneNumber).HasColumnName("PhoneNumber").HasMaxLength(20);
            builder.Property(a => a.AvatarUrl).HasColumnName("AvatarURL");
            builder.Property(a => a.Status).HasColumnName("Status").HasMaxLength(50);
            builder.Property(a => a.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
            builder.Property(a => a.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("now()");
            builder.Property(a => a.UpdatedAt).HasColumnName("UpdatedAt");

            builder.HasIndex(a => a.Email).IsUnique();
        }
    }
}
