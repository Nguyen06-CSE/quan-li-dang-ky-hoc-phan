using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Services;
using Xunit;

namespace QuanLyDKHP.Tests;

public class DangKyHocPhanServiceTests
{
    private readonly Mock<IDangKyHocPhanRepository> _mockDangKyRepo;
    private readonly Mock<ILopHocPhanRepository> _mockLhpRepo;
    private readonly Mock<ISinhVienRepository> _mockSvRepo;
    private readonly Mock<ICauHinhService> _mockCauHinhService;
    private readonly Mock<IHocPhiService> _mockHocPhiService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly DangKyHocPhanService _service;

    public DangKyHocPhanServiceTests()
    {
        _mockDangKyRepo = new Mock<IDangKyHocPhanRepository>();
        _mockLhpRepo = new Mock<ILopHocPhanRepository>();
        _mockSvRepo = new Mock<ISinhVienRepository>();
        _mockCauHinhService = new Mock<ICauHinhService>();
        _mockHocPhiService = new Mock<IHocPhiService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        // Default setup for CurrentUserService
        var mockUser = new CurrentUserInfo
        {
            Id = 1,
            TenDangNhap = "test_user",
            HoTen = "Test User",
            Role = UserRole.Admin
        };
        _mockCurrentUserService.Setup(u => u.CurrentUser).Returns(mockUser);

        // Default setup for CauHinhService
        _mockCauHinhService.Setup(c => c.GetInt("SoTinChiToiDa")).ReturnsAsync(25);
        _mockCauHinhService.Setup(c => c.GetInt("SoTinChiToiThieu")).ReturnsAsync(10);
        _mockCauHinhService.Setup(c => c.GetDecimal("DonGiaTinChiLT")).ReturnsAsync(500000m);
        _mockCauHinhService.Setup(c => c.GetDecimal("DonGiaTinChiTH")).ReturnsAsync(700000m);

        _service = new DangKyHocPhanService(
            _mockDangKyRepo.Object,
            _mockLhpRepo.Object,
            _mockSvRepo.Object,
            _mockCauHinhService.Object,
            _mockHocPhiService.Object,
            _mockCurrentUserService.Object);
    }

    private LopHocPhan CreateSampleLhp(string maLHP, string maMon, string tenMon, int tcLT, int tcTH, int? siSoToiDa = 40, string maHocKy = "HK1_2026_2027")
    {
        var mon = new MonHoc
        {
            MaMon = maMon,
            TenMon = tenMon,
            SoTinChiLT = tcLT,
            SoTinChiTH = tcTH
        };

        return new LopHocPhan
        {
            MaLHP = maLHP,
            MaMon = maMon,
            MaHocKy = maHocKy,
            MonHoc = mon,
            SiSoToiDa = siSoToiDa
        };
    }

    private SinhVien CreateSampleSv(string maSV = "SV001", string hoTen = "Nguyễn Văn A")
    {
        return new SinhVien
        {
            MaSV = maSV,
            HoTen = hoTen,
            LopSinhHoat = "CNTT1"
        };
    }

