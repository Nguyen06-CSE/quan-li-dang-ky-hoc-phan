import os

with open("src/QuanLyDKHP.Infrastructure/Data/AppDbContext.cs", "w") as f:
    f.write("""using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;

namespace QuanLyDKHP.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<NguoiDung> NguoiDungs { get; set; } = null!;
    public DbSet<SinhVien> SinhViens { get; set; } = null!;
    public DbSet<MonHoc> MonHocs { get; set; } = null!;
    public DbSet<HocKy> HocKys { get; set; } = null!;
    public DbSet<LopHocPhan> LopHocPhans { get; set; } = null!;
    public DbSet<DangKyHocPhan> DangKyHocPhans { get; set; } = null!;
    public DbSet<CauHinhHeThong> CauHinhHeThongs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // NguoiDung
        modelBuilder.Entity<NguoiDung>(b =>
        {
            b.ToTable("NguoiDung");
            b.HasKey(e => e.Id);
            b.Property(e => e.TenDangNhap).HasColumnType("varchar(50)").IsRequired();
            b.HasIndex(e => e.TenDangNhap).IsUnique();
            b.Property(e => e.MatKhauHash).HasColumnType("varchar(255)").IsRequired();
            b.Property(e => e.HoTen).HasColumnType("varchar(100)").IsRequired();
            b.Property(e => e.Role).HasColumnType("varchar(30)").IsRequired();
            b.Property(e => e.MaGV).HasColumnType("varchar(20)");
            b.Property(e => e.TrangThai).HasColumnType("varchar(20)").HasDefaultValue("HoatDong");
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        // SinhVien
        modelBuilder.Entity<SinhVien>(b =>
        {
            b.ToTable("SinhVien");
            b.HasKey(e => e.MaSV);
            b.Property(e => e.MaSV).HasColumnType("varchar(20)");
            b.Property(e => e.HoTen).HasColumnType("varchar(100)").IsRequired();
            b.Property(e => e.LopSinhHoat).HasColumnType("varchar(30)");
            b.Property(e => e.KhoaHoc).HasColumnType("varchar(20)");
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        // MonHoc
        modelBuilder.Entity<MonHoc>(b =>
        {
            b.ToTable("MonHoc");
            b.HasKey(e => e.MaMon);
            b.Property(e => e.MaMon).HasColumnType("varchar(20)");
            b.Property(e => e.TenMon).HasColumnType("varchar(150)").IsRequired();
            b.Property(e => e.SoTinChiLT).HasDefaultValue(0);
            b.Property(e => e.SoTinChiTH).HasDefaultValue(0);
            b.Property(e => e.BacDaoTao).HasColumnType("varchar(10)");
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
            b.ToTable(t => t.HasCheckConstraint("CK_MonHoc_SoTinChi", "SoTinChiLT + SoTinChiTH > 0"));
        });

        // HocKy
        modelBuilder.Entity<HocKy>(b =>
        {
            b.ToTable("HocKy");
            b.HasKey(e => e.MaHocKy);
            b.Property(e => e.MaHocKy).HasColumnType("varchar(20)");
            b.Property(e => e.TenHocKy).HasColumnType("varchar(50)").IsRequired();
            b.Property(e => e.DangMo).HasDefaultValue(true);
            b.Property(e => e.NgayBatDau).HasColumnType("date");
            b.Property(e => e.NgayKetThuc).HasColumnType("date");
        });

        // LopHocPhan
        modelBuilder.Entity<LopHocPhan>(b =>
        {
            b.ToTable("LopHocPhan");
            b.HasKey(e => e.MaLHP);
            b.Property(e => e.MaLHP).HasColumnType("varchar(30)");
            b.Property(e => e.MaMon).HasColumnType("varchar(20)").IsRequired();
            b.Property(e => e.MaHocKy).HasColumnType("varchar(20)").IsRequired();
            b.Property(e => e.MaGV).HasColumnType("varchar(20)");
            b.Property(e => e.LoaiHinhDT).HasColumnType("varchar(10)");
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

        // DangKyHocPhan
        modelBuilder.Entity<DangKyHocPhan>(b =>
        {
            b.ToTable("DangKyHocPhan");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            b.Property(e => e.MaSV).HasColumnType("varchar(20)").IsRequired();
            b.Property(e => e.MaLHP).HasColumnType("varchar(30)").IsRequired();
            b.Property(e => e.HinhThucDK).HasColumnType("varchar(10)");
            b.Property(e => e.NguoiDK).HasColumnType("varchar(50)");
            b.Property(e => e.DiemSo).HasColumnType("numeric(4,2)");
            b.Property(e => e.DiemChu).HasColumnType("varchar(5)");
            b.Property(e => e.TrangThai).HasColumnType("varchar(20)").HasDefaultValue("DangHoc");
            b.Property(e => e.SoTienPhaiDong).HasColumnType("numeric(12,2)");
            b.Property(e => e.SoTienDaDong).HasColumnType("numeric(12,2)").HasDefaultValue(0);

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

        // CauHinhHeThong
        modelBuilder.Entity<CauHinhHeThong>(b =>
        {
            b.ToTable("CauHinhHeThong");
            b.HasKey(e => e.Key);
            b.Property(e => e.Key).HasColumnType("varchar(50)");
            b.Property(e => e.Value).HasColumnType("varchar(50)").IsRequired();
            b.Property(e => e.MoTa).HasColumnType("varchar(200)");
        });

        // Global auditing fields config
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IAuditableEntity.NgayTao))
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");
                
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IAuditableEntity.NgayCapNhat))
                    .HasColumnType("timestamptz");
            }
        }

        // Seed data
        modelBuilder.Entity<CauHinhHeThong>().HasData(
            new CauHinhHeThong { Key = "SoTinChiToiThieu", Value = "10", MoTa = "Số TC tối thiểu/học kỳ", NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new CauHinhHeThong { Key = "SoTinChiToiDa", Value = "25", MoTa = "Số TC tối đa/học kỳ", NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new CauHinhHeThong { Key = "DonGiaTinChiLT", Value = "500000", MoTa = "Đơn giá 1 TC lý thuyết (VNĐ)", NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new CauHinhHeThong { Key = "DonGiaTinChiTH", Value = "700000", MoTa = "Đơn giá 1 TC thực hành (VNĐ)", NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<NguoiDung>().HasData(
            new NguoiDung
            {
                Id = 1,
                TenDangNhap = "admin",
                MatKhauHash = "$2a$11$jZTP6M2fUm3QlomDlyrWIO6IRd8BeVrcyOhY/wlXprA1A8iFII/LO", // pass: admin
                HoTen = "Quản trị viên",
                Role = "Admin",
                TrangThai = "HoatDong",
                NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // NgayTao handled by DB
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.NgayCapNhat = DateTime.UtcNow;
            }
        }
    }
}
""")
