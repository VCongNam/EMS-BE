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

            builder.Property(a => a.AccountId).HasColumnName("accountid");
            builder.Property(a => a.RoleId).HasColumnName("roleid").IsRequired();
            builder.Property(a => a.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            builder.Property(a => a.PasswordHash).HasColumnName("passwordhash").IsRequired();
            builder.Property(a => a.FullName).HasColumnName("fullname").IsRequired().HasMaxLength(255);
            builder.Property(a => a.PhoneNumber).HasColumnName("phonenumber").HasMaxLength(20);
            builder.Property(a => a.AvatarUrl).HasColumnName("avatarurl");
            builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(50);
            builder.Property(a => a.IsDeleted).HasColumnName("isdeleted").HasDefaultValue(false);
            builder.Property(a => a.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("now()");
            builder.Property(a => a.UpdatedAt).HasColumnName("updatedat");

            builder.HasIndex(a => a.Email).IsUnique();
        }
    }
}
