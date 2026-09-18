using System;
using QuanLyDKHP.Core.Enums;

namespace QuanLyDKHP.Core.Entities;

public class NguoiDung : IAuditableEntity, ISoftDeleteEntity
{
    public int Id { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? MaGV { get; set; }
    public string TrangThai { get; set; } = "HoatDong";
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public bool IsDeleted { get; set; }
}
