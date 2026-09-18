using System;
using System.Collections.Generic;

namespace QuanLyDKHP.Core.Entities;

public class MonHoc : IAuditableEntity, ISoftDeleteEntity
{
    public string MaMon { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SoTinChiLT { get; set; }
    public int SoTinChiTH { get; set; }
    public string? BacDaoTao { get; set; }
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<LopHocPhan> LopHocPhans { get; set; } = new List<LopHocPhan>();
}
