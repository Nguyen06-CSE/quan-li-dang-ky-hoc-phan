using System;

namespace QuanLyDKHP.Core.Entities;

public class DangKyHocPhan : IAuditableEntity
{
    public Guid Id { get; set; }
    public string MaSV { get; set; } = string.Empty;
    public string MaLHP { get; set; } = string.Empty;
    public string? HinhThucDK { get; set; }
    public DateTime NgayDK { get; set; }
    public string? NguoiDK { get; set; }
    public decimal? DiemSo { get; set; }
    public string? DiemChu { get; set; }
    public string TrangThai { get; set; } = "DangHoc";
    public decimal? SoTienPhaiDong { get; set; }
    public decimal SoTienDaDong { get; set; }
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }

    public SinhVien SinhVien { get; set; } = null!;
    public LopHocPhan LopHocPhan { get; set; } = null!;
}
