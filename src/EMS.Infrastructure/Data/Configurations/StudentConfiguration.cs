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
    public class StudentConfiguration
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("student");

            builder.HasKey(s => s.StudentID);

            builder.Property(s => s.StudentID).HasColumnName("studentid");
            builder.Property(s => s.ParentName).HasColumnName("parentname").IsRequired().HasMaxLength(255);
            builder.Property(s => s.ParentPhone).HasColumnName("parentphone").IsRequired().HasMaxLength(20);
            builder.Property(s => s.ParentEmail).HasColumnName("parentemail").HasMaxLength(255);
            builder.Property(s => s.Address).HasColumnName("address");
            builder.Property(s => s.DOB).HasColumnName("dob").HasColumnType("date");
            builder.HasOne(s => s.Account)
                   .WithOne(a => a.Student)
                   .HasForeignKey<Student>(s => s.StudentID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
