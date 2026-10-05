using System;
using System.Collections.Generic;

namespace QuanLyDKHP.Core.Entities;

public class LopHocPhan : IAuditableEntity, ISoftDeleteEntity
{
    public string MaLHP { get; set; } = string.Empty;
    public string MaMon { get; set; } = string.Empty;
    public string MaHocKy { get; set; } = string.Empty;
    public string? MaGV { get; set; }
    public string? LoaiHinhDT { get; set; }
    public int? SiSoToiDa { get; set; }
    public bool GiangDayOnline { get; set; }

    // Lịch học — cột mới, xem db/2026-10-01_them-lich-hoc-LopHocPhan.sql.
    // Có thể NULL với các lớp học phần chưa được nhập lịch (toàn bộ dữ liệu hiện có trong Neon).
    public int? Thu { get; set; }          // 2 = Thứ 2 ... 7 = Thứ 7, 8 = Chủ nhật
    public int? TietBatDau { get; set; }
    public int? SoTiet { get; set; }
    public string? Phong { get; set; }
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public bool IsDeleted { get; set; }

    public MonHoc MonHoc { get; set; } = null!;
    public HocKy HocKy { get; set; } = null!;
    public ICollection<DangKyHocPhan> DangKyHocPhans { get; set; } = new List<DangKyHocPhan>();
}
