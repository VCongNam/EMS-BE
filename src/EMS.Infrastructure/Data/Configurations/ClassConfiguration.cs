using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Data.Configurations
{
    public class ClassConfiguration : IEntityTypeConfiguration<Class>
    {
        public void Configure(EntityTypeBuilder<Class> builder)
        {
            builder.ToTable("class");

            builder.HasKey(c => c.ClassId);

            builder.Property(c => c.ClassId).HasColumnName("classid"); 
            builder.Property(c => c.TeacherId).HasColumnName("teacherid");
            builder.Property(c => c.ClassName).HasColumnName("classname").IsRequired();
            builder.Property(c => c.StartDate).HasColumnName("startdate").HasColumnType("date"); 
            builder.Property(c => c.EndDate).HasColumnName("enddate").HasColumnType("date");
            builder.Property(c => c.TuitionFee).HasColumnName("tuitionfee").HasColumnType("numeric");
            builder.Property(c => c.Room).HasColumnName("room");
            builder.Property(c => c.Status).HasColumnName("status").IsRequired();
            builder.Property(c => c.IsDeleted).HasColumnName("isdeleted").HasDefaultValue(false);
            builder.Property(c => c.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("now()");
            builder.Property(c => c.UpdatedAt).HasColumnName("updatedat");

            builder.HasIndex(c => c.ClassName);
        }
    }

}
