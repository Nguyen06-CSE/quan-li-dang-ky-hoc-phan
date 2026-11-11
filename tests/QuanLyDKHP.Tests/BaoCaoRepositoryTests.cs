using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Infrastructure.Data;
using QuanLyDKHP.Infrastructure.Repositories;
using Xunit;

namespace QuanLyDKHP.Tests;

public class BaoCaoRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly TestDbContextFactory _factory;

    public class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new AppDbContext(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(_options));
    }

    public BaoCaoRepositoryTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();

        _factory = new TestDbContextFactory(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task ThongKeSoSvTheoMonAsync_GroupsByMaMon_ExcludesSoftDeletedAndCancelled()
    {
        using (var context = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026", DangMo = true };
            var mon1 = new MonHoc { MaMon = "CS101", TenMon = "Lập trình C#", SoTinChiLT = 2, SoTinChiTH = 1 };
            var monDeleted = new MonHoc { MaMon = "CS999", TenMon = "Môn Đã Xóa", IsDeleted = true };

            var gv = new NguoiDung { TenDangNhap = "gv_lam", HoTen = "ThS. Liam / Lâm", Role = "GiangVien", MaGV = "GV01" };

            var lhp1 = new LopHocPhan { MaLHP = "LHP01", MaMon = "CS101", MaHocKy = "HK261", MaGV = "GV01", SiSoToiDa = 40 };
            var lhp2 = new LopHocPhan { MaLHP = "LHP02", MaMon = "CS101", MaHocKy = "HK261", MaGV = "GV01", SiSoToiDa = 40 };
            var lhpDeleted = new LopHocPhan { MaLHP = "LHP03", MaMon = "CS101", MaHocKy = "HK261", IsDeleted = true, SiSoToiDa = 40 };
            var lhpMonDel = new LopHocPhan { MaLHP = "LHP04", MaMon = "CS999", MaHocKy = "HK261", SiSoToiDa = 40 };

            var sv1 = new SinhVien { MaSV = "21001", HoTen = "Sinh Viên 1", LopSinhHoat = "CTK45" };
            var sv2 = new SinhVien { MaSV = "21002", HoTen = "Sinh Viên 2", LopSinhHoat = "CTK45" };

            var dk1 = new DangKyHocPhan { MaSV = "21001", MaLHP = "LHP01", TrangThai = "DangHoc" };
            var dk2 = new DangKyHocPhan { MaSV = "21002", MaLHP = "LHP02", TrangThai = "DangHoc" };
            var dkCancelled = new DangKyHocPhan { MaSV = "21001", MaLHP = "LHP02", TrangThai = "DaHuy" };

            context.HocKys.Add(hk);
            context.MonHocs.AddRange(mon1, monDeleted);
            context.NguoiDungs.Add(gv);
            context.LopHocPhans.AddRange(lhp1, lhp2, lhpDeleted, lhpMonDel);
            context.SinhViens.AddRange(sv1, sv2);
            context.DangKyHocPhans.AddRange(dk1, dk2, dkCancelled);
            await context.SaveChangesAsync();
        }

        var repo = new BaoCaoRepository(_factory);
        var stats = await repo.ThongKeSoSvTheoMonAsync("HK261");

        Assert.Single(stats); // Only CS101 should appear; CS999 is soft-deleted
        Assert.Equal("CS101", stats[0].MaMon);
        Assert.Equal(2, stats[0].SoLhpDangMo); // LHP01, LHP02 (LHP03 is soft-deleted)
        Assert.Equal(2, stats[0].SoSinhVien); // sv1, sv2 (dkCancelled is excluded)
    }

    [Fact]
    public async Task LayDsSvTheoMonAsync_ResolvesInstructorFullName_AndExcludesDeletedAndCancelled()
    {
        using (var context = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026" };
            var mon = new MonHoc { MaMon = "CS101", TenMon = "Lập trình C#" };
            var gv = new NguoiDung { TenDangNhap = "gv_lam", HoTen = "ThS. Liam / Lâm", Role = "GiangVien", MaGV = "GV01" };
            var lhp = new LopHocPhan { MaLHP = "LHP01", MaMon = "CS101", MaHocKy = "HK261", MaGV = "GV01" };

            var sv1 = new SinhVien { MaSV = "21001", HoTen = "Nguyễn Văn Active", LopSinhHoat = "CTK45" };
            var sv2 = new SinhVien { MaSV = "21002", HoTen = "Trần Văn Cancelled", LopSinhHoat = "CTK45" };
            var svDeleted = new SinhVien { MaSV = "21003", HoTen = "Lê Văn Deleted", LopSinhHoat = "CTK45", IsDeleted = true };

            context.HocKys.Add(hk);
            context.MonHocs.Add(mon);
            context.NguoiDungs.Add(gv);
            context.LopHocPhans.Add(lhp);
            context.SinhViens.AddRange(sv1, sv2, svDeleted);
            context.DangKyHocPhans.AddRange(
                new DangKyHocPhan { MaSV = "21001", MaLHP = "LHP01", TrangThai = "DangHoc" },
                new DangKyHocPhan { MaSV = "21002", MaLHP = "LHP01", TrangThai = "DaHuy" },
                new DangKyHocPhan { MaSV = "21003", MaLHP = "LHP01", TrangThai = "DangHoc" }
            );
            await context.SaveChangesAsync();
        }

        var repo = new BaoCaoRepository(_factory);
        var ds = await repo.LayDsSvTheoMonAsync("CS101", "HK261");

        Assert.Single(ds);
        Assert.Equal("21001", ds[0].MaSV);
        Assert.Equal("ThS. Liam / Lâm", ds[0].TenGiangVien);
    }

    [Fact]
    public async Task LayDsThiAsync_ResolvesInstructorFullName_AndExcludesDeleted()
    {
        using (var context = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026" };
            var mon = new MonHoc { MaMon = "CS101", TenMon = "Lập trình C#" };
            var gv = new NguoiDung { TenDangNhap = "gv_lam", HoTen = "ThS. Liam / Lâm", Role = "GiangVien", MaGV = "GV01" };
            var lhp = new LopHocPhan { MaLHP = "LHP01", MaMon = "CS101", MaHocKy = "HK261", MaGV = "GV01" };

            var sv1 = new SinhVien { MaSV = "21001", HoTen = "Nguyễn Văn Active", LopSinhHoat = "CTK45" };
            var svDeleted = new SinhVien { MaSV = "21003", HoTen = "Lê Văn Deleted", LopSinhHoat = "CTK45", IsDeleted = true };

            context.HocKys.Add(hk);
            context.MonHocs.Add(mon);
            context.NguoiDungs.Add(gv);
            context.LopHocPhans.Add(lhp);
            context.SinhViens.AddRange(sv1, svDeleted);
            context.DangKyHocPhans.AddRange(
                new DangKyHocPhan { MaSV = "21001", MaLHP = "LHP01", TrangThai = "DangHoc" },
                new DangKyHocPhan { MaSV = "21003", MaLHP = "LHP01", TrangThai = "DangHoc" }
            );
            await context.SaveChangesAsync();
        }

        var repo = new BaoCaoRepository(_factory);
        var dsThi = await repo.LayDsThiAsync("CS101", "HK261", "LHP01");

        Assert.Single(dsThi);
        Assert.Equal("21001", dsThi[0].MaSV);
        Assert.Equal("ThS. Liam / Lâm", dsThi[0].TenGiangVien);
    }

    [Fact]
    public async Task LayPhieuDangKyAsync_ResolvesInstructorFullName()
    {
        using (var context = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026" };
            var mon = new MonHoc { MaMon = "CS101", TenMon = "Lập trình C#", SoTinChiLT = 2, SoTinChiTH = 1 };
            var gv = new NguoiDung { TenDangNhap = "gv_lam", HoTen = "ThS. Liam / Lâm", Role = "GiangVien", MaGV = "GV01" };
            var lhp = new LopHocPhan { MaLHP = "LHP01", MaMon = "CS101", MaHocKy = "HK261", MaGV = "GV01" };
            var sv = new SinhVien { MaSV = "21001", HoTen = "Nguyễn Văn Active", LopSinhHoat = "CTK45", KhoaHoc = "K21" };

            context.HocKys.Add(hk);
            context.MonHocs.Add(mon);
            context.NguoiDungs.Add(gv);
            context.LopHocPhans.Add(lhp);
            context.SinhViens.Add(sv);
            context.DangKyHocPhans.Add(new DangKyHocPhan { MaSV = "21001", MaLHP = "LHP01", TrangThai = "DangHoc" });
            await context.SaveChangesAsync();
        }

        var repo = new BaoCaoRepository(_factory);
        var phieu = await repo.LayPhieuDangKyAsync("21001", "HK261");

        Assert.NotNull(phieu);
        Assert.Single(phieu.DanhSachMon);
        Assert.Equal("ThS. Liam / Lâm", phieu.DanhSachMon[0].TenGiangVien);
        Assert.Equal(3, phieu.TongSoTinChi);
    }
}
