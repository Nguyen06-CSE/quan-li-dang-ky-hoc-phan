using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class HocKyRepository : IHocKyRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public HocKyRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<HocKy>> LayTatCaAsync()
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.HocKys
            .OrderByDescending(hk => hk.MaHocKy)
            .ToListAsync();
    }

    public async Task<HocKy?> GetByIdAsync(string maHocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.HocKys.FirstOrDefaultAsync(hk => hk.MaHocKy == maHocKy);
    }

    public async Task ThemAsync(HocKy hocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.HocKys.Add(hocKy);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(HocKy hocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.HocKys.Update(hocKy);
        await _context.SaveChangesAsync();
    }

    public async Task DatHocKyHienHanhAsync(string maHocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var tatCaHocKy = await _context.HocKys.ToListAsync();
        foreach (var hk in tatCaHocKy)
        {
            hk.DangMo = (hk.MaHocKy == maHocKy);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TonTaiAsync(string maHocKy)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.HocKys.AnyAsync(hk => hk.MaHocKy == maHocKy);
    }
}
