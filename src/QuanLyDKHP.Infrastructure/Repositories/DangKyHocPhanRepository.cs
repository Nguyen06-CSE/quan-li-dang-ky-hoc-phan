using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class DangKyHocPhanRepository : IDangKyHocPhanRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public DangKyHocPhanRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<DangKyHocPhan>> LayTheoMaSVVaMaHocKyAsync(string maSV, string maHocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.DangKyHocPhans
            .Include(dk => dk.SinhVien)
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.HocKy)
            .Where(dk => dk.MaSV == maSV && dk.LopHocPhan.MaHocKy == maHocKy)
            .OrderBy(dk => dk.LopHocPhan.MaLHP)
            .ToListAsync();
    }

    public async Task<DangKyHocPhan?> LayTheoMaSVVaMaLHPAsync(string maSV, string maLHP)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.DangKyHocPhans
            .Include(dk => dk.SinhVien)
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.HocKy)
            .FirstOrDefaultAsync(dk => dk.MaSV == maSV && dk.MaLHP == maLHP);
    }

    public async Task<DangKyHocPhan?> LayDangKyCungMonDangHocAsync(string maSV, string maMon, string maHocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.DangKyHocPhans
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .FirstOrDefaultAsync(dk => dk.MaSV == maSV && 
                                       dk.LopHocPhan.MaMon == maMon && 
                                       dk.LopHocPhan.MaHocKy == maHocKy && 
                                       dk.TrangThai == "DangHoc");
    }

    public async Task<int> DemSiSoDangKyAsync(string maLHP)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.DangKyHocPhans
            .CountAsync(dk => dk.MaLHP == maLHP && dk.TrangThai == "DangHoc");
    }

    public async Task ThemAsync(DangKyHocPhan dk)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.DangKyHocPhans.Add(dk);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(DangKyHocPhan dk)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.DangKyHocPhans.Update(dk);
        await _context.SaveChangesAsync();
    }
}
