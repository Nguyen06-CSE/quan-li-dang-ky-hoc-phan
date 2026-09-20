using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class HocPhiServiceTests
{
    private readonly Mock<IDangKyHocPhanRepository> _mockDangKyRepo;
    private readonly Mock<ICauHinhService> _mockCauHinhService;
    private readonly Mock<ISinhVienRepository> _mockSinhVienRepo;
    private readonly Mock<IHocKyRepository> _mockHocKyRepo;
    private readonly HocPhiService _service;

    public HocPhiServiceTests()
    {
        _mockDangKyRepo = new Mock<IDangKyHocPhanRepository>();
        _mockCauHinhService = new Mock<ICauHinhService>();
        _mockSinhVienRepo = new Mock<ISinhVienRepository>();
        _mockHocKyRepo = new Mock<IHocKyRepository>();

        // Default prices: LT = 500,000 VND, TH = 700,000 VND
        _mockCauHinhService.Setup(c => c.GetDecimal("DonGiaTinChiLT")).ReturnsAsync(500000m);
        _mockCauHinhService.Setup(c => c.GetDecimal("DonGiaTinChiTH")).ReturnsAsync(700000m);

        _service = new HocPhiService(
            _mockDangKyRepo.Object,
            _mockCauHinhService.Object,
            _mockSinhVienRepo.Object,
            _mockHocKyRepo.Object);
    }

    private DangKyHocPhan CreateDangKy(string maLHP, string maMon, string tenMon, int tcLT, int tcTH, decimal soTienDaDong = 0, string trangThai = "DangHoc")
    {
        return new DangKyHocPhan
        {
            Id = Guid.NewGuid(),
            MaSV = "SV001",
            MaLHP = maLHP,
            TrangThai = trangThai,
            SoTienDaDong = soTienDaDong,
            LopHocPhan = new LopHocPhan
            {
                MaLHP = maLHP,
                MaMon = maMon,
                MonHoc = new MonHoc
                {
                    MaMon = maMon,
                    TenMon = tenMon,
                    SoTinChiLT = tcLT,
                    SoTinChiTH = tcTH
                }
            }
        };
    }

    [Fact]
    public async Task TinhHocPhiSinhVienAsync_SinhVienHocNhieuMon_MonCoCaLTVaTH_TinhDungCongThuc()
    {
        // Arrange
        string maSV = "SV001";
        string maHK = "HK1_2026_2027";

        _mockSinhVienRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(new SinhVien
        {
            MaSV = maSV,
            HoTen = "Nguyễn Văn A",
            LopSinhHoat = "CNTT1"
        });

        _mockHocKyRepo.Setup(r => r.GetByIdAsync(maHK)).ReturnsAsync(new HocKy
        {
            MaHocKy = maHK,
            TenHocKy = "Học kỳ 1 - 2026-2027"
        });

        // Môn 1: 3 TC LT, 0 TC TH => 3 * 500k = 1,500,000
        // Môn 2: 2 TC LT, 1 TC TH => 2 * 500k + 1 * 700k = 1,700,000
        // Môn 3: 0 TC LT, 2 TC TH => 0 + 2 * 700k = 1,400,000
        // Môn 4: Đã hủy (TrangThai = "DaHuy"), 3 TC LT => không tính vào học phí
        var dsDk = new List<DangKyHocPhan>
        {
            CreateDangKy("LHP01", "CSDL", "Cơ sở dữ liệu", 3, 0, soTienDaDong: 1000000),
            CreateDangKy("LHP02", "OOP", "Lập trình hướng đối tượng", 2, 1, soTienDaDong: 500000),
            CreateDangKy("LHP03", "TH_HDH", "Thực hành hệ điều hành", 0, 2, soTienDaDong: 0),
            CreateDangKy("LHP04", "TRR", "Toán rời rạc", 3, 0, soTienDaDong: 0, trangThai: "DaHuy")
        };

        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync(maSV, maHK)).ReturnsAsync(dsDk);

        // Act
        var result = await _service.TinhHocPhiSinhVienAsync(maSV, maHK);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(maSV, result.MaSV);
        Assert.Equal("Nguyễn Văn A", result.HoTen);
        Assert.Equal("CNTT1", result.LopSinhHoat);
        Assert.Equal(3, result.DanhSachMon.Count); // Chỉ 3 môn "DangHoc"

        // Kiểm tra chi tiết môn 1
        var mon1 = result.DanhSachMon.FirstOrDefault(m => m.MaLHP == "LHP01");
        Assert.NotNull(mon1);
        Assert.Equal(3, mon1.TongTinChi);
        Assert.Equal(1500000m, mon1.ThanhTien);

        // Kiểm tra chi tiết môn 2 (có cả LT và TH)
        var mon2 = result.DanhSachMon.FirstOrDefault(m => m.MaLHP == "LHP02");
        Assert.NotNull(mon2);
        Assert.Equal(3, mon2.TongTinChi);
        Assert.Equal(1700000m, mon2.ThanhTien);

        // Kiểm tra chi tiết môn 3
        var mon3 = result.DanhSachMon.FirstOrDefault(m => m.MaLHP == "LHP03");
        Assert.NotNull(mon3);
        Assert.Equal(2, mon3.TongTinChi);
        Assert.Equal(1400000m, mon3.ThanhTien);

        // Tổng tín chỉ = 3 + 3 + 2 = 8
        Assert.Equal(8, result.TongSoTinChi);
        // Tổng học phí = 1.5tr + 1.7tr + 1.4tr = 4,600,000 đ
        Assert.Equal(4600000m, result.TongHocPhi);
        // Đã đóng = 1tr + 500k + 0 = 1,500,000 đ
        Assert.Equal(1500000m, result.DaDong);
        // Còn nợ = 4.6tr - 1.5tr = 3,100,000 đ
        Assert.Equal(3100000m, result.ConNo);
        Assert.False(result.DaDongDu);
    }

    [Fact]
    public async Task TinhHocPhiSinhVienAsync_DaDongDu_TrangThaiDaDongDuLaTrue()
    {
        // Arrange
        string maSV = "SV002";
        string maHK = "HK1_2026_2027";

        _mockSinhVienRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(new SinhVien
        {
            MaSV = maSV,
            HoTen = "Trần Thị B",
            LopSinhHoat = "CNTT2"
        });
        _mockHocKyRepo.Setup(r => r.GetByIdAsync(maHK)).ReturnsAsync(new HocKy { MaHocKy = maHK, TenHocKy = "HK1" });

        // Môn 1: 2 TC LT => 1,000,000 đ, đã đóng 1,000,000 đ
        var dsDk = new List<DangKyHocPhan>
        {
            CreateDangKy("LHP01", "M1", "Môn 1", 2, 0, soTienDaDong: 1000000)
        };
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync(maSV, maHK)).ReturnsAsync(dsDk);

        // Act
        var result = await _service.TinhHocPhiSinhVienAsync(maSV, maHK);

        // Assert
        Assert.Equal(1000000m, result.TongHocPhi);
        Assert.Equal(1000000m, result.DaDong);
        Assert.Equal(0m, result.ConNo);
        Assert.True(result.DaDongDu);
    }

    [Fact]
    public async Task TinhLaiHocPhiAsync_CapNhatSoTienPhaiDongVaoDangKyHocPhan()
    {
        // Arrange
        string maSV = "SV001";
        string maHK = "HK1_2026_2027";

        var dk1 = CreateDangKy("LHP01", "CSDL", "Cơ sở dữ liệu", 3, 0, trangThai: "DangHoc");
        var dk2 = CreateDangKy("LHP02", "OOP", "Lập trình hướng đối tượng", 2, 1, trangThai: "DangHoc");
        var dk3 = CreateDangKy("LHP03", "HUY", "Môn hủy", 3, 0, trangThai: "DaHuy");

        var dsDk = new List<DangKyHocPhan> { dk1, dk2, dk3 };
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync(maSV, maHK)).ReturnsAsync(dsDk);

        // Act
        await _service.TinhLaiHocPhiAsync(maSV, maHK);

        // Assert
        // DK1: 3 * 500,000 = 1,500,000
        Assert.Equal(1500000m, dk1.SoTienPhaiDong);
        // DK2: 2 * 500,000 + 1 * 700,000 = 1,700,000
        Assert.Equal(1700000m, dk2.SoTienPhaiDong);
        // DK3: DaHuy => 0
        Assert.Equal(0m, dk3.SoTienPhaiDong);

        _mockDangKyRepo.Verify(r => r.CapNhatAsync(dk1), Times.Once);
        _mockDangKyRepo.Verify(r => r.CapNhatAsync(dk2), Times.Once);
        _mockDangKyRepo.Verify(r => r.CapNhatAsync(dk3), Times.Once);
    }

    [Fact]
    public async Task TinhHocPhiTheoDanhSachAsync_TraVeDanhSachTongHopChinhXac()
    {
        // Arrange
        string maHK = "HK1_2026_2027";
        var dsMaSV = new List<string> { "SV001", "SV002" };

        _mockSinhVienRepo.Setup(r => r.GetByIdAsync("SV001")).ReturnsAsync(new SinhVien { MaSV = "SV001", HoTen = "SV 1" });
        _mockSinhVienRepo.Setup(r => r.GetByIdAsync("SV002")).ReturnsAsync(new SinhVien { MaSV = "SV002", HoTen = "SV 2" });
        _mockHocKyRepo.Setup(r => r.GetByIdAsync(maHK)).ReturnsAsync(new HocKy { MaHocKy = maHK });

        var dsDk1 = new List<DangKyHocPhan> { CreateDangKy("LHP01", "M1", "Môn 1", 3, 0, soTienDaDong: 1500000) };
        var dsDk2 = new List<DangKyHocPhan> { CreateDangKy("LHP02", "M2", "Môn 2", 2, 1, soTienDaDong: 500000) };

        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync("SV001", maHK)).ReturnsAsync(dsDk1);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync("SV002", maHK)).ReturnsAsync(dsDk2);

        // Act
        var result = await _service.TinhHocPhiTheoDanhSachAsync(dsMaSV, maHK);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("SV001", result[0].MaSV);
        Assert.Equal(1500000m, result[0].TongHocPhi);
        Assert.Equal(1500000m, result[0].DaDong);
        Assert.True(result[0].DaDongDu);
        Assert.Equal("Đã đóng đủ", result[0].TrangThaiText);

        Assert.Equal("SV002", result[1].MaSV);
        Assert.Equal(1700000m, result[1].TongHocPhi);
        Assert.Equal(500000m, result[1].DaDong);
        Assert.False(result[1].DaDongDu);
        Assert.Equal("Còn nợ", result[1].TrangThaiText);
    }
}
