using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class SinhVienService : ISinhVienService
{
    private readonly ISinhVienRepository _sinhVienRepository;

    public SinhVienService(ISinhVienRepository sinhVienRepository)
    {
        _sinhVienRepository = sinhVienRepository;
    }

    public Task<PagedResult<SinhVienDto>> TimKiemAsync(string? tuKhoa, string? lop, string? khoaHoc, int page, int pageSize)
    {
        return _sinhVienRepository.TimKiemAsync(tuKhoa, lop, khoaHoc, page, pageSize);
    }

    public Task<List<SinhVien>> LayDanhSachAsync(string? tuKhoa = null, string? lop = null, string? khoaHoc = null)
    {
        return _sinhVienRepository.LayDanhSachAsync(tuKhoa, lop, khoaHoc);
    }

    public Task<List<string>> GetDanhSachLopSinhHoatAsync()
    {
        return _sinhVienRepository.GetDanhSachLopSinhHoatAsync();
    }

    public Task<List<string>> GetDanhSachKhoaHocAsync()
    {
        return _sinhVienRepository.GetDanhSachKhoaHocAsync();
    }

    public async Task ThemAsync(SinhVien sv)
    {
        if (string.IsNullOrWhiteSpace(sv.MaSV))
            throw new InvalidOperationException("Mã sinh viên không được để trống.");

        if (string.IsNullOrWhiteSpace(sv.HoTen))
            throw new InvalidOperationException("Họ tên sinh viên không được để trống.");

        if (await _sinhVienRepository.TonTaiAsync(sv.MaSV))
            throw new InvalidOperationException($"Mã sinh viên '{sv.MaSV}' đã tồn tại trong hệ thống.");

        await _sinhVienRepository.ThemAsync(sv);
    }

    public async Task CapNhatAsync(SinhVien sv)
    {
        if (string.IsNullOrWhiteSpace(sv.HoTen))
            throw new InvalidOperationException("Họ tên sinh viên không được để trống.");

        var existing = await _sinhVienRepository.GetByIdAsync(sv.MaSV);
        if (existing == null)
            throw new InvalidOperationException($"Không tìm thấy sinh viên có mã '{sv.MaSV}'.");

        existing.HoTen = sv.HoTen;
        existing.LopSinhHoat = sv.LopSinhHoat;
        existing.KhoaHoc = sv.KhoaHoc;

        await _sinhVienRepository.CapNhatAsync(existing);
    }

    public async Task XoaAsync(string maSV)
    {
        if (await _sinhVienRepository.CoDangKyHocPhanDangHocAsync(maSV))
        {
            throw new InvalidOperationException("Không thể xóa: sinh viên đang có đăng ký học phần.");
        }

        await _sinhVienRepository.XoaMoiAsync(maSV);
    }

    public Task<bool> TonTaiAsync(string maSV)
    {
        return _sinhVienRepository.TonTaiAsync(maSV);
    }
}
