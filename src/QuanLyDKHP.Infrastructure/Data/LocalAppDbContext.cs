using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Infrastructure.Data;

public class LocalAppDbContext : DbContext
{
    public LocalAppDbContext(DbContextOptions<LocalAppDbContext> options) : base(options)
    {
    }

    public DbSet<SinhVien> SinhViens => Set<SinhVien>();
    public DbSet<MonHoc> MonHocs => Set<MonHoc>();
    public DbSet<HocKy> HocKys => Set<HocKy>();
    public DbSet<LopHocPhan> LopHocPhans => Set<LopHocPhan>();
    public DbSet<DangKyHocPhan> DangKyHocPhans => Set<DangKyHocPhan>();
    public DbSet<SyncMetadata> SyncMetadatas => Set<SyncMetadata>();

    public static string GetDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuanLyDKHP");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "local_cache.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SinhVien>(b =>
        {
            b.ToTable("SinhVien");
            b.HasKey(e => e.MaSV);
            b.Property(e => e.MaSV).HasMaxLength(20);
            b.Property(e => e.HoTen).HasMaxLength(100).IsRequired();
            b.Property(e => e.LopSinhHoat).HasMaxLength(30);
            b.Property(e => e.KhoaHoc).HasMaxLength(20);
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
            b.HasIndex(e => e.LopSinhHoat);
            b.HasIndex(e => e.KhoaHoc);
        });

        modelBuilder.Entity<MonHoc>(b =>
        {
            b.ToTable("MonHoc");
            b.HasKey(e => e.MaMon);
            b.Property(e => e.MaMon).HasMaxLength(20);
            b.Property(e => e.TenMon).HasMaxLength(150).IsRequired();
            b.Property(e => e.BacDaoTao).HasMaxLength(10);
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<HocKy>(b =>
        {
            b.ToTable("HocKy");
            b.HasKey(e => e.MaHocKy);
            b.Property(e => e.MaHocKy).HasMaxLength(20);
            b.Property(e => e.TenHocKy).HasMaxLength(50).IsRequired();
            b.Property(e => e.DangMo).HasDefaultValue(true);
        });

        modelBuilder.Entity<LopHocPhan>(b =>
        {
            b.ToTable("LopHocPhan");
            b.HasKey(e => e.MaLHP);
            b.Property(e => e.MaLHP).HasMaxLength(30);
            b.Property(e => e.MaMon).HasMaxLength(20).IsRequired();
            b.Property(e => e.MaHocKy).HasMaxLength(20).IsRequired();
            b.Property(e => e.MaGV).HasMaxLength(20);
            b.Property(e => e.LoaiHinhDT).HasMaxLength(10);
            b.Property(e => e.Phong).HasMaxLength(20);
            b.Property(e => e.GiangDayOnline).HasDefaultValue(false);
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
            b.HasIndex(e => new { e.MaMon, e.MaHocKy });

            b.HasOne(e => e.MonHoc)
                .WithMany(m => m.LopHocPhans)
                .HasForeignKey(e => e.MaMon)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.HocKy)
                .WithMany(h => h.LopHocPhans)
                .HasForeignKey(e => e.MaHocKy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DangKyHocPhan>(b =>
        {
            b.ToTable("DangKyHocPhan");
            b.HasKey(e => e.Id);
            b.Property(e => e.MaSV).HasMaxLength(20).IsRequired();
            b.Property(e => e.MaLHP).HasMaxLength(30).IsRequired();
            b.Property(e => e.HinhThucDK).HasMaxLength(10);
            b.Property(e => e.NguoiDK).HasMaxLength(50);
            b.Property(e => e.DiemChu).HasMaxLength(5);
            b.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("DangHoc");
            b.HasIndex(e => new { e.MaSV, e.MaLHP }).IsUnique();

            b.HasOne(e => e.SinhVien)
                .WithMany(s => s.DangKyHocPhans)
                .HasForeignKey(e => e.MaSV)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.LopHocPhan)
                .WithMany(l => l.DangKyHocPhans)
                .HasForeignKey(e => e.MaLHP)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SyncMetadata>(b =>
        {
            b.ToTable("SyncMetadata");
            b.HasKey(e => e.TableName);
            b.Property(e => e.TableName).HasMaxLength(64);
        });
    }

    public override int SaveChanges()
    {
        NormalizeAuditDates();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeAuditDates();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void NormalizeAuditDates()
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            entry.Entity.NgayTao = AsUtc(entry.Entity.NgayTao);
            entry.Entity.NgayCapNhat = entry.Entity.NgayCapNhat.HasValue
                ? AsUtc(entry.Entity.NgayCapNhat.Value)
                : null;
        }
    }

    private static DateTime AsUtc(DateTime value)
    {
        if (value == default) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
