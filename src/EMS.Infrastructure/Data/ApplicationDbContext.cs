using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Class> Classes { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 1. Ép EF Core map chính xác với tên BẢNG (số ít)
            modelBuilder.Entity<Account>().ToTable("Account");
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<Class>().ToTable("Class");

            // 2. Ép EF Core map chính xác với tên CỘT có chứa chữ "ID" (viết hoa)
            // Bảng Account
            modelBuilder.Entity<Account>()
                .Property(a => a.AccountId)
                .HasColumnName("AccountID"); // Map chính xác chữ hoa/thường trong DB

            modelBuilder.Entity<Account>()
                .Property(a => a.RoleId)
                .HasColumnName("RoleID");

            // Bảng Role
            modelBuilder.Entity<Role>()
                .Property(r => r.RoleId)
                .HasColumnName("RoleID");

            // 3. Khai báo Khóa ngoại (Foreign Key)
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Role)
                .WithMany(r => r.Accounts)
                .HasForeignKey(a => a.RoleId);

            //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }

}
