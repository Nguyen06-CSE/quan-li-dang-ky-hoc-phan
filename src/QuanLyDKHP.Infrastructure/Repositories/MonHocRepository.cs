using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class MonHocRepository : IMonHocRepository
{
    private readonly AppDbContext _context;

    public MonHocRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MonHoc>> LayDanhSachAsync(string? tuKhoa)
    {
        var query = _context.MonHocs.AsNoTracking().Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var key = tuKhoa.Trim().ToLower();
            query = query.Where(m => m.MaMon.ToLower().Contains(key) || m.TenMon.ToLower().Contains(key));
        }

        return await query.ToListAsync();
    }

    public async Task<MonHoc?> GetByIdAsync(string maMon)
    {
        return await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMon == maMon && !m.IsDeleted);
    }

    public async Task ThemAsync(MonHoc mon)
    {
        _context.MonHocs.Add(mon);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(MonHoc mon)
    {
        _context.MonHocs.Update(mon);
        await _context.SaveChangesAsync();
    }

    public async Task XoaMoiAsync(string maMon)
    {
        var mon = await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMon == maMon);
        if (mon != null)
        {
            mon.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TonTaiAsync(string maMon)
    {
        return await _context.MonHocs.AnyAsync(m => m.MaMon == maMon && !m.IsDeleted);
    }

    public async Task<bool> CoLopHocPhanAsync(string maMon)
    {
        return await _context.LopHocPhans.AnyAsync(l => l.MaMon == maMon && !l.IsDeleted);
    }

    public async Task<int> DemSoLhpDangMoAsync(string maMon, string? maHocKy)
    {
        if (string.IsNullOrEmpty(maHocKy))
        {
            var activeHocKy = await _context.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.DangMo);
            if (activeHocKy == null) return 0;
            maHocKy = activeHocKy.MaHocKy;
        }

        return await _context.LopHocPhans.CountAsync(l => l.MaMon == maMon && l.MaHocKy == maHocKy && !l.IsDeleted);
    }
}
