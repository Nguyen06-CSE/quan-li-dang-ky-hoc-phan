using System;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.App.Dtos;

public class LopHocPhanDisplayDto
{
    public string MaLHP { get; set; } = string.Empty;
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string? MaGV { get; set; }
    public string TenGiangVien { get; set; } = "Chưa phân công";
    public bool CoGiangVien => !string.IsNullOrEmpty(MaGV);
    public string? LoaiHinhDT { get; set; }
    public int? SiSoToiDa { get; set; }
    public int SiSoDangKy { get; set; }
    public string SiSoHienThi => SiSoToiDa.HasValue ? $"{SiSoDangKy}/{SiSoToiDa.Value}" : $"{SiSoDangKy}/∞";
    public double TyLeLapDay => SiSoToiDa.HasValue && SiSoToiDa.Value > 0 
        ? Math.Min(100.0, (double)SiSoDangKy / SiSoToiDa.Value * 100.0) 
        : 0;
    
    // Status color category: Primary (<80%), Warning (80-99%), Danger (>=100%)
    public string SiSoStatusClass
    {
        get
        {
            if (!SiSoToiDa.HasValue || SiSoToiDa.Value == 0) return "Primary";
            double pct = (double)SiSoDangKy / SiSoToiDa.Value * 100.0;
            if (pct >= 100.0) return "Danger";
            if (pct >= 80.0) return "Warning";
            return "Primary";
        }
    }

    public bool GiangDayOnline { get; set; }
    public LopHocPhan Entity { get; set; } = null!;

    private static readonly string[] TenThu =
        { "", "", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN" };

    /// <summary>Hiển thị gọn lịch học trên danh sách LHP. Rỗng nếu lớp chưa được nhập lịch (xem db/2026-10-01_them-lich-hoc-LopHocPhan.sql).</summary>
    public string LichHocHienThi
    {
        get
        {
            if (Entity?.Thu is not int thu || thu < 2 || thu > 8 || Entity.TietBatDau == null || Entity.SoTiet == null)
                return "Chưa xếp lịch";
            string phong = string.IsNullOrWhiteSpace(Entity.Phong) ? "" : $" - {Entity.Phong}";
            return $"{TenThu[thu]}, tiết {Entity.TietBatDau}-{Entity.TietBatDau + Entity.SoTiet - 1}{phong}";
        }
    }
}
