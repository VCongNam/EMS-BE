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
    public class ClassEnrollmentConfiguration : IEntityTypeConfiguration<ClassEnrollment>
    {
        public void Configure(EntityTypeBuilder<ClassEnrollment> builder)
        {
            builder.ToTable("classenrollment");

            builder.HasKey(ce => ce.EnrollmentId);
            builder.Property(ce => ce.EnrollmentId).HasColumnName("enrollmentid");
            builder.Property(ce => ce.ClassId).HasColumnName("classid").IsRequired();
            builder.Property(ce => ce.StudentId).HasColumnName("studentid").IsRequired();
            builder.Property(ce => ce.EnrolledDate).HasColumnName("enrolleddate").HasColumnType("date");
            builder.Property(ce => ce.DroppedDate).HasColumnName("droppeddate").HasColumnType("date");
            builder.Property(ce => ce.Status).HasColumnName("status").HasMaxLength(50);
            builder.Property(ce => ce.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("now()");
            builder.Property(ce => ce.UpdatedAt).HasColumnName("updatedat");

            builder.HasOne(ce => ce.Class)
                   .WithMany(c => c.ClassEnrollments)
                   .HasForeignKey(ce => ce.ClassId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ce => ce.Student)
                   .WithMany(s => s.ClassEnrollments)
                   .HasForeignKey(ce => ce.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
