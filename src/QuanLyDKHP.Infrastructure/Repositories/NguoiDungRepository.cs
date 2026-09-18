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
}
