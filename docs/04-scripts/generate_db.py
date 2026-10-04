import os

os.makedirs("src/QuanLyDKHP.Core/Enums", exist_ok=True)
os.makedirs("src/QuanLyDKHP.Core/Entities", exist_ok=True)
os.makedirs("src/QuanLyDKHP.Infrastructure/Data", exist_ok=True)

with open("src/QuanLyDKHP.Core/Enums/UserRole.cs", "w") as f:
    f.write("""namespace QuanLyDKHP.Core.Enums;

public enum UserRole
{
    Admin,
    TroLyGiaoVu,
    GiaoVuBoMon,
    GiangVien
}
""")

with open("src/QuanLyDKHP.Core/Entities/IAuditableEntity.cs", "w") as f:
    f.write("""using System;

namespace QuanLyDKHP.Core.Entities;

public interface IAuditableEntity
{
    DateTime NgayTao { get; set; }
    DateTime? NgayCapNhat { get; set; }
}
""")

with open("src/QuanLyDKHP.Core/Entities/ISoftDeleteEntity.cs", "w") as f:
    f.write("""namespace QuanLyDKHP.Core.Entities;

public interface ISoftDeleteEntity
{
    bool IsDeleted { get; set; }
}
""")

with open("src/QuanLyDKHP.Core/Entities/NguoiDung.cs", "w") as f:
    f.write("""using System;
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
""")

with open("src/QuanLyDKHP.Core/Entities/SinhVien.cs", "w") as f:
    f.write("""using System;
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
""")

with open("src/QuanLyDKHP.Core/Entities/MonHoc.cs", "w") as f:
    f.write("""using System;
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
""")

with open("src/QuanLyDKHP.Core/Entities/HocKy.cs", "w") as f:
    f.write("""using System;
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
""")

with open("src/QuanLyDKHP.Core/Entities/LopHocPhan.cs", "w") as f:
    f.write("""using System;
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
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public bool IsDeleted { get; set; }

    public MonHoc MonHoc { get; set; } = null!;
    public HocKy HocKy { get; set; } = null!;
    public ICollection<DangKyHocPhan> DangKyHocPhans { get; set; } = new List<DangKyHocPhan>();
}
""")

with open("src/QuanLyDKHP.Core/Entities/DangKyHocPhan.cs", "w") as f:
    f.write("""using System;

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
""")

with open("src/QuanLyDKHP.Core/Entities/CauHinhHeThong.cs", "w") as f:
    f.write("""using System;

namespace QuanLyDKHP.Core.Entities;

public class CauHinhHeThong : IAuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}
""")

