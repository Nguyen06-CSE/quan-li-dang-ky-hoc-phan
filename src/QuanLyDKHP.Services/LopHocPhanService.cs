using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class LopHocPhanService : ILopHocPhanService
{
    private readonly ILopHocPhanRepository _lhpRepository;
    private readonly IHocKyRepository _hocKyRepository;
    private readonly IMonHocRepository _monHocRepository;
    private readonly ICurrentUserService _currentUserService;

    public LopHocPhanService(
        ILopHocPhanRepository lhpRepository,
        IHocKyRepository hocKyRepository,
        IMonHocRepository monHocRepository,
        ICurrentUserService currentUserService)
    {
        _lhpRepository = lhpRepository;
        _hocKyRepository = hocKyRepository;
        _monHocRepository = monHocRepository;
        _currentUserService = currentUserService;
    }

    private void CheckPermission()
    {
        if (_currentUserService.CurrentUser != null)
        {
            PermissionMatrix.Authorize(ChucNang.CrudHocKyLopHocPhan, _currentUserService.CurrentUser.Role);
        }
    }

    public async Task<List<LopHocPhan>> LayTheoHocKyAsync(string maHocKy, string? tuKhoa, string? maMon)
    {
        if (string.IsNullOrWhiteSpace(maHocKy))
            return new List<LopHocPhan>();

        return await _lhpRepository.LayTheoHocKyAsync(maHocKy, tuKhoa, maMon);
    }

    public async Task<int> DemSiSoDangKyAsync(string maLHP)
    {
        return await _lhpRepository.DemSiSoDangKyAsync(maLHP);
    }

    public async Task ThemAsync(LopHocPhan lhp)
    {
        CheckPermission();
        if (string.IsNullOrWhiteSpace(lhp.MaLHP))
            throw new InvalidOperationException("Mã lớp học phần không được để trống.");

        if (string.IsNullOrWhiteSpace(lhp.MaMon))
            throw new InvalidOperationException("Vui lòng chọn Môn học.");

        if (string.IsNullOrWhiteSpace(lhp.MaHocKy))
            throw new InvalidOperationException("Mã học kỳ không được để trống.");

        if (!await _monHocRepository.TonTaiAsync(lhp.MaMon))
            throw new InvalidOperationException($"Không tìm thấy môn học có mã '{lhp.MaMon}'.");

        if (!await _hocKyRepository.TonTaiAsync(lhp.MaHocKy))
            throw new InvalidOperationException($"Không tìm thấy học kỳ có mã '{lhp.MaHocKy}'.");

        if (await _lhpRepository.TonTaiAsync(lhp.MaLHP))
            throw new InvalidOperationException($"Lớp học phần có mã '{lhp.MaLHP}' đã tồn tại.");

        await _lhpRepository.ThemAsync(lhp);
    }

    public async Task CapNhatAsync(LopHocPhan lhp)
    {
        CheckPermission();
        if (string.IsNullOrWhiteSpace(lhp.MaMon))
            throw new InvalidOperationException("Vui lòng chọn Môn học.");

        var existing = await _lhpRepository.GetByIdAsync(lhp.MaLHP);
        if (existing == null)
            throw new InvalidOperationException($"Không tìm thấy lớp học phần có mã '{lhp.MaLHP}'.");

        if (!await _monHocRepository.TonTaiAsync(lhp.MaMon))
            throw new InvalidOperationException($"Không tìm thấy môn học có mã '{lhp.MaMon}'.");

        existing.MaMon = lhp.MaMon;
        existing.MaGV = lhp.MaGV;
        existing.LoaiHinhDT = lhp.LoaiHinhDT;
        existing.SiSoToiDa = lhp.SiSoToiDa;
        existing.GiangDayOnline = lhp.GiangDayOnline;

        await _lhpRepository.CapNhatAsync(existing);
    }

    public async Task XoaAsync(string maLHP)
    {
        CheckPermission();
        if (await _lhpRepository.CoDangKyHocPhanAsync(maLHP))
        {
            throw new InvalidOperationException("Không thể xóa: lớp học phần đã có sinh viên đăng ký học phần.");
        }

        await _lhpRepository.XoaAsync(maLHP);
    }
}
