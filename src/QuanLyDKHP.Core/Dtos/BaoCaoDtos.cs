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

    public bool IsDangHoc => string.Equals(TrangThai, "DangHoc", StringComparison.OrdinalIgnoreCase);
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
    public string? TenGiangVien { get; set; }
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
    public double TyLePhanBo { get; set; }

    public string BinhQuanMoiLop => SoLhpDangMo > 0
        ? $"{(double)SoSinhVien / SoLhpDangMo:F1} SV / lớp"
        : "Chưa mở lớp";
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

/// <summary>
/// DTO Tab 5 (Nâng Cấp 3 - Mục tiêu 3): Cảnh báo học vụ &amp; sinh viên nguy cơ (0 tín chỉ, thiếu tín chỉ tối thiểu, nợ học phí).
/// </summary>
public class CanhBaoHocVuDto
{
    public int Stt { get; set; }
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string LopSinhHoat { get; set; } = "--";
    public string KhoaHoc { get; set; } = "--";
    public int SoTinChiDangKy { get; set; }
    public int TinChiToiThieu { get; set; }
    public int SoTinChiThieu => Math.Max(0, TinChiToiThieu - SoTinChiDangKy);
    public decimal ConNoHocPhi { get; set; }
    public string LoaiNguyCo { get; set; } = string.Empty; // "KhongDangKy" | "ThieuTinChi" | "NoHocPhi"
    public string NhanNguyCo { get; set; } = string.Empty;
    public string HuongXuLyDeXuat { get; set; } = string.Empty;

    public string TinChiHienThi => $"{SoTinChiDangKy} / {TinChiToiThieu} TC";
    public string ConNoFormat => ConNoHocPhi > 0 ? $"{ConNoHocPhi:N0} đ" : "0 đ";
    public bool IsNguyCoCao => LoaiNguyCo == "KhongDangKy";
    public bool IsThieuTinChi => LoaiNguyCo == "ThieuTinChi";
    public bool IsNoHocPhi => LoaiNguyCo == "NoHocPhi";
}

/// <summary>
/// DTO Tab 6A (Nâng Cấp 3 - Mục tiêu 1): Phát hiện xung đột phòng học, trùng lịch dạy giảng viên và lớp chưa hoàn thiện.
/// </summary>
public class XungDotDaoTaoDto
{
    public int Stt { get; set; }
    public string LoaiXungDot { get; set; } = string.Empty; // "TrungGiangVien" | "TrungPhongHoc" | "ChuaHoanThien"
    public string TenLoaiHienThi { get; set; } = string.Empty;
    public string MucDo { get; set; } = "NghiemTrong"; // "NghiemTrong" | "CanhBao"
    public string DoiTuongXungDot { get; set; } = string.Empty;
    public string MaLhpChinh { get; set; } = string.Empty;
    public string TenMonChinh { get; set; } = string.Empty;
    public string MaLhpDoiUng { get; set; } = "--";
    public string TenMonDoiUng { get; set; } = string.Empty;
    public string ThoiGianDienRa { get; set; } = string.Empty;
    public string KhuyenNghiXuLy { get; set; } = string.Empty;

    public bool IsNghiemTrong => MucDo == "NghiemTrong";
    public string MoTaCapLop => string.IsNullOrWhiteSpace(MaLhpDoiUng) || MaLhpDoiUng == "--"
        ? $"{MaLhpChinh} ({TenMonChinh})"
        : $"{MaLhpChinh} ⇄ {MaLhpDoiUng}";
}

/// <summary>
/// DTO Tab 6B (Nâng Cấp 3 - Mục tiêu 4): Bảng theo dõi định mức &amp; tải giờ giảng của giảng viên trong học kỳ.
/// </summary>
public class DinhMucGiangDayDto
{
    public int Stt { get; set; }
    public string MaGV { get; set; } = string.Empty;
    public string HoTenGV { get; set; } = string.Empty;
    public int SoLopPhuTrach { get; set; }
    public int TongSinhVien { get; set; }
    public int TietLyThuyet { get; set; }
    public int TietThucHanh { get; set; }
    public double TongTietQuyDoi { get; set; }
    public double DinhMucChuan { get; set; } = 135.0; // Định mức chuẩn 1 học kỳ (270 tiết/năm)
    public string TrangThaiTai { get; set; } = "DatChuan"; // "QuaTai" | "DatChuan" | "ThieuGio"
    public string NhanTrangThai { get; set; } = string.Empty;
    public string DanhSachLopPhuTrach { get; set; } = string.Empty;

    public string TietQuyDoiHienThi => $"{TongTietQuyDoi:F1} / {DinhMucChuan:F0} tiết";
    public double TyLeDinhMuc => DinhMucChuan > 0
        ? Math.Min(100.0, Math.Round(TongTietQuyDoi / DinhMucChuan * 100.0, 1))
        : 0.0;

    public bool IsQuaTai => TrangThaiTai == "QuaTai";
    public bool IsDatChuan => TrangThaiTai == "DatChuan";
    public bool IsThieuGio => TrangThaiTai == "ThieuGio";
}
