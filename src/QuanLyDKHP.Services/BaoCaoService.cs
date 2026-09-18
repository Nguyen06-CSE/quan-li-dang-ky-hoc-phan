using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class BaoCaoService : IBaoCaoService
{
    private readonly IDashboardRepository _dashboardRepository;

    public BaoCaoService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public Task<DashboardStatsDto> GetDashboardStatsAsync(string? maHocKy)
    {
        return _dashboardRepository.GetStatsAsync(maHocKy);
    }

    public Task<List<HocKy>> GetDanhSachHocKyAsync()
    {
        return _dashboardRepository.GetAllHocKyAsync();
    }

    public Task<HocKy?> GetHocKyMacDinhAsync()
    {
        return _dashboardRepository.GetHocKyMacDinhAsync();
    }

    public Task<List<GiangVienLhpDto>> GetLhpGiangVienAsync(string maGV, string? maHocKy)
    {
        return _dashboardRepository.GetLhpByGiangVienAsync(maGV, maHocKy);
    }
}
