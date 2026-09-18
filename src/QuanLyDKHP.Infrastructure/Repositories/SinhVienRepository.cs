using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class SinhVienRepository : ISinhVienRepository
{
    private readonly AppDbContext _context;

    public SinhVienRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SinhVienDto>> TimKiemAsync(string? tuKhoa, string? lop, string? khoaHoc, int page, int pageSize)
    {
        var query = _context.SinhViens.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var key = tuKhoa.Trim().ToLower();
            query = query.Where(s => s.MaSV.ToLower().Contains(key) || s.HoTen.ToLower().Contains(key));
        }

        if (!string.IsNullOrWhiteSpace(lop) && lop != "Tất cả")
        {
            query = query.Where(s => s.LopSinhHoat == lop);
        }

        if (!string.IsNullOrWhiteSpace(khoaHoc) && khoaHoc != "Tất cả")
        {
            query = query.Where(s => s.KhoaHoc == khoaHoc);
        }

        int totalItems = await query.CountAsync();

        // Lấy học kỳ đang mở
        var activeHocKy = await _context.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.DangMo);
        string? currentHocKyMa = activeHocKy?.MaHocKy;

        var items = await query
            .OrderBy(s => s.MaSV)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SinhVienDto
            {
                MaSV = s.MaSV,
                HoTen = s.HoTen,
                LopSinhHoat = s.LopSinhHoat,
                KhoaHoc = s.KhoaHoc,
                SoTinChiDangKy = _context.DangKyHocPhans
                    .Where(dk => dk.MaSV == s.MaSV 
                                 && dk.TrangThai == "DangHoc" 
                                 && (currentHocKyMa == null || dk.LopHocPhan.MaHocKy == currentHocKyMa))
                    .Sum(dk => dk.LopHocPhan.MonHoc.SoTinChiLT + dk.LopHocPhan.MonHoc.SoTinChiTH)
            })
            .ToListAsync();

        return new PagedResult<SinhVienDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<List<SinhVien>> LayDanhSachAsync(string? tuKhoa = null, string? lop = null, string? khoaHoc = null)
    {
        var query = _context.SinhViens.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var key = tuKhoa.Trim().ToLower();
            query = query.Where(s => s.MaSV.ToLower().Contains(key) || s.HoTen.ToLower().Contains(key));
        }

        if (!string.IsNullOrWhiteSpace(lop) && lop != "Tất cả")
        {
            query = query.Where(s => s.LopSinhHoat == lop);
        }

        if (!string.IsNullOrWhiteSpace(khoaHoc) && khoaHoc != "Tất cả")
        {
            query = query.Where(s => s.KhoaHoc == khoaHoc);
        }

        return await query.OrderBy(s => s.MaSV).ToListAsync();
    }

    public async Task<List<string>> GetDanhSachLopSinhHoatAsync()
    {
        return await _context.SinhViens
            .AsNoTracking()
            .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.LopSinhHoat))
            .Select(s => s.LopSinhHoat!)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
    }

    public async Task<List<string>> GetDanhSachKhoaHocAsync()
    {
        return await _context.SinhViens
            .AsNoTracking()
            .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.KhoaHoc))
            .Select(s => s.KhoaHoc!)
            .Distinct()
            .OrderBy(k => k)
            .ToListAsync();
    }

    public async Task<SinhVien?> GetByIdAsync(string maSV)
    {
        return await _context.SinhViens.FirstOrDefaultAsync(s => s.MaSV == maSV && !s.IsDeleted);
    }

    public async Task ThemAsync(SinhVien sv)
    {
        _context.SinhViens.Add(sv);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(SinhVien sv)
    {
        _context.SinhViens.Update(sv);
        await _context.SaveChangesAsync();
    }

    public async Task XoaMoiAsync(string maSV)
    {
        var sv = await _context.SinhViens.FirstOrDefaultAsync(s => s.MaSV == maSV);
        if (sv != null)
        {
            sv.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TonTaiAsync(string maSV)
    {
        return await _context.SinhViens.AnyAsync(s => s.MaSV == maSV && !s.IsDeleted);
    }

    public async Task<bool> CoDangKyHocPhanDangHocAsync(string maSV)
    {
        return await _context.DangKyHocPhans.AnyAsync(dk => dk.MaSV == maSV && dk.TrangThai == "DangHoc");
    }
}
