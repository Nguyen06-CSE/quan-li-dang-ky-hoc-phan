using System.Collections.Generic;

namespace QuanLyDKHP.Core.Dtos;

public class HocPhiChiTietMonDto
{
    public string MaLHP { get; set; } = string.Empty;
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoTinChiLT { get; set; }
    public int SoTinChiTH { get; set; }
    public decimal DonGiaLT { get; set; }
    public decimal DonGiaTH { get; set; }
    public decimal ThanhTien { get; set; }
    public int TongTinChi => SoTinChiLT + SoTinChiTH;
    public string ThanhTienFormat => ThanhTien.ToString("N0") + " đ";
}

public class HocPhiChiTietDto
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public List<HocPhiChiTietMonDto> DanhSachMon { get; set; } = new();
    public int TongSoTinChi { get; set; }
    public decimal TongHocPhi { get; set; }
    public decimal DaDong { get; set; }
    public decimal ConNo => TongHocPhi - DaDong;
    public bool DaDongDu => ConNo <= 0;
    public string TongHocPhiFormat => TongHocPhi.ToString("N0") + " đ";
    public string DaDongFormat => DaDong.ToString("N0") + " đ";
    public string ConNoFormat => ConNo.ToString("N0") + " đ";
}

public class HocPhiTongHopDto
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public int TongSoTinChi { get; set; }
    public decimal TongHocPhi { get; set; }
    public decimal DaDong { get; set; }
    public decimal ConNo => TongHocPhi - DaDong;
    public bool DaDongDu => ConNo <= 0;
    public string TrangThaiText => DaDongDu ? "Đã đóng đủ" : "Còn nợ";
    public string TongHocPhiFormat => TongHocPhi.ToString("N0") + " đ";
    public string DaDongFormat => DaDong.ToString("N0") + " đ";
    public string ConNoFormat => ConNo.ToString("N0") + " đ";
}
