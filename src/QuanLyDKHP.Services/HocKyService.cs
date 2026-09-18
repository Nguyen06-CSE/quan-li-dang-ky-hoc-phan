using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class HocKyService : IHocKyService
{
    private readonly IHocKyRepository _hocKyRepository;
    private readonly ICurrentUserService _currentUserService;

    public HocKyService(IHocKyRepository hocKyRepository, ICurrentUserService currentUserService)
    {
        _hocKyRepository = hocKyRepository;
        _currentUserService = currentUserService;
    }

    private void CheckPermission()
    {
        if (_currentUserService.CurrentUser != null)
        {
            PermissionMatrix.Authorize(ChucNang.CrudHocKyLopHocPhan, _currentUserService.CurrentUser.Role);
        }
    }

    public async Task<List<HocKy>> LayTatCaAsync()
    {
        return await _hocKyRepository.LayTatCaAsync();
    }

    public async Task DatHocKyHienHanhAsync(string maHocKy)
    {
        CheckPermission();
        if (string.IsNullOrWhiteSpace(maHocKy))
            throw new InvalidOperationException("Mã học kỳ không được để trống.");

        var hk = await _hocKyRepository.GetByIdAsync(maHocKy);
        if (hk == null)
            throw new InvalidOperationException($"Không tìm thấy học kỳ có mã '{maHocKy}'.");

        await _hocKyRepository.DatHocKyHienHanhAsync(maHocKy);
    }

    public async Task ThemAsync(HocKy hocKy)
    {
        CheckPermission();
        if (string.IsNullOrWhiteSpace(hocKy.MaHocKy))
            throw new InvalidOperationException("Mã học kỳ không được để trống.");

        if (string.IsNullOrWhiteSpace(hocKy.TenHocKy))
            throw new InvalidOperationException("Tên học kỳ không được để trống.");

        if (await _hocKyRepository.TonTaiAsync(hocKy.MaHocKy))
            throw new InvalidOperationException($"Học kỳ với mã '{hocKy.MaHocKy}' đã tồn tại.");

        await _hocKyRepository.ThemAsync(hocKy);

        if (hocKy.DangMo)
        {
            await _hocKyRepository.DatHocKyHienHanhAsync(hocKy.MaHocKy);
        }
    }

    public async Task CapNhatAsync(HocKy hocKy)
    {
        CheckPermission();
        if (string.IsNullOrWhiteSpace(hocKy.TenHocKy))
            throw new InvalidOperationException("Tên học kỳ không được để trống.");

        var existing = await _hocKyRepository.GetByIdAsync(hocKy.MaHocKy);
        if (existing == null)
            throw new InvalidOperationException($"Không tìm thấy học kỳ có mã '{hocKy.MaHocKy}'.");

        existing.TenHocKy = hocKy.TenHocKy;
        existing.NgayBatDau = hocKy.NgayBatDau;
        existing.NgayKetThuc = hocKy.NgayKetThuc;

        await _hocKyRepository.CapNhatAsync(existing);

        if (hocKy.DangMo && !existing.DangMo)
        {
            await _hocKyRepository.DatHocKyHienHanhAsync(hocKy.MaHocKy);
        }
    }
}
