using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class BaoCaoServiceTests
{
    private readonly Mock<IDashboardRepository> _mockDashboardRepo;
    private readonly Mock<IBaoCaoRepository> _mockBaoCaoRepo;
    private readonly BaoCaoService _baoCaoService;

    public BaoCaoServiceTests()
    {
        _mockDashboardRepo = new Mock<IDashboardRepository>();
        _mockBaoCaoRepo = new Mock<IBaoCaoRepository>();
        _baoCaoService = new BaoCaoService(_mockDashboardRepo.Object, _mockBaoCaoRepo.Object);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsCorrectStats()
    {
        var expectedStats = new DashboardStatsDto
        {
            TongSinhVien = 500,
            TongMonHoc = 30,
            TongLopHocPhan = 100,
            TongLuotDangKy = 4500
        };

        _mockDashboardRepo.Setup(r => r.GetStatsAsync("HK2026_1"))
                 .ReturnsAsync(expectedStats);

        var result = await _baoCaoService.GetDashboardStatsAsync("HK2026_1");

        Assert.NotNull(result);
        Assert.Equal(500, result.TongSinhVien);
        Assert.Equal(30, result.TongMonHoc);
        Assert.Equal(100, result.TongLopHocPhan);
        Assert.Equal(4500, result.TongLuotDangKy);
    }

    [Fact]
    public async Task GetLhpGiangVienAsync_EmptyMaGV_ReturnsEmptyList()
    {
        _mockDashboardRepo.Setup(r => r.GetLhpByGiangVienAsync("", "HK2026_1"))
                 .ReturnsAsync(new List<GiangVienLhpDto>());

        var result = await _baoCaoService.GetLhpGiangVienAsync("", "HK2026_1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task DsSinhVienTheoMonAsync_ValidArgs_ReturnsList()
    {
        var mockList = new List<SinhVienTheoMonDto>
        {
            new() { MaSV = "SV001", HoTen = "Nguyen Van A", MaLHP = "LHP01", TrangThai = "DangHoc" }
        };

        _mockBaoCaoRepo.Setup(r => r.LayDsSvTheoMonAsync("CS101", "HK20251"))
            .ReturnsAsync(mockList);

        var result = await _baoCaoService.DsSinhVienTheoMonAsync("CS101", "HK20251");

        Assert.Single(result);
        Assert.Equal("SV001", result[0].MaSV);
    }

    [Fact]
    public async Task DsThiTheoMonAsync_ValidArgs_ReturnsList()
    {
        var mockList = new List<DanhSachThiDto>
        {
            new() { Stt = 1, MaSV = "SV001", HoTen = "Nguyen Van A", MaLHP = "LHP01" }
        };

        _mockBaoCaoRepo.Setup(r => r.LayDsThiAsync("CS101", "HK20251", "LHP01"))
            .ReturnsAsync(mockList);

        var result = await _baoCaoService.DsThiTheoMonAsync("CS101", "HK20251", "LHP01");

        Assert.Single(result);
        Assert.Equal("SV001", result[0].MaSV);
    }

    [Fact]
    public async Task ThongKeSoLuongTheoMonAsync_ReturnsSortedList()
    {
        var mockList = new List<ThongKeMonDto>
        {
            new() { MaMon = "CS101", TenMon = "Lập trình C", SoSinhVien = 80, SoLhpDangMo = 2 },
            new() { MaMon = "CS102", TenMon = "Cấu trúc dữ liệu", SoSinhVien = 50, SoLhpDangMo = 1 }
        };

        _mockBaoCaoRepo.Setup(r => r.ThongKeSoSvTheoMonAsync("HK20251"))
            .ReturnsAsync(mockList);

        var result = await _baoCaoService.ThongKeSoLuongTheoMonAsync("HK20251");

        Assert.Equal(2, result.Count);
        Assert.Equal(80, result[0].SoSinhVien);
    }

    [Fact]
    public async Task LayPhieuDangKyAsync_ReturnsPhieu()
    {
        var mockPhieu = new PhieuDangKyDto
        {
            MaSV = "SV001",
            HoTen = "Nguyen Van A",
            MaHocKy = "HK20251",
            TongSoTinChi = 6,
            DanhSachMon = new List<PhieuDangKyMonDto>
            {
                new() { Stt = 1, MaMon = "CS101", TenMon = "Lập trình C", SoTinChi = 3, MaLHP = "LHP01" },
                new() { Stt = 2, MaMon = "CS102", TenMon = "CTDL", SoTinChi = 3, MaLHP = "LHP02" }
            }
        };

        _mockBaoCaoRepo.Setup(r => r.LayPhieuDangKyAsync("SV001", "HK20251"))
            .ReturnsAsync(mockPhieu);

        var result = await _baoCaoService.LayPhieuDangKyAsync("SV001", "HK20251");

        Assert.NotNull(result);
        Assert.Equal(2, result.DanhSachMon.Count);
        Assert.Equal(6, result.TongSoTinChi);
    }
}
