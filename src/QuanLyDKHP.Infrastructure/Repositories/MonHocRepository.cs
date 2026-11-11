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
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public MonHocRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<MonHoc>> LayDanhSachAsync(string? tuKhoa)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

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
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMon == maMon && !m.IsDeleted);
    }

    public async Task ThemAsync(MonHoc mon)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        // Tái tạo mã sau soft-delete: kiểm tra xem Mã Môn đã từng tồn tại trong DB chưa
        var existing = await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMon == mon.MaMon);
        if (existing != null)
        {
            if (!existing.IsDeleted)
            {
                throw new InvalidOperationException($"Mã môn học '{mon.MaMon}' đã tồn tại trong hệ thống.");
            }

            // Tái kích hoạt bản ghi đã xóa mềm và cập nhật thông tin mới
            existing.TenMon = mon.TenMon;
            existing.SoTinChiLT = mon.SoTinChiLT;
            existing.SoTinChiTH = mon.SoTinChiTH;
            existing.BacDaoTao = mon.BacDaoTao;
            existing.IsDeleted = false;
            existing.NgayCapNhat = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return;
        }

        mon.NgayTao = DateTime.UtcNow;
        mon.IsDeleted = false;
        _context.MonHocs.Add(mon);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(MonHoc mon)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.MonHocs.Update(mon);
        await _context.SaveChangesAsync();
    }

    public async Task XoaMoiAsync(string maMon)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var mon = await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMon == maMon);
        if (mon != null)
        {
            mon.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TonTaiAsync(string maMon)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.MonHocs.AnyAsync(m => m.MaMon == maMon && !m.IsDeleted);
    }

    public async Task<bool> CoLopHocPhanAsync(string maMon)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.LopHocPhans.AnyAsync(l => l.MaMon == maMon && !l.IsDeleted);
    }

    public async Task<int> DemSoLhpDangMoAsync(string maMon, string? maHocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        if (string.IsNullOrEmpty(maHocKy))
        {
            var activeHocKy = await _context.HocKys.AsNoTracking().FirstOrDefaultAsync(h => h.DangMo);
            if (activeHocKy == null) return 0;
            maHocKy = activeHocKy.MaHocKy;
        }

        return await _context.LopHocPhans.CountAsync(l => l.MaMon == maMon && l.MaHocKy == maHocKy && !l.IsDeleted);
    }
}
