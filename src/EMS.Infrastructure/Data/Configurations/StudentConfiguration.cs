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

            builder.HasKey(s => s.StudentId);

            builder.Property(s => s.StudentId).HasColumnName("studentid");
            builder.Property(s => s.ParentName).HasColumnName("parentname").IsRequired().HasMaxLength(255);
            builder.Property(s => s.ParentPhone).HasColumnName("parentphone").IsRequired().HasMaxLength(20);
            builder.Property(s => s.ParentEmail).HasColumnName("parentemail").HasMaxLength(255);
            builder.Property(s => s.Address).HasColumnName("address");
            builder.Property(s => s.Dob).HasColumnName("dob").HasColumnType("date");
            builder.HasOne(s => s.StudentNavigation)
                   .WithOne(a => a.Student)
                   .HasForeignKey<Student>(s => s.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
