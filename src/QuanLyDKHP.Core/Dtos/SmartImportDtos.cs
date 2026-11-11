using System.Collections.Generic;

namespace QuanLyDKHP.Core.Dtos;

public class ColumnMappingDto
{
    public int MaSVCol { get; set; } = -1;
    public int HoTenCol { get; set; } = -1;
    public int HoDemCol { get; set; } = -1;
    public int TenCol { get; set; } = -1;
    public int LopSHCol { get; set; } = -1;
    public int KhoaHocCol { get; set; } = -1;
    public bool HasHeader { get; set; } = true;
}

public class SinhVienImportPreviewDto
{
    public int Stt { get; set; }
    public int DongSo => Stt;
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string LopSinhHoat { get; set; } = string.Empty;
    public string KhoaHoc { get; set; } = string.Empty;
    
    /// <summary>
    /// "HopLe", "DaCoTrongLop", "Loi", "DaTonTaiHeThong"
    /// </summary>
    public string TrangThaiXacThuc { get; set; } = "HopLe";
    public string ThongBaoLoi { get; set; } = string.Empty;
    public string? CanhBaoXungDot { get; set; }
    public bool IsSelected { get; set; } = true;

    public bool IsHopLe => TrangThaiXacThuc == "HopLe";
    public bool IsDaCo => TrangThaiXacThuc == "DaCoTrongLop";
    public bool IsLoi => TrangThaiXacThuc == "Loi";
    public bool DaTonTaiTrongLop => TrangThaiXacThuc == "DaCoTrongLop";
    public bool HasXungDotLich => !string.IsNullOrEmpty(CanhBaoXungDot) && CanhBaoXungDot.Contains("Trùng lịch");
    public bool HasTrungMon => !string.IsNullOrEmpty(CanhBaoXungDot) && (CanhBaoXungDot.Contains("cùng môn") || CanhBaoXungDot.Contains("Trùng môn"));
    public string TrangThaiLoi => !string.IsNullOrEmpty(ThongBaoLoi) ? ThongBaoLoi : CanhBaoXungDot ?? string.Empty;
}

public class ThongKeCoCauDto
{
    public string Nhan { get; set; } = string.Empty;
    public string TenNhom => Nhan;
    public int SoLuong { get; set; }
    public double PhanTram { get; set; }
    public string HienThiPhanTram => $"{PhanTram:F1}%";
}

public class ClassInsightsDto
{
    public string MaLHP { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SiSoHienTai { get; set; }
    public int SiSoThemVao { get; set; }
    public int SiSoDuKien => SiSoHienTai + SiSoThemVao;
    public int SiSoSauKhiNap => SiSoDuKien;
    public int SiSoToiDa { get; set; }
    public string SiSoHienThi => $"{SiSoHienTai} / {(SiSoToiDa > 0 ? SiSoToiDa.ToString() : "∞")}";
    public double TiLeLapDayDuKien => SiSoToiDa > 0 ? System.Math.Min(100, (double)SiSoDuKien / SiSoToiDa * 100.0) : 0;
    public bool VuotSiSo => SiSoToiDa > 0 && SiSoDuKien > SiSoToiDa;
    public int SoLuongVuot => VuotSiSo ? (SiSoDuKien - SiSoToiDa) : 0;
    
    public int SoLuongHopLe { get; set; }
    public int SoLuongDaCo { get; set; }
    public int SoLuongLoi { get; set; }

    public bool CoXungDotLich => DanhSachCanhBao.Exists(c => c.Contains("Trùng lịch"));
    public bool CoTrungMon => DanhSachCanhBao.Exists(c => c.Contains("cùng môn") || c.Contains("Trùng môn"));

    public List<ThongKeCoCauDto> CoCauLop { get; set; } = new();
    public List<ThongKeCoCauDto> CoCauKhoa { get; set; } = new();
    public List<string> DanhSachCanhBao { get; set; } = new();
}

public class KetQuaSmartEnrollDto
{
    public string MaLHP { get; set; } = string.Empty;
    public int TongSoLuong { get; set; }
    public int ThanhCong { get; set; }
    public int BoQua { get; set; }
    public int Loi { get; set; }
    public int SoSinhVienMoiTao { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public List<string> ChiTietLoi { get; set; } = new();
}
