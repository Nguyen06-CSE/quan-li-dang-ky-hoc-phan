using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IBaoCaoService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(string? maHocKy);
    Task<List<HocKy>> GetDanhSachHocKyAsync();
    Task<HocKy?> GetHocKyMacDinhAsync();
    Task<List<GiangVienLhpDto>> GetLhpGiangVienAsync(string maGV, string? maHocKy);
}
