using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Infrastructure.Export;
using Xunit;

namespace QuanLyDKHP.Tests;

public class ExportServiceTests
{
    private readonly ExcelExportService _excelExportService;
    private readonly PdfExportService _pdfExportService;

    public ExportServiceTests()
    {
        _excelExportService = new ExcelExportService();
        _pdfExportService = new PdfExportService();
    }

    [Fact]
    public async Task XuatExcelAsync_GeneratesNonEmptyByteArray()
    {
        // Arrange
        var data = new List<SinhVienDto>
        {
            new SinhVienDto { MaSV = "21001", HoTen = "Nguyễn Văn A", LopSinhHoat = "CNTT1", KhoaHoc = "K21", SoTinChiDangKy = 15 },
            new SinhVienDto { MaSV = "21002", HoTen = "Trần Thị B", LopSinhHoat = "CNTT2", KhoaHoc = "K21", SoTinChiDangKy = 12 }
        };

        var columns = new List<(string TieuDe, Func<SinhVienDto, object?> LayGiaTri)>
        {
            ("Mã SV", x => x.MaSV),
            ("Họ và tên", x => x.HoTen),
            ("Lớp", x => x.LopSinhHoat),
            ("Số TC", x => x.SoTinChiDangKy)
        };

        // Act
        byte[] bytes = await _excelExportService.XuatExcelAsync("SinhVien", data, columns);

        // Assert
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 100);
    }

    [Fact]
    public async Task XuatPhieuHocPhiPdfAsync_GeneratesNonEmptyPdf()
    {
        // Arrange
        var chiTiet = new HocPhiChiTietDto
        {
            MaSV = "21001",
            HoTen = "Nguyễn Văn A",
            LopSinhHoat = "CNTT1",
            TenHocKy = "Học kỳ 1 - 2024-2025",
            TongSoTinChi = 6,
            TongHocPhi = 3000000,
            DaDong = 3000000,
            DanhSachMon = new List<HocPhiChiTietMonDto>
            {
                new HocPhiChiTietMonDto
                {
                    MaLHP = "LHP01",
                    TenMon = "Lập trình C#",
                    SoTinChiLT = 2,
                    SoTinChiTH = 1,
                    DonGiaLT = 450000,
                    DonGiaTH = 600000,
                    ThanhTien = 1500000
                },
                new HocPhiChiTietMonDto
                {
                    MaLHP = "LHP02",
                    TenMon = "Cơ sở dữ liệu",
                    SoTinChiLT = 2,
                    SoTinChiTH = 1,
                    DonGiaLT = 450000,
                    DonGiaTH = 600000,
                    ThanhTien = 1500000
                }
            }
        };

        // Act
        byte[] pdfBytes = await _pdfExportService.XuatPhieuHocPhiPdfAsync(chiTiet);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        // Standard PDF magic header %PDF
        Assert.True(pdfBytes.Length > 500);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public async Task XuatDanhSachThiPdfAsync_GeneratesNonEmptyPdf()
    {
        // Arrange
        var danhSach = new List<DanhSachThiDto>
        {
            new DanhSachThiDto { MaSV = "21001", HoTen = "Nguyễn Văn A", LopSinhHoat = "CNTT1", MaLHP = "LHP01" },
            new DanhSachThiDto { MaSV = "21002", HoTen = "Trần Thị B", LopSinhHoat = "CNTT2", MaLHP = "LHP01" }
        };

        // Act
        byte[] pdfBytes = await _pdfExportService.XuatDanhSachThiPdfAsync(danhSach, "Lập trình C#", "LHP01", "HK1 - 2024-2025");

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public async Task XuatPhieuKetQuaDKHPPdfAsync_GeneratesNonEmptyPdf()
    {
        // Arrange
        var phieu = new PhieuDangKyDto
        {
            MaSV = "21001",
            HoTen = "Nguyễn Văn A",
            LopSinhHoat = "CNTT1",
            KhoaHoc = "K21",
            TenHocKy = "Học kỳ 1 - 2024-2025",
            TongSoTinChi = 3,
            NgayIn = DateTime.Now,
            DanhSachMon = new List<PhieuDangKyMonDto>
            {
                new PhieuDangKyMonDto
                {
                    MaMon = "CS01",
                    TenMon = "Lập trình C#",
                    SoTinChi = 3,
                    MaLHP = "LHP01",
                    TenGiangVien = "ThS. Nguyễn Văn C",
                    DiemSo = null,
                    DiemChu = null
                }
            }
        };

        // Act
        byte[] pdfBytes = await _pdfExportService.XuatPhieuKetQuaDKHPPdfAsync(phieu);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }
}
