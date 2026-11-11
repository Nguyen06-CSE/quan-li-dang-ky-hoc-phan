using System;
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
        // Entity truyền vào thường được tải bằng LayTheoMaSVVaMaLHPAsync/LayTheoMaSVVaMaHocKyAsync,
        // 2 hàm đó Include SinhVien + LopHocPhan (->MonHoc, ->HocKy) để tiện hiển thị. Nếu giữ
        // nguyên các navigation đó rồi gọi Update() trên context MỚI (do dùng factory), EF sẽ coi
        // cả SinhVien/LopHocPhan/MonHoc/HocKy là "Modified" (vì khóa chính của chúng khác giá trị
        // mặc định) và ghi đè UPDATE xuống CẢ 4 bảng đó bằng dữ liệu đã tải từ trước — âm thầm xóa
        // mất mọi thay đổi người khác vừa làm trên các bảng đó. Cắt navigation trước khi Update()
        // để chỉ đúng dòng DangKyHocPhan được ghi.
        dk.SinhVien = null!;
        dk.LopHocPhan = null!;

        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.DangKyHocPhans.Update(dk);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DangKyHocPhan>> LayTheoDanhSachMaSVVaMaHocKyAsync(IEnumerable<string> dsMaSV, string maHocKy)
    {
        var ds = dsMaSV.Distinct().ToList();
        if (ds.Count == 0) return new List<DangKyHocPhan>();

        await using var _context = await _contextFactory.CreateDbContextAsync();
        // Chỉ đọc, không cần theo dõi thay đổi -> nhanh hơn và ít RAM hơn
        return await _context.DangKyHocPhans
            .AsNoTracking()
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
            .Where(dk => ds.Contains(dk.MaSV) && dk.LopHocPhan.MaHocKy == maHocKy)
            .ToListAsync();
    }

    public async Task CapNhatSoTienPhaiDongAsync(IEnumerable<(Guid Id, decimal SoTien)> capNhat)
    {
        var danhSach = capNhat.ToList();
        if (danhSach.Count == 0) return;

        await using var _context = await _contextFactory.CreateDbContextAsync();
        foreach (var (id, soTien) in danhSach)
        {
            // Chỉ gắn "khung" entity theo Id và đánh dấu đúng 1 cột -> UPDATE chỉ SoTienPhaiDong,
            // không kéo theo cập nhật lại SinhVien/LopHocPhan/MonHoc như DbSet.Update(graph).
            var stub = new DangKyHocPhan { Id = id, SoTienPhaiDong = soTien };
            _context.DangKyHocPhans.Attach(stub);
            _context.Entry(stub).Property(d => d.SoTienPhaiDong).IsModified = true;
        }
        await _context.SaveChangesAsync();
    }

    public async Task CapNhatSoTienDaDongAsync(IEnumerable<(Guid Id, decimal SoTienDaDong)> capNhat)
    {
        var danhSach = capNhat.ToList();
        if (danhSach.Count == 0) return;

        await using var _context = await _contextFactory.CreateDbContextAsync();
        foreach (var (id, soTien) in danhSach)
        {
            var stub = new DangKyHocPhan { Id = id, SoTienDaDong = soTien };
            _context.DangKyHocPhans.Attach(stub);
            _context.Entry(stub).Property(d => d.SoTienDaDong).IsModified = true;
        }
        await _context.SaveChangesAsync();
    }

    private static readonly System.Threading.SemaphoreSlim _dangKyLock = new(1, 1);

    public async Task<(bool ThanhCong, string? ThongBaoLoi)> LuuDangKyAtomicAsync(DangKyHocPhan dk, bool laTaoMoi, int? siSoToiDa)
    {
        await _dangKyLock.WaitAsync();
        try
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (siSoToiDa.HasValue && siSoToiDa.Value > 0)
                {
                    int siSoHienTai = await _context.DangKyHocPhans
                        .CountAsync(d => d.MaLHP == dk.MaLHP && d.TrangThai == "DangHoc" && d.MaSV != dk.MaSV);

                    if (siSoHienTai >= siSoToiDa.Value)
                    {
                        await transaction.RollbackAsync();
                        return (false, "Lớp học phần đã đủ sĩ số.");
                    }
                }

                dk.SinhVien = null!;
                dk.LopHocPhan = null!;

                if (laTaoMoi)
                {
                    _context.DangKyHocPhans.Add(dk);
                }
                else
                {
                    _context.DangKyHocPhans.Update(dk);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Lỗi ghi nhận đăng ký: {ex.Message}");
            }
        }
        finally
        {
            _dangKyLock.Release();
        }
    }
}
