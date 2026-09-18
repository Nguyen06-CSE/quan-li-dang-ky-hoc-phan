using System;
using System.Collections.Generic;

namespace QuanLyDKHP.Core.Entities;

public class HocKy : IAuditableEntity
{
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public bool DangMo { get; set; } = true;
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }

    public ICollection<LopHocPhan> LopHocPhans { get; set; } = new List<LopHocPhan>();
}
