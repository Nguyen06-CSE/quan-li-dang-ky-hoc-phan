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
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public NguoiDungRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<NguoiDung>> LayTatCaAsync()
    {
        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        return await _dbContext.NguoiDungs
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Id)
            .ToListAsync();
    }

    public async Task<NguoiDung?> GetByIdAsync(int id)
    {
        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        return await _dbContext.NguoiDungs
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    public async Task<NguoiDung?> GetByTenDangNhapAsync(string tenDangNhap)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap)) return null;

        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        return await _dbContext.NguoiDungs
            .FirstOrDefaultAsync(u => u.TenDangNhap.ToLower() == tenDangNhap.ToLower() && !u.IsDeleted);
    }

    public async Task<NguoiDung?> GetByMaGVAsync(string maGV)
    {
        if (string.IsNullOrWhiteSpace(maGV)) return null;

        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        return await _dbContext.NguoiDungs
            .FirstOrDefaultAsync(u => u.MaGV != null && u.MaGV.ToLower() == maGV.ToLower() && !u.IsDeleted);
    }

    public async Task<List<NguoiDung>> LayGiangVienAsync()
    {
        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        string roleGiangVien = Core.Enums.UserRole.GiangVien.ToString();
        return await _dbContext.NguoiDungs
            .Where(u => u.Role == roleGiangVien && !u.IsDeleted && u.TrangThai == "HoatDong")
            .OrderBy(u => u.HoTen)
            .ToListAsync();
    }

    public async Task ThemAsync(NguoiDung nd)
    {
        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        
        // Tái tạo mã sau soft-delete: kiểm tra xem tài khoản đã từng tồn tại chưa
        var existing = await _dbContext.NguoiDungs
            .FirstOrDefaultAsync(u => u.TenDangNhap.ToLower() == nd.TenDangNhap.ToLower());

        if (existing != null)
        {
            if (!existing.IsDeleted)
            {
                throw new InvalidOperationException($"Tên đăng nhập '{nd.TenDangNhap}' đã tồn tại trong hệ thống.");
            }

            // Phục hồi bản ghi xóa mềm với thông tin mới
            existing.HoTen = nd.HoTen;
            existing.MatKhauHash = nd.MatKhauHash;
            existing.Role = nd.Role;
            existing.MaGV = nd.MaGV;
            existing.TrangThai = nd.TrangThai;
            existing.IsDeleted = false;
            existing.NgayCapNhat = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return;
        }

        nd.NgayTao = DateTime.UtcNow;
        nd.IsDeleted = false;
        _dbContext.NguoiDungs.Add(nd);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CapNhatAsync(NguoiDung nd)
    {
        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        nd.NgayCapNhat = DateTime.UtcNow;
        _dbContext.NguoiDungs.Update(nd);
        await _dbContext.SaveChangesAsync();
    }

    public async Task XoaAsync(int id)
    {
        await using var _dbContext = await _contextFactory.CreateDbContextAsync();
        var item = await _dbContext.NguoiDungs.FindAsync(id);
        if (item != null)
        {
            item.IsDeleted = true;
            item.NgayCapNhat = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
}
