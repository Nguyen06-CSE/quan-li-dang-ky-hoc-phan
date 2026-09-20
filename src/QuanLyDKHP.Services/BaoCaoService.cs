using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class BaoCaoService : IBaoCaoService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IBaoCaoRepository _baoCaoRepository;

    public BaoCaoService(IDashboardRepository dashboardRepository, IBaoCaoRepository baoCaoRepository)
    {
        _dashboardRepository = dashboardRepository;
        _baoCaoRepository = baoCaoRepository;
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

    // Spec 012
    public Task<List<SinhVienTheoMonDto>> DsSinhVienTheoMonAsync(string maMon, string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(maHocKy))
        {
            return Task.FromResult(new List<SinhVienTheoMonDto>());
        }
        return _baoCaoRepository.LayDsSvTheoMonAsync(maMon, maHocKy);
    }

    public Task<List<DanhSachThiDto>> DsThiTheoMonAsync(string maMon, string maHocKy, string? maLHP)
    {
        if (string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(maHocKy))
        {
            return Task.FromResult(new List<DanhSachThiDto>());
        }
        return _baoCaoRepository.LayDsThiAsync(maMon, maHocKy, maLHP);
    }

    public Task<List<ThongKeMonDto>> ThongKeSoLuongTheoMonAsync(string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maHocKy))
        {
            return Task.FromResult(new List<ThongKeMonDto>());
        }
        return _baoCaoRepository.ThongKeSoSvTheoMonAsync(maHocKy);
    }

    public async Task<PhieuDangKyDto> LayPhieuDangKyAsync(string maSV, string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maSV) || string.IsNullOrWhiteSpace(maHocKy))
        {
            return new PhieuDangKyDto { MaSV = maSV ?? "", MaHocKy = maHocKy ?? "" };
        }
        var result = await _baoCaoRepository.LayPhieuDangKyAsync(maSV, maHocKy);
        return result ?? new PhieuDangKyDto { MaSV = maSV, MaHocKy = maHocKy };
    }
}
