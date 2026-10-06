using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Services;

public class LocalReadService : ILocalReadService
{
    private readonly IDbContextFactory<LocalAppDbContext> _localFactory;

    public LocalReadService(IDbContextFactory<LocalAppDbContext> localFactory)
    {
        _localFactory = localFactory;
    }

    public async Task<List<SinhVienDto>> GetSinhViensLocalAsync(string? tuKhoa = null, string? lop = null, string? khoaHoc = null)
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        var query = local.SinhViens.AsNoTracking().Where(s => !s.IsDeleted);

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

        var activeHocKy = await local.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.DangMo);
        string? currentHocKyMa = activeHocKy?.MaHocKy;

        return await query
            .OrderBy(s => s.MaSV)
            .Select(s => new SinhVienDto
            {
                MaSV = s.MaSV,
                HoTen = s.HoTen,
                LopSinhHoat = s.LopSinhHoat,
                KhoaHoc = s.KhoaHoc,
                SoTinChiDangKy = local.DangKyHocPhans
                    .Where(dk => dk.MaSV == s.MaSV
                                 && dk.TrangThai == "DangHoc"
                                 && (currentHocKyMa == null || dk.LopHocPhan.MaHocKy == currentHocKyMa))
                    .Sum(dk => dk.LopHocPhan.MonHoc.SoTinChiLT + dk.LopHocPhan.MonHoc.SoTinChiTH)
            })
            .ToListAsync();
    }

    public async Task<List<MonHocDto>> GetMonHocsLocalAsync(string? tuKhoa = null)
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        var query = local.MonHocs.AsNoTracking().Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var key = tuKhoa.Trim().ToLower();
            query = query.Where(m => m.MaMon.ToLower().Contains(key) || m.TenMon.ToLower().Contains(key));
        }

        var activeHocKy = await local.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.DangMo);
        string? currentHocKyMa = activeHocKy?.MaHocKy;

        return await query
            .OrderBy(m => m.TenMon)
            .Select(m => new MonHocDto
            {
                MaMon = m.MaMon,
                TenMon = m.TenMon,
                SoTinChiLT = m.SoTinChiLT,
                SoTinChiTH = m.SoTinChiTH,
                BacDaoTao = m.BacDaoTao,
                SoLhpDangMo = currentHocKyMa == null
                    ? 0
                    : local.LopHocPhans.Count(l => l.MaMon == m.MaMon && l.MaHocKy == currentHocKyMa && !l.IsDeleted)
            })
            .ToListAsync();
    }

    public async Task<List<LopHocPhan>> GetLopHocPhansLocalAsync(string? maHocKy = null, string? tuKhoa = null, string? maMon = null)
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        var query = local.LopHocPhans
            .AsNoTracking()
            .Include(l => l.MonHoc)
            .Include(l => l.HocKy)
            .Where(l => !l.IsDeleted);

        if (!string.IsNullOrWhiteSpace(maHocKy))
        {
            query = query.Where(l => l.MaHocKy == maHocKy);
        }

        if (!string.IsNullOrWhiteSpace(maMon))
        {
            query = query.Where(l => l.MaMon == maMon);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var key = tuKhoa.Trim().ToLower();
            query = query.Where(l => l.MaLHP.ToLower().Contains(key) ||
                                     l.MonHoc.TenMon.ToLower().Contains(key) ||
                                     l.MaMon.ToLower().Contains(key));
        }

        return await query.OrderBy(l => l.MaLHP).ToListAsync();
    }

    public async Task<List<DangKyHocPhan>> GetDangKyLocalAsync(string maSV, string maHocKy)
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        return await local.DangKyHocPhans
            .AsNoTracking()
            .Include(dk => dk.SinhVien)
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.HocKy)
            .Where(dk => dk.MaSV == maSV && dk.LopHocPhan.MaHocKy == maHocKy)
            .OrderBy(dk => dk.LopHocPhan.MaLHP)
            .ToListAsync();
    }

    public async Task<List<DangKyHocPhan>> GetDangKyNhieuSvLocalAsync(IEnumerable<string> dsMaSV, string maHocKy)
    {
        var listSv = dsMaSV.Distinct().ToList();
        if (listSv.Count == 0) return new List<DangKyHocPhan>();

        await using var local = await _localFactory.CreateDbContextAsync();
        return await local.DangKyHocPhans
            .AsNoTracking()
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .Where(dk => listSv.Contains(dk.MaSV) && dk.LopHocPhan.MaHocKy == maHocKy)
            .ToListAsync();
    }

    public async Task<int> DemSiSoDangKyLocalAsync(string maLHP)
    {
        await using var local = await _localFactory.CreateDbContextAsync();
        return await local.DangKyHocPhans
            .CountAsync(dk => dk.MaLHP == maLHP && dk.TrangThai == "DangHoc");
    }

    public async Task<Dictionary<string, int>> DemSiSoDangKyBulkLocalAsync(IEnumerable<string> dsMaLHP)
    {
        var list = dsMaLHP.Distinct().ToList();
        if (list.Count == 0) return new Dictionary<string, int>();

        await using var local = await _localFactory.CreateDbContextAsync();
        return await local.DangKyHocPhans
            .AsNoTracking()
            .Where(dk => list.Contains(dk.MaLHP) && dk.TrangThai == "DangHoc")
            .GroupBy(dk => dk.MaLHP)
            .Select(g => new { MaLHP = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MaLHP, x => x.Count);
    }

    public async Task<List<SinhVienTheoMonDto>> GetDsSinhVienTheoMonLocalAsync(string maMon, string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(maHocKy))
            return new List<SinhVienTheoMonDto>();

        await using var local = await _localFactory.CreateDbContextAsync();
        var query = from dk in local.DangKyHocPhans.AsNoTracking()
                    join lhp in local.LopHocPhans.AsNoTracking() on dk.MaLHP equals lhp.MaLHP
                    join sv in local.SinhViens.AsNoTracking() on dk.MaSV equals sv.MaSV
                    where lhp.MaMon == maMon && lhp.MaHocKy == maHocKy
                    orderby dk.MaLHP, sv.MaSV
                    select new SinhVienTheoMonDto
                    {
                        MaSV = sv.MaSV,
                        HoTen = sv.HoTen,
                        LopSinhHoat = sv.LopSinhHoat,
                        MaLHP = dk.MaLHP,
                        TenGiangVien = lhp.MaGV,
                        TrangThai = dk.TrangThai
                    };

        return await query.ToListAsync();
    }

    public async Task<DashboardStatsDto> GetDashboardStatsLocalAsync(string? maHocKy)
    {
        await using var local = await _localFactory.CreateDbContextAsync();

        var tongSV = await local.SinhViens.CountAsync(s => !s.IsDeleted);
        var tongMH = await local.MonHocs.CountAsync(m => !m.IsDeleted);

        int tongLHP;
        int tongDK;

        if (!string.IsNullOrEmpty(maHocKy))
        {
            tongLHP = await local.LopHocPhans
                .CountAsync(l => l.MaHocKy == maHocKy && !l.IsDeleted);

            tongDK = await local.DangKyHocPhans
                .CountAsync(d => d.LopHocPhan.MaHocKy == maHocKy && d.TrangThai == "DangHoc");
        }
        else
        {
            tongLHP = await local.LopHocPhans.CountAsync(l => !l.IsDeleted);
            tongDK = await local.DangKyHocPhans.CountAsync(d => d.TrangThai == "DangHoc");
        }

        return new DashboardStatsDto
        {
            TongSinhVien = tongSV,
            TongMonHoc = tongMH,
            TongLopHocPhan = tongLHP,
            TongLuotDangKy = tongDK
        };
    }
}
