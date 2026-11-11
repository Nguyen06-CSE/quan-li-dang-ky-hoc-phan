using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;

namespace QuanLyDKHP.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<NguoiDung> NguoiDungs { get; set; } = null!;
    public DbSet<SinhVien> SinhViens { get; set; } = null!;
    public DbSet<MonHoc> MonHocs { get; set; } = null!;
    public DbSet<HocKy> HocKys { get; set; } = null!;
    public DbSet<LopHocPhan> LopHocPhans { get; set; } = null!;
    public DbSet<DangKyHocPhan> DangKyHocPhans { get; set; } = null!;
    public DbSet<CauHinhHeThong> CauHinhHeThongs { get; set; } = null!;
    public DbSet<HocPhiHocKy> HocPhiHocKys { get; set; } = null!;
    public DbSet<SyncMetadata> SyncMetadatas { get; set; } = null!;

    public static string GetPrimarySqlitePath()
    {
        // 1. Kiểm tra các vị trí tệp quanlydkhp.db khả dụng trong project hoặc thư mục làm việc
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "QuanLyDKHP", "quanlydkhp.db"),
            Path.Combine(Directory.GetCurrentDirectory(), "Library", "Application Support", "QuanLyDKHP", "quanlydkhp.db"),
            Path.Combine(Directory.GetCurrentDirectory(), "quanlydkhp.db"),
            Path.Combine(AppContext.BaseDirectory, "quanlydkhp.db"),
            Path.Combine(AppContext.BaseDirectory, "QuanLyDKHP", "quanlydkhp.db")
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c) && new FileInfo(c).Length > 0)
            {
                return Path.GetFullPath(c);
            }
        }

        // 2. Thử đường dẫn AppData chuẩn của hệ điều hành
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home)) home = Environment.GetEnvironmentVariable("HOME") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(home))
            {
                var macSupport = Path.Combine(home, "Library", "Application Support");
                localAppData = Directory.Exists(macSupport) ? macSupport : Path.Combine(home, ".local", "share");
            }
            else
            {
                localAppData = AppContext.BaseDirectory;
            }
        }

        var folder = Path.Combine(localAppData, "QuanLyDKHP");
        Directory.CreateDirectory(folder);
        var targetDb = Path.Combine(folder, "quanlydkhp.db");

        // Nếu DB ở AppData chưa có dữ liệu, nhưng có tệp có sẵn trong project, tự động copy sang
        if (!File.Exists(targetDb) || new FileInfo(targetDb).Length == 0)
        {
            foreach (var c in candidates)
            {
                if (File.Exists(c) && new FileInfo(c).Length > 0)
                {
                    try { File.Copy(c, targetDb, true); return targetDb; } catch { }
                }
            }
        }

        return targetDb;
    }

    public static async Task EnsureTablesAndSeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (db.Database.IsSqlite())
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""NguoiDung"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_NguoiDung"" PRIMARY KEY AUTOINCREMENT,
                    ""TenDangNhap"" TEXT NOT NULL,
                    ""MatKhauHash"" TEXT NOT NULL,
                    ""HoTen"" TEXT NOT NULL,
                    ""Role"" TEXT NOT NULL,
                    ""MaGV"" TEXT NULL,
                    ""TrangThai"" TEXT NOT NULL DEFAULT 'HoatDong',
                    ""NgayTao"" TEXT NOT NULL,
                    ""NgayCapNhat"" TEXT NULL,
                    ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_NguoiDung_TenDangNhap"" ON ""NguoiDung"" (""TenDangNhap"");
            ");

            if (!await db.NguoiDungs.AnyAsync())
            {
                await db.NguoiDungs.AddAsync(new NguoiDung
                {
                    Id = 1,
                    TenDangNhap = "admin",
                    MatKhauHash = "$2a$11$jZTP6M2fUm3QlomDlyrWIO6IRd8BeVrcyOhY/wlXprA1A8iFII/LO", // pass: admin
                    HoTen = "Quản trị viên",
                    Role = "Admin",
                    TrangThai = "HoatDong",
                    NgayTao = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        bool isSqlite = Database.IsSqlite();

        // NguoiDung
        modelBuilder.Entity<NguoiDung>(b =>
        {
            b.ToTable("NguoiDung");
            b.HasKey(e => e.Id);
            b.Property(e => e.TenDangNhap).HasMaxLength(50).IsRequired();
            b.HasIndex(e => e.TenDangNhap).IsUnique();
            b.Property(e => e.MatKhauHash).HasMaxLength(255).IsRequired();
            b.Property(e => e.HoTen).HasMaxLength(100).IsRequired();
            b.Property(e => e.Role).HasMaxLength(30).IsRequired();
            b.Property(e => e.MaGV).HasMaxLength(20);
            b.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("HoatDong");
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        // SinhVien
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

        // MonHoc
        modelBuilder.Entity<MonHoc>(b =>
        {
            b.ToTable("MonHoc");
            b.HasKey(e => e.MaMon);
            b.Property(e => e.MaMon).HasMaxLength(20);
            b.Property(e => e.TenMon).HasMaxLength(150).IsRequired();
            b.Property(e => e.SoTinChiLT).HasDefaultValue(0);
            b.Property(e => e.SoTinChiTH).HasDefaultValue(0);
            b.Property(e => e.BacDaoTao).HasMaxLength(10);
            b.Property(e => e.IsDeleted).HasDefaultValue(false);
        });

        // HocKy
        modelBuilder.Entity<HocKy>(b =>
        {
            b.ToTable("HocKy");
            b.HasKey(e => e.MaHocKy);
            b.Property(e => e.MaHocKy).HasMaxLength(20);
            b.Property(e => e.TenHocKy).HasMaxLength(50).IsRequired();
            b.Property(e => e.DangMo).HasDefaultValue(true);
        });

        // LopHocPhan
        modelBuilder.Entity<LopHocPhan>(b =>
        {
            b.ToTable("LopHocPhan");
            b.HasKey(e => e.MaLHP);
            b.Property(e => e.MaLHP).HasMaxLength(30);
            b.Property(e => e.MaMon).HasMaxLength(20).IsRequired();
            b.Property(e => e.MaHocKy).HasMaxLength(20).IsRequired();
            b.Property(e => e.MaGV).HasMaxLength(20);
            b.Property(e => e.LoaiHinhDT).HasMaxLength(10);
            b.Property(e => e.GiangDayOnline).HasDefaultValue(false);
            b.Property(e => e.Phong).HasMaxLength(20);
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
            if (!isSqlite)
            {
                b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            }
            b.Property(e => e.MaSV).HasMaxLength(20).IsRequired();
            b.Property(e => e.MaLHP).HasMaxLength(30).IsRequired();
            b.Property(e => e.HinhThucDK).HasMaxLength(10);
            b.Property(e => e.NguoiDK).HasMaxLength(50);
            b.Property(e => e.DiemSo).HasColumnType("numeric(4,2)");
            b.Property(e => e.DiemChu).HasMaxLength(5);
            b.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("DangHoc");
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
            b.Property(e => e.Key).HasMaxLength(50);
            b.Property(e => e.Value).HasMaxLength(50).IsRequired();
            b.Property(e => e.MoTa).HasMaxLength(200);
        });

        // HocPhiHocKy
        modelBuilder.Entity<HocPhiHocKy>(b =>
        {
            b.ToTable("HocPhiHocKy");
            b.HasKey(e => e.Id);
            if (!isSqlite)
            {
                b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            }
            b.Property(e => e.MaSV).HasMaxLength(20).IsRequired();
            b.Property(e => e.MaHocKy).HasMaxLength(20).IsRequired();
            b.Property(e => e.TongSoTinChi).HasDefaultValue(0);
            b.Property(e => e.TongHocPhi).HasColumnType("numeric(12,2)").HasDefaultValue(0);
            b.Property(e => e.DaDong).HasColumnType("numeric(12,2)").HasDefaultValue(0);
            b.Property(e => e.ConNo).HasColumnType("numeric(12,2)").HasDefaultValue(0);
            b.Property(e => e.DaKhoaSo).HasDefaultValue(false);

            b.HasIndex(e => new { e.MaSV, e.MaHocKy }).IsUnique();

            b.HasOne(e => e.SinhVien)
             .WithMany()
             .HasForeignKey(e => e.MaSV)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.HocKy)
             .WithMany()
             .HasForeignKey(e => e.MaHocKy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // SyncMetadata
        modelBuilder.Entity<SyncMetadata>(b =>
        {
            b.ToTable("SyncMetadata");
            b.HasKey(e => e.TableName);
            b.Property(e => e.TableName).HasMaxLength(64);
        });

        // Global auditing fields config
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                if (!isSqlite)
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
                if (entry.Entity.NgayTao == default)
                {
                    entry.Entity.NgayTao = DateTime.UtcNow;
                }
                if (entry.Entity is DangKyHocPhan dk && dk.Id == Guid.Empty)
                {
                    dk.Id = Guid.NewGuid();
                }
                if (entry.Entity is HocPhiHocKy hp && hp.Id == Guid.Empty)
                {
                    hp.Id = Guid.NewGuid();
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.NgayCapNhat = DateTime.UtcNow;
            }

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
