using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class NguoiDungRepository : INguoiDungRepository
{
    private readonly AppDbContext _dbContext;

    public NguoiDungRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<NguoiDung>> LayTatCaAsync()
    {
        return await _dbContext.NguoiDungs
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Id)
            .ToListAsync();
    }

    public async Task<NguoiDung?> GetByIdAsync(int id)
    {
        return await _dbContext.NguoiDungs
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    public async Task<NguoiDung?> GetByTenDangNhapAsync(string tenDangNhap)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap)) return null;

        return await _dbContext.NguoiDungs
            .FirstOrDefaultAsync(u => u.TenDangNhap.ToLower() == tenDangNhap.ToLower() && !u.IsDeleted);
    }

    public async Task<List<NguoiDung>> LayGiangVienAsync()
    {
        string roleGiangVien = Core.Enums.UserRole.GiangVien.ToString();
        return await _dbContext.NguoiDungs
            .Where(u => u.Role == roleGiangVien && !u.IsDeleted && u.TrangThai == "HoatDong")
            .OrderBy(u => u.HoTen)
            .ToListAsync();
    }

    public async Task ThemAsync(NguoiDung nd)
    {
        nd.NgayTao = DateTime.UtcNow;
        _dbContext.NguoiDungs.Add(nd);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CapNhatAsync(NguoiDung nd)
    {
        nd.NgayCapNhat = DateTime.UtcNow;
        _dbContext.NguoiDungs.Update(nd);
        await _dbContext.SaveChangesAsync();
    }

    public async Task XoaAsync(int id)
    {
        var item = await _dbContext.NguoiDungs.FindAsync(id);
        if (item != null)
        {
            item.IsDeleted = true;
            item.NgayCapNhat = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
}
