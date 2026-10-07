using System;

namespace QuanLyDKHP.Core.Entities;

public class HocPhiHocKy : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MaSV { get; set; } = string.Empty;
    public string MaHocKy { get; set; } = string.Empty;
    public int TongSoTinChi { get; set; }
    public decimal TongHocPhi { get; set; }
    public decimal DaDong { get; set; }
    public decimal ConNo { get; set; }
    public bool DaKhoaSo { get; set; } // true: Học kỳ đã đóng, số liệu bất biến
    public DateTime? NgayKhoaSo { get; set; }
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }

    public SinhVien? SinhVien { get; set; }
    public HocKy? HocKy { get; set; }
}
