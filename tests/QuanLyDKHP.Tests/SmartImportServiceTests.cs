using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;
using QuanLyDKHP.Infrastructure.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class SmartImportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly TestDbContextFactory _factory;
    private readonly Mock<IHocPhiService> _mockHocPhiService;
    private readonly SmartImportService _service;

    public class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new AppDbContext(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(_options));
    }

    public SmartImportServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();

        _factory = new TestDbContextFactory(_options);
        _mockHocPhiService = new Mock<IHocPhiService>();
        _service = new SmartImportService(_factory, _mockHocPhiService.Object);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void ParseClipboardOrText_EmptyString_ReturnsEmptyList()
    {
        var result = _service.ParseClipboardOrText("");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseClipboardOrText_TabSeparated_AutoDetectsHeadersAndParsesStudents()
    {
        string rawText = "MSSV\tHọ và tên\tLớp\tKhóa\r\n" +
                         "2100011\tNguyễn Văn An\tCTK45A\tK45\r\n" +
                         "2100012\tTrần Thị Bình\tCTK45B\tK45";

        var list = _service.ParseClipboardOrText(rawText);

        Assert.Equal(2, list.Count);
        Assert.Equal("2100011", list[0].MaSV);
        Assert.Equal("Nguyễn Văn An", list[0].HoTen);
        Assert.Equal("CTK45A", list[0].LopSinhHoat);
        Assert.Equal("K45", list[0].KhoaHoc);
        Assert.True(list[0].IsHopLe);

        Assert.Equal("2100012", list[1].MaSV);
        Assert.Equal("Trần Thị Bình", list[1].HoTen);
    }

    [Fact]
    public void ParseClipboardOrText_MergedHoDemAndTen_CombinesProperly()
    {
        string rawText = "STT\tMSSV\tHọ đệm\tTên\tLớp sinh hoạt\r\n" +
                         "1\t2111813\tHoàng Hải\tĐăng\tCTK45A\r\n" +
                         "2\t2111844\tĐoàn Trương\tPhúc\tCTK45A";

        var list = _service.ParseClipboardOrText(rawText);

        Assert.Equal(2, list.Count);
        Assert.Equal("2111813", list[0].MaSV);
        Assert.Equal("Hoàng Hải Đăng", list[0].HoTen);
        Assert.Equal("CTK45A", list[0].LopSinhHoat);
        Assert.Equal("K21", list[0].KhoaHoc); // Suy luận từ 21...

        Assert.Equal("2111844", list[1].MaSV);
        Assert.Equal("Đoàn Trương Phúc", list[1].HoTen);
    }

    [Fact]
    public void ParseClipboardOrText_RawDataNoHeader_InfersMssvAndName()
    {
        string rawText = "2111901\tTrần Nhật Long\r\n" +
                         "2111906\tPhan Ngọc Mai";

        var list = _service.ParseClipboardOrText(rawText);

        Assert.Equal(2, list.Count);
        Assert.Equal("2111901", list[0].MaSV);
        Assert.Equal("Trần Nhật Long", list[0].HoTen);
        Assert.Equal("K21", list[0].KhoaHoc);
    }

    [Fact]
    public async Task AnalyzeAndValidateAsync_CalculatesCapacityAndDetectsExistingStudents()
    {
        // 1. Seed database
        using (var ctx = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026", DangMo = true };
            var mon = new MonHoc { MaMon = "CS101", TenMon = "Lập trình Web", SoTinChiLT = 3, SoTinChiTH = 0 };
            var lhp = new LopHocPhan
            {
                MaLHP = "261CS10101",
                MaMon = "CS101",
                MaHocKy = "HK261",
                SiSoToiDa = 40,
                Thu = 2,
                TietBatDau = 1,
                SoTiet = 3
            };
            var sv1 = new SinhVien { MaSV = "2100001", HoTen = "Nguyễn Văn Đã Có", LopSinhHoat = "CTK45A", KhoaHoc = "K45" };
            var dk1 = new DangKyHocPhan { MaSV = "2100001", MaLHP = "261CS10101", TrangThai = "DangHoc", HinhThucDK = "KH" };

            ctx.HocKys.Add(hk);
            ctx.MonHocs.Add(mon);
            ctx.LopHocPhans.Add(lhp);
            ctx.SinhViens.Add(sv1);
            ctx.DangKyHocPhans.Add(dk1);
            await ctx.SaveChangesAsync();
        }

        // 2. Phân tích danh sách gồm 2 SV: 1 SV đã có trong lớp, 1 SV mới
        var items = new List<SinhVienImportPreviewDto>
        {
            new SinhVienImportPreviewDto { MaSV = "2100001", HoTen = "Nguyễn Văn Đã Có" },
            new SinhVienImportPreviewDto { MaSV = "2100002", HoTen = "Lê Thị Mới", LopSinhHoat = "CTK45B", KhoaHoc = "K45" }
        };

        var insights = await _service.AnalyzeAndValidateAsync("HK261", "261CS10101", items);

        Assert.Equal(1, insights.SiSoHienTai);
        Assert.Equal(40, insights.SiSoToiDa);
        Assert.Equal(1, insights.SoLuongDaCo);
        Assert.Equal(1, insights.SoLuongHopLe);
        Assert.False(insights.VuotSiSo);

        Assert.Equal("DaCoTrongLop", items[0].TrangThaiXacThuc);
        Assert.False(items[0].IsSelected);

        Assert.Equal("HopLe", items[1].TrangThaiXacThuc);
        Assert.True(items[1].IsSelected);
    }

    [Fact]
    public async Task AnalyzeAndValidateAsync_DetectsScheduleConflict_AndDuplicateCourse()
    {
        // 1. Seed database
        using (var ctx = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026", DangMo = true };
            var mon1 = new MonHoc { MaMon = "CS101", TenMon = "Lập trình Web" };
            var mon2 = new MonHoc { MaMon = "CS102", TenMon = "Cơ sở dữ liệu" };

            // Lớp học phần đích: Thứ 3, tiết 1-3
            var lhpTarget = new LopHocPhan
            {
                MaLHP = "261CS10101",
                MaMon = "CS101",
                MaHocKy = "HK261",
                SiSoToiDa = 50,
                Thu = 3,
                TietBatDau = 1,
                SoTiet = 3
            };

            // Lớp học phần đang học bị trùng lịch: Thứ 3, tiết 2-4 (giao nhau tiết 2,3)
            var lhpOther = new LopHocPhan
            {
                MaLHP = "261CS10201",
                MaMon = "CS102",
                MaHocKy = "HK261",
                SiSoToiDa = 50,
                Thu = 3,
                TietBatDau = 2,
                SoTiet = 3
            };

            var sv = new SinhVien { MaSV = "2100099", HoTen = "Trần Trùng Lịch", LopSinhHoat = "CTK45A" };
            var dk = new DangKyHocPhan { MaSV = "2100099", MaLHP = "261CS10201", TrangThai = "DangHoc", HinhThucDK = "KH" };

            ctx.HocKys.Add(hk);
            ctx.MonHocs.AddRange(mon1, mon2);
            ctx.LopHocPhans.AddRange(lhpTarget, lhpOther);
            ctx.SinhViens.Add(sv);
            ctx.DangKyHocPhans.Add(dk);
            await ctx.SaveChangesAsync();
        }

        var items = new List<SinhVienImportPreviewDto>
        {
            new SinhVienImportPreviewDto { MaSV = "2100099", HoTen = "Trần Trùng Lịch" }
        };

        var insights = await _service.AnalyzeAndValidateAsync("HK261", "261CS10101", items);

        Assert.True(items[0].HasXungDotLich);
        Assert.True(insights.CoXungDotLich);
        Assert.Contains("Trùng lịch", items[0].CanhBaoXungDot);
    }

    [Fact]
    public async Task EnrollStudentsAsync_CreatesNewStudentProfiles_AndEnrollsAtomic()
    {
        // 1. Seed database
        using (var ctx = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026", DangMo = true };
            var mon = new MonHoc { MaMon = "CS101", TenMon = "Lập trình Web" };
            var lhp = new LopHocPhan
            {
                MaLHP = "261CS10101",
                MaMon = "CS101",
                MaHocKy = "HK261",
                SiSoToiDa = 50
            };

            ctx.HocKys.Add(hk);
            ctx.MonHocs.Add(mon);
            ctx.LopHocPhans.Add(lhp);
            await ctx.SaveChangesAsync();
        }

        var items = new List<SinhVienImportPreviewDto>
        {
            new SinhVienImportPreviewDto
            {
                MaSV = "2212338",
                HoTen = "Lê Bình Dương",
                LopSinhHoat = "CTK46A",
                KhoaHoc = "K46",
                TrangThaiXacThuc = "HopLe",
                IsSelected = true
            },
            new SinhVienImportPreviewDto
            {
                MaSV = "2212339",
                HoTen = "Lê Song Toàn",
                LopSinhHoat = "CTK46B",
                KhoaHoc = "K46",
                TrangThaiXacThuc = "HopLe",
                IsSelected = true
            }
        };

        var result = await _service.EnrollStudentsAsync("HK261", "261CS10101", items, tuDongTaoSv: true);

        Assert.Equal(2, result.ThanhCong);
        Assert.Equal(2, result.SoSinhVienMoiTao);
        Assert.Equal(0, result.Loi);

        // Kiểm tra trong database
        using (var ctx = new AppDbContext(_options))
        {
            var sv1 = await ctx.SinhViens.FindAsync("2212338");
            Assert.NotNull(sv1);
            Assert.Equal("Lê Bình Dương", sv1.HoTen);

            var countDk = await ctx.DangKyHocPhans.CountAsync(dk => dk.MaLHP == "261CS10101" && dk.TrangThai == "DangHoc");
            Assert.Equal(2, countDk);
        }

        // Kiểm tra đã gọi tính lại học phí cho 2 sinh viên
        _mockHocPhiService.Verify(h => h.TinhLaiHocPhiAsync("2212338", "HK261"), Times.Once);
        _mockHocPhiService.Verify(h => h.TinhLaiHocPhiAsync("2212339", "HK261"), Times.Once);
    }

    [Fact]
    public async Task EnrollStudentsAsync_ClosedSemester_RejectsEnrollment()
    {
        using (var ctx = new AppDbContext(_options))
        {
            var hk = new HocKy { MaHocKy = "HK261", TenHocKy = "Học kỳ 1 2026", DangMo = false }; // Đã đóng
            var mon = new MonHoc { MaMon = "CS101", TenMon = "Lập trình Web" };
            var lhp = new LopHocPhan { MaLHP = "261CS10101", MaMon = "CS101", MaHocKy = "HK261" };

            ctx.HocKys.Add(hk);
            ctx.MonHocs.Add(mon);
            ctx.LopHocPhans.Add(lhp);
            await ctx.SaveChangesAsync();
        }

        var items = new List<SinhVienImportPreviewDto>
        {
            new SinhVienImportPreviewDto { MaSV = "2100001", HoTen = "Trần A", TrangThaiXacThuc = "HopLe", IsSelected = true }
        };

        var result = await _service.EnrollStudentsAsync("HK261", "261CS10101", items);

        Assert.Equal(0, result.ThanhCong);
        Assert.Equal(1, result.Loi);
        Assert.Contains("đã đóng cổng", result.ThongBao);
    }
}
