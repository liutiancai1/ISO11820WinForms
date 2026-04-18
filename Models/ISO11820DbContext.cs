using ISO11820WinForms.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestServer.Models;

namespace ISO11820WinForms.Models
{
    public partial class ISO11820DbContext : DbContext
    {
        public ISO11820DbContext()
        {
        }

        public ISO11820DbContext(DbContextOptions<ISO11820DbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Apparatus> Apparatuses { get; set; }
        public virtual DbSet<Operator> Operators { get; set; }
        public virtual DbSet<Productmaster> Productmasters { get; set; }
        public virtual DbSet<Testmaster> Testmasters { get; set; }
        public virtual DbSet<Sensor> Sensors { get; set; } = null!;
        public virtual DbSet<CalibrationRecord> CalibrationRecords { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 从配置文件读取连接字符串
                string connectionString = ConfigurationHelper.GetConnectionString("ISO11820");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Testmaster>(entity =>
            {
                entity.HasKey(e => new { e.Productid, e.Testid });

                entity.Property(e => e.Deltatf).HasComment("[判定项]本次试验样品的最终温升");

                entity.Property(e => e.LostweightPer).HasComment("[判定项]样品质量失重率");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Testmasters)
                    .HasForeignKey(d => d.Productid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_testmaster_productmaster");

                // 性能优化：添加索引以提高查询性能
                // 索引1：按试验日期查询（最常用的查询条件）
                entity.HasIndex(e => e.Testdate)
                    .HasDatabaseName("IX_Testmaster_Testdate");

                // 索引2：按操作员查询
                entity.HasIndex(e => e.Operator)
                    .HasDatabaseName("IX_Testmaster_Operator");

                // 索引3：组合索引 - 日期和样品编号（用于复合查询）
                entity.HasIndex(e => new { e.Testdate, e.Productid })
                    .HasDatabaseName("IX_Testmaster_Testdate_Productid");
            });

            modelBuilder.Entity<Sensor>(entity =>
            {
                entity.Property(e => e.Sensorid).ValueGeneratedNever();
            });

            modelBuilder.Entity<Apparatus>(entity =>
            {
                entity.Property(e => e.Apparatusid).ValueGeneratedNever();
            });

            modelBuilder.Entity<CalibrationRecord>(entity =>
            {
                // 索引：按校验日期查询
                entity.HasIndex(e => e.CalibrationDate)
                    .HasDatabaseName("IX_CalibrationRecord_Date");

                // 索引：按操作员查询
                entity.HasIndex(e => e.Operator)
                    .HasDatabaseName("IX_CalibrationRecord_Operator");

                // 将 TemperatureData 配置为 JSON 列
                entity.Property(e => e.TemperatureData)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<TemperaturePoint>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<TemperaturePoint>()
                    );
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

