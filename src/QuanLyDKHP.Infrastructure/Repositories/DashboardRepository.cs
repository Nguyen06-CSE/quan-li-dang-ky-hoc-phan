using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _dbContext;

    public DashboardRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<HocKy>> GetAllHocKyAsync()
    {
        return await _dbContext.HocKys
            .OrderByDescending(h => h.NgayBatDau)
            .ToListAsync();
    }

    public async Task<HocKy?> GetHocKyMacDinhAsync()
    {
        var hk = await _dbContext.HocKys.FirstOrDefaultAsync(h => h.DangMo);
        if (hk != null) return hk;

        return await _dbContext.HocKys.OrderByDescending(h => h.NgayBatDau).FirstOrDefaultAsync();
    }

    public async Task<DashboardStatsDto> GetStatsAsync(string? maHocKy)
    {
        var tongSV = await _dbContext.SinhViens.CountAsync(s => !s.IsDeleted);
        var tongMH = await _dbContext.MonHocs.CountAsync(m => !m.IsDeleted);

        int tongLHP = 0;
        int tongDK = 0;

        if (!string.IsNullOrEmpty(maHocKy))
        {
            tongLHP = await _dbContext.LopHocPhans
                .CountAsync(l => l.MaHocKy == maHocKy && !l.IsDeleted);

            tongDK = await _dbContext.DangKyHocPhans
                .CountAsync(d => d.LopHocPhan.MaHocKy == maHocKy && d.TrangThai == "DangHoc");
        }
        else
        {
            tongLHP = await _dbContext.LopHocPhans.CountAsync(l => !l.IsDeleted);
            tongDK = await _dbContext.DangKyHocPhans.CountAsync(d => d.TrangThai == "DangHoc");
        }

        return new DashboardStatsDto
        {
            TongSinhVien = tongSV,
            TongMonHoc = tongMH,
            TongLopHocPhan = tongLHP,
            TongLuotDangKy = tongDK
        };
    }

    public async Task<List<GiangVienLhpDto>> GetLhpByGiangVienAsync(string maGV, string? maHocKy)
    {
        if (string.IsNullOrEmpty(maGV)) return new List<GiangVienLhpDto>();

        var query = _dbContext.LopHocPhans
            .Where(l => l.MaGV == maGV && !l.IsDeleted);

        if (!string.IsNullOrEmpty(maHocKy))
        {
            query = query.Where(l => l.MaHocKy == maHocKy);
        }

        return await query
            .Select(l => new GiangVienLhpDto
            {
                MaLHP = l.MaLHP,
                TenMon = l.MonHoc.TenMon,
                SiSo = l.DangKyHocPhans.Count(d => d.TrangThai == "DangHoc")
            })
            .ToListAsync();
    }
}
