using System;
using System.Collections.Generic;

namespace QuanLyDKHP.Core.Dtos;

/// <summary>
/// DTO Tab 1: Danh sách sinh viên đăng ký theo môn
/// </summary>
public class SinhVienTheoMonDto
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public string MaLHP { get; set; } = string.Empty;
    public string? TenGiangVien { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

/// <summary>
/// DTO Tab 2: Danh sách thi theo môn / LHP
/// </summary>
public class DanhSachThiDto
{
    public int Stt { get; set; }
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public string MaLHP { get; set; } = string.Empty;
}

/// <summary>
/// DTO Tab 3: Thống kê số lượng sinh viên theo môn trong học kỳ
/// </summary>
public class ThongKeMonDto
{
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoSinhVien { get; set; }
    public int SoLhpDangMo { get; set; }
}

/// <summary>
/// DTO Tab 4: Phiếu kết quả đăng ký học phần của 1 sinh viên
/// </summary>
public class PhieuDangKyDto
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public string? KhoaHoc { get; set; }
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public List<PhieuDangKyMonDto> DanhSachMon { get; set; } = new();
    public int TongSoTinChi { get; set; }
    public DateTime NgayIn { get; set; } = DateTime.Now;
}

public class PhieuDangKyMonDto
{
    public int Stt { get; set; }
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoTinChi { get; set; }
    public string MaLHP { get; set; } = string.Empty;
    public string? TenGiangVien { get; set; }
    public decimal? DiemSo { get; set; }
    public string? DiemChu { get; set; }
    public string DiemSoFormat => DiemSo.HasValue ? DiemSo.Value.ToString("F1") : "--";
    public string DiemChuFormat => !string.IsNullOrWhiteSpace(DiemChu) ? DiemChu : "--";
}
