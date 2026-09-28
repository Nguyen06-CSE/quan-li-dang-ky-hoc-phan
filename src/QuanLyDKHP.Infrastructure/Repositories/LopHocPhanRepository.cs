using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class LopHocPhanRepository : ILopHocPhanRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public LopHocPhanRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<LopHocPhan>> LayTheoHocKyAsync(string maHocKy, string? tuKhoa, string? maMon)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        var query = _context.LopHocPhans
            .Include(l => l.MonHoc)
            .Include(l => l.HocKy)
            .Where(l => l.MaHocKy == maHocKy && !l.IsDeleted);

        if (!string.IsNullOrWhiteSpace(maMon))
        {
            query = query.Where(l => l.MaMon == maMon);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var keyword = tuKhoa.Trim().ToLower();
            query = query.Where(l => l.MaLHP.ToLower().Contains(keyword) || 
                                     l.MonHoc.TenMon.ToLower().Contains(keyword) || 
                                     l.MaMon.ToLower().Contains(keyword));
        }

        return await query.OrderBy(l => l.MaLHP).ToListAsync();
    }

    public async Task<LopHocPhan?> GetByIdAsync(string maLHP)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.LopHocPhans
            .Include(l => l.MonHoc)
            .Include(l => l.HocKy)
            .FirstOrDefaultAsync(l => l.MaLHP == maLHP && !l.IsDeleted);
    }

    public async Task<int> DemSiSoDangKyAsync(string maLHP)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.DangKyHocPhans
            .CountAsync(dk => dk.MaLHP == maLHP && dk.TrangThai == "DangHoc");
    }

    public async Task ThemAsync(LopHocPhan lhp)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.LopHocPhans.Add(lhp);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(LopHocPhan lhp)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.LopHocPhans.Update(lhp);
        await _context.SaveChangesAsync();
    }

    public async Task XoaAsync(string maLHP)
    {
        // Không gọi GetByIdAsync ở đây vì nó sẽ dùng 1 DbContext KHÁC (tạo rồi hủy riêng) ->
        // entity trả về sẽ "detached" khỏi context của SaveChangesAsync bên dưới, khiến
        // việc set IsDeleted không được lưu xuống DB. Phải tự truy vấn bằng CHÍNH context
        // sẽ dùng để lưu thay đổi.
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var lhp = await _context.LopHocPhans.FirstOrDefaultAsync(l => l.MaLHP == maLHP && !l.IsDeleted);
        if (lhp != null)
        {
            lhp.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TonTaiAsync(string maLHP)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.LopHocPhans.AnyAsync(l => l.MaLHP == maLHP && !l.IsDeleted);
    }

    public async Task<bool> CoDangKyHocPhanAsync(string maLHP)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.DangKyHocPhans.AnyAsync(dk => dk.MaLHP == maLHP);
    }
}
