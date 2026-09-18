using System;
using System.Collections.Generic;

namespace QuanLyDKHP.Core.Entities;

public class SinhVien : IAuditableEntity, ISoftDeleteEntity
{
    public string MaSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? LopSinhHoat { get; set; }
    public string? KhoaHoc { get; set; }
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<DangKyHocPhan> DangKyHocPhans { get; set; } = new List<DangKyHocPhan>();
}
