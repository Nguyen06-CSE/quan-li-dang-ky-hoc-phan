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
    private readonly AppDbContext _context;

    public LopHocPhanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LopHocPhan>> LayTheoHocKyAsync(string maHocKy, string? tuKhoa, string? maMon)
    {
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
        return await _context.LopHocPhans
            .Include(l => l.MonHoc)
            .Include(l => l.HocKy)
            .FirstOrDefaultAsync(l => l.MaLHP == maLHP && !l.IsDeleted);
    }

    public async Task<int> DemSiSoDangKyAsync(string maLHP)
    {
        return await _context.DangKyHocPhans
            .CountAsync(dk => dk.MaLHP == maLHP && dk.TrangThai == "DangHoc");
    }

    public async Task ThemAsync(LopHocPhan lhp)
    {
        _context.LopHocPhans.Add(lhp);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(LopHocPhan lhp)
    {
        _context.LopHocPhans.Update(lhp);
        await _context.SaveChangesAsync();
    }

    public async Task XoaAsync(string maLHP)
    {
        var lhp = await GetByIdAsync(maLHP);
        if (lhp != null)
        {
            lhp.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TonTaiAsync(string maLHP)
    {
        return await _context.LopHocPhans.AnyAsync(l => l.MaLHP == maLHP && !l.IsDeleted);
    }

    public async Task<bool> CoDangKyHocPhanAsync(string maLHP)
    {
        return await _context.DangKyHocPhans.AnyAsync(dk => dk.MaLHP == maLHP);
    }
}