    [Fact]
    public async Task DangKyAsync_TrungLopHocPhan_DangHoc_TraVeLoiChanCung()
    {
        // Arrange: SV đã có đăng ký đúng lớp học phần này và đang học
        string maSV = "SV001";
        string maLHP = "LHP_CSDL_01";
        var lhp = CreateSampleLhp(maLHP, "CSDL", "Cơ sở dữ liệu", 3, 0);
        var sv = CreateSampleSv(maSV);

        _mockSvRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(sv);
        _mockLhpRepo.Setup(r => r.GetByIdAsync(maLHP)).ReturnsAsync(lhp);

        var existingDk = new DangKyHocPhan
        {
            MaSV = maSV,
            MaLHP = maLHP,
            TrangThai = "DangHoc",
            LopHocPhan = lhp
        };
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLHP)).ReturnsAsync(existingDk);

        // Act
        var result = await _service.DangKyAsync(maSV, maLHP);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sinh viên đã đăng ký lớp học phần này", result.Message);
        _mockDangKyRepo.Verify(r => r.ThemAsync(It.IsAny<DangKyHocPhan>()), Times.Never);
    }

    [Fact]
    public async Task DangKyAsync_TrungMonHocKhacLop_ChuaXacNhan_TraVeCanhBaoMem()
    {
        // Arrange: SV đã đăng ký môn CSDL ở LHP_CSDL_01, nay đăng ký LHP_CSDL_02 cùng môn
        string maSV = "SV001";
        string maLhpMoi = "LHP_CSDL_02";
        string maLhpCu = "LHP_CSDL_01";
        var lhpMoi = CreateSampleLhp(maLhpMoi, "CSDL", "Cơ sở dữ liệu", 3, 0);
        var sv = CreateSampleSv(maSV);

        _mockSvRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(sv);
        _mockLhpRepo.Setup(r => r.GetByIdAsync(maLhpMoi)).ReturnsAsync(lhpMoi);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLhpMoi)).ReturnsAsync((DangKyHocPhan?)null);

        var dkCungMon = new DangKyHocPhan
        {
            MaSV = maSV,
            MaLHP = maLhpCu,
            TrangThai = "DangHoc",
            LopHocPhan = CreateSampleLhp(maLhpCu, "CSDL", "Cơ sở dữ liệu", 3, 0)
        };
        _mockDangKyRepo.Setup(r => r.LayDangKyCungMonDangHocAsync(maSV, "CSDL", lhpMoi.MaHocKy)).ReturnsAsync(dkCungMon);

        // Act (boQuaCanhBaoTrungMon = false)
        var result = await _service.DangKyAsync(maSV, maLhpMoi, boQuaCanhBaoTrungMon: false);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsCanhBaoTrungMon);
        Assert.Equal(maLhpCu, result.MaLHPCu);
        Assert.Contains("học lại/học cải thiện", result.Message);
        _mockDangKyRepo.Verify(r => r.ThemAsync(It.IsAny<DangKyHocPhan>()), Times.Never);
    }

    [Fact]
    public async Task DangKyAsync_TrungMonHocKhacLop_DaXacNhan_ChoPhepDangKy()
    {
        // Arrange: SV đã xác nhận học lại (boQuaCanhBaoTrungMon = true)
        string maSV = "SV001";
        string maLhpMoi = "LHP_CSDL_02";
        var lhpMoi = CreateSampleLhp(maLhpMoi, "CSDL", "Cơ sở dữ liệu", 3, 0);
        var sv = CreateSampleSv(maSV);

        _mockSvRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(sv);
        _mockLhpRepo.Setup(r => r.GetByIdAsync(maLhpMoi)).ReturnsAsync(lhpMoi);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLhpMoi)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.DemSiSoDangKyAsync(maLhpMoi)).ReturnsAsync(10);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync(maSV, lhpMoi.MaHocKy)).ReturnsAsync(new List<DangKyHocPhan>());

        // Act
        var result = await _service.DangKyAsync(maSV, maLhpMoi, boQuaCanhBaoTrungMon: true);

        // Assert
        Assert.True(result.Success);
        _mockDangKyRepo.Verify(r => r.ThemAsync(It.Is<DangKyHocPhan>(d => d.MaLHP == maLhpMoi && d.MaSV == maSV && d.TrangThai == "DangHoc")), Times.Once);
        _mockHocPhiService.Verify(h => h.TinhLaiHocPhiAsync(maSV, lhpMoi.MaHocKy), Times.Once);
    }

    [Fact]
    public async Task DangKyAsync_LopHocPhanDaySiSo_TraVeLoiChanCung()
    {
        // Arrange: Lớp có sĩ số tối đa 40, hiện đã có 40 SV đăng ký
        string maSV = "SV001";
        string maLHP = "LHP_LTC_01";
        var lhp = CreateSampleLhp(maLHP, "LTC", "Lập trình C", 3, 0, siSoToiDa: 40);
        var sv = CreateSampleSv(maSV);

        _mockSvRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(sv);
        _mockLhpRepo.Setup(r => r.GetByIdAsync(maLHP)).ReturnsAsync(lhp);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLHP)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.LayDangKyCungMonDangHocAsync(maSV, "LTC", lhp.MaHocKy)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.DemSiSoDangKyAsync(maLHP)).ReturnsAsync(40); // Đã đầy

        // Act
        var result = await _service.DangKyAsync(maSV, maLHP);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Lớp học phần đã đủ sĩ số", result.Message);
        _mockDangKyRepo.Verify(r => r.ThemAsync(It.IsAny<DangKyHocPhan>()), Times.Never);
    }

    [Fact]
    public async Task DangKyAsync_VuotQuaSoTinChiToiDa_TraVeLoiChanCung()
    {
        // Arrange: Cấu hình tối đa 25 TC. SV hiện đã học 23 TC, môn mới 3 TC (23+3=26 > 25)
        string maSV = "SV001";
        string maLHP = "LHP_CTDL_01";
        var lhp = CreateSampleLhp(maLHP, "CTDL", "Cấu trúc dữ liệu", 3, 0, siSoToiDa: 50);
        var sv = CreateSampleSv(maSV);

        _mockSvRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(sv);
        _mockLhpRepo.Setup(r => r.GetByIdAsync(maLHP)).ReturnsAsync(lhp);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLHP)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.LayDangKyCungMonDangHocAsync(maSV, "CTDL", lhp.MaHocKy)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.DemSiSoDangKyAsync(maLHP)).ReturnsAsync(10);

        // SV đã có 23 TC đang học
        var existingRegistrations = new List<DangKyHocPhan>
        {
            new() { TrangThai = "DangHoc", LopHocPhan = CreateSampleLhp("L1", "M1", "Môn 1", 10, 0) },
            new() { TrangThai = "DangHoc", LopHocPhan = CreateSampleLhp("L2", "M2", "Môn 2", 10, 3) }
        };
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync(maSV, lhp.MaHocKy)).ReturnsAsync(existingRegistrations);

        // Act
        var result = await _service.DangKyAsync(maSV, maLHP);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Vượt quá số tín chỉ tối đa cho phép", result.Message);
        Assert.Contains("25 tín chỉ", result.Message);
        _mockDangKyRepo.Verify(r => r.ThemAsync(It.IsAny<DangKyHocPhan>()), Times.Never);
    }

    [Fact]
    public async Task DangKyAsync_HopLe_TaoBanGhiVaTinhLaiHocPhi()
    {
        // Arrange
        string maSV = "SV001";
        string maLHP = "LHP_OOP_01";
        var lhp = CreateSampleLhp(maLHP, "OOP", "Lập trình hướng đối tượng", 2, 1, siSoToiDa: 45);
        var sv = CreateSampleSv(maSV);

        _mockSvRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(sv);
        _mockLhpRepo.Setup(r => r.GetByIdAsync(maLHP)).ReturnsAsync(lhp);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLHP)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.LayDangKyCungMonDangHocAsync(maSV, "OOP", lhp.MaHocKy)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.DemSiSoDangKyAsync(maLHP)).ReturnsAsync(15);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync(maSV, lhp.MaHocKy)).ReturnsAsync(new List<DangKyHocPhan>());

        // Act
        var result = await _service.DangKyAsync(maSV, maLHP);

        // Assert
        Assert.True(result.Success);
        _mockDangKyRepo.Verify(r => r.ThemAsync(It.Is<DangKyHocPhan>(d =>
            d.MaSV == maSV &&
            d.MaLHP == maLHP &&
            d.TrangThai == "DangHoc" &&
            d.HinhThucDK == "KH" &&
            d.NguoiDK == "test_user")), Times.Once);
        _mockHocPhiService.Verify(h => h.TinhLaiHocPhiAsync(maSV, lhp.MaHocKy), Times.Once);
    }

    [Fact]
    public async Task DangKyAsync_DaTungHuy_TaiKichHoatBanGhiCu()
    {
        // Arrange: SV đã từng đăng ký LHP này nhưng đã hủy
        string maSV = "SV001";
        string maLHP = "LHP_OOP_01";
        var lhp = CreateSampleLhp(maLHP, "OOP", "Lập trình hướng đối tượng", 2, 1, siSoToiDa: 45);
        var sv = CreateSampleSv(maSV);

        _mockSvRepo.Setup(r => r.GetByIdAsync(maSV)).ReturnsAsync(sv);
        _mockLhpRepo.Setup(r => r.GetByIdAsync(maLHP)).ReturnsAsync(lhp);

        var existingDk = new DangKyHocPhan
        {
            Id = Guid.NewGuid(),
            MaSV = maSV,
            MaLHP = maLHP,
            TrangThai = "DaHuy",
            LopHocPhan = lhp
        };
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLHP)).ReturnsAsync(existingDk);
        _mockDangKyRepo.Setup(r => r.LayDangKyCungMonDangHocAsync(maSV, "OOP", lhp.MaHocKy)).ReturnsAsync((DangKyHocPhan?)null);
        _mockDangKyRepo.Setup(r => r.DemSiSoDangKyAsync(maLHP)).ReturnsAsync(10);
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaHocKyAsync(maSV, lhp.MaHocKy)).ReturnsAsync(new List<DangKyHocPhan>());

        // Act
        var result = await _service.DangKyAsync(maSV, maLHP);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("DangHoc", existingDk.TrangThai);
        Assert.Equal("test_user", existingDk.NguoiDK);
        _mockDangKyRepo.Verify(r => r.CapNhatAsync(existingDk), Times.Once);
        _mockDangKyRepo.Verify(r => r.ThemAsync(It.IsAny<DangKyHocPhan>()), Times.Never);
        _mockHocPhiService.Verify(h => h.TinhLaiHocPhiAsync(maSV, lhp.MaHocKy), Times.Once);
    }

    [Fact]
    public async Task HuyDangKyAsync_HopLe_ChuyenSangDaHuyVaTinhLaiHocPhi()
    {
        // Arrange
        string maSV = "SV001";
        string maLHP = "LHP_OOP_01";
        var lhp = CreateSampleLhp(maLHP, "OOP", "Lập trình hướng đối tượng", 2, 1);
        var existingDk = new DangKyHocPhan
        {
            Id = Guid.NewGuid(),
            MaSV = maSV,
            MaLHP = maLHP,
            TrangThai = "DangHoc",
            LopHocPhan = lhp
        };

        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLHP)).ReturnsAsync(existingDk);

        // Act
        await _service.HuyDangKyAsync(maSV, maLHP);

        // Assert
        Assert.Equal("DaHuy", existingDk.TrangThai);
        _mockDangKyRepo.Verify(r => r.CapNhatAsync(existingDk), Times.Once);
        _mockHocPhiService.Verify(h => h.TinhLaiHocPhiAsync(maSV, lhp.MaHocKy), Times.Once);
    }

    [Fact]
    public async Task HuyDangKyAsync_KhongTonTai_ThrowsException()
    {
        // Arrange
        string maSV = "SV001";
        string maLHP = "LHP_OOP_01";
        _mockDangKyRepo.Setup(r => r.LayTheoMaSVVaMaLHPAsync(maSV, maLHP)).ReturnsAsync((DangKyHocPhan?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.HuyDangKyAsync(maSV, maLHP));
        _mockDangKyRepo.Verify(r => r.CapNhatAsync(It.IsAny<DangKyHocPhan>()), Times.Never);
    }
}
