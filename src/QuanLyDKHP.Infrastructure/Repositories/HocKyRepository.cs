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
    private readonly AppDbContext _context;

    public HocKyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HocKy>> LayTatCaAsync()
    {
        return await _context.HocKys
            .OrderByDescending(hk => hk.MaHocKy)
            .ToListAsync();
    }

    public async Task<HocKy?> GetByIdAsync(string maHocKy)
    {
        return await _context.HocKys.FirstOrDefaultAsync(hk => hk.MaHocKy == maHocKy);
    }

    public async Task ThemAsync(HocKy hocKy)
    {
        _context.HocKys.Add(hocKy);
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatAsync(HocKy hocKy)
    {
        _context.HocKys.Update(hocKy);
        await _context.SaveChangesAsync();
    }

    public async Task DatHocKyHienHanhAsync(string maHocKy)
    {
        var tatCaHocKy = await _context.HocKys.ToListAsync();
        foreach (var hk in tatCaHocKy)
        {
            hk.DangMo = (hk.MaHocKy == maHocKy);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TonTaiAsync(string maHocKy)
    {
        return await _context.HocKys.AnyAsync(hk => hk.MaHocKy == maHocKy);
    }
}
