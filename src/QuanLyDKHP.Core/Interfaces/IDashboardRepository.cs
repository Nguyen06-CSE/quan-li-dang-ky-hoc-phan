using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public class DashboardStatsDto
{
    public int TongSinhVien { get; set; }
    public int TongMonHoc { get; set; }
    public int TongLopHocPhan { get; set; }
    public int TongLuotDangKy { get; set; }
}

public class GiangVienLhpDto
{
    public string MaLHP { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public int SiSo { get; set; }
}

public interface IDashboardRepository
{
    Task<List<HocKy>> GetAllHocKyAsync();
    Task<HocKy?> GetHocKyMacDinhAsync();
    Task<DashboardStatsDto> GetStatsAsync(string? maHocKy);
    Task<List<GiangVienLhpDto>> GetLhpByGiangVienAsync(string maGV, string? maHocKy);
}
