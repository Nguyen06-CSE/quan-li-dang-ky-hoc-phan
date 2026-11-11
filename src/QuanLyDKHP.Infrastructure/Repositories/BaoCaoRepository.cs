using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.Infrastructure.Repositories;

public class BaoCaoRepository : IBaoCaoRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public BaoCaoRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private static async Task<Dictionary<string, string>> GetGiangVienDictAsync(AppDbContext context)
    {
        var gvList = await context.NguoiDungs.AsNoTracking()
            .Where(u => u.Role == "GiangVien" && !u.IsDeleted)
            .ToListAsync();

        var dict = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var gv in gvList)
        {
            if (!string.IsNullOrWhiteSpace(gv.TenDangNhap))
                dict[gv.TenDangNhap] = gv.HoTen;
            if (!string.IsNullOrWhiteSpace(gv.MaGV))
                dict[gv.MaGV] = gv.HoTen;
        }
        return dict;
    }

    public async Task<List<SinhVienTheoMonDto>> LayDsSvTheoMonAsync(string maMon, string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(maHocKy))
            return new List<SinhVienTheoMonDto>();

        await using var _context = await _contextFactory.CreateDbContextAsync();
        var gvDict = await GetGiangVienDictAsync(_context);

        var rawList = await (from dk in _context.DangKyHocPhans.AsNoTracking()
                             join lhp in _context.LopHocPhans.AsNoTracking() on dk.MaLHP equals lhp.MaLHP
                             join sv in _context.SinhViens.AsNoTracking() on dk.MaSV equals sv.MaSV
                             where lhp.MaMon == maMon 
                                   && lhp.MaHocKy == maHocKy
                                   && !lhp.IsDeleted 
                                   && !sv.IsDeleted 
                                   && dk.TrangThai == "DangHoc"
                             orderby dk.MaLHP, sv.MaSV
                             select new
                             {
                                 sv.MaSV,
                                 sv.HoTen,
                                 sv.LopSinhHoat,
                                 dk.MaLHP,
                                 lhp.MaGV,
                                 dk.TrangThai
                             }).ToListAsync();

        return rawList.Select(x =>
        {
            string tenGv = "Chưa phân công";
            if (!string.IsNullOrWhiteSpace(x.MaGV))
            {
                tenGv = gvDict.TryGetValue(x.MaGV, out var name) ? name : x.MaGV;
            }

            return new SinhVienTheoMonDto
            {
                MaSV = x.MaSV,
                HoTen = x.HoTen,
                LopSinhHoat = x.LopSinhHoat,
                MaLHP = x.MaLHP,
                TenGiangVien = tenGv,
                TrangThai = x.TrangThai
            };
        }).ToList();
    }

    public async Task<List<DanhSachThiDto>> LayDsThiAsync(string maMon, string maHocKy, string? maLHP)
    {
        if (string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(maHocKy))
            return new List<DanhSachThiDto>();

        await using var _context = await _contextFactory.CreateDbContextAsync();
        var gvDict = await GetGiangVienDictAsync(_context);

        var query = from dk in _context.DangKyHocPhans.AsNoTracking()
                    join lhp in _context.LopHocPhans.AsNoTracking() on dk.MaLHP equals lhp.MaLHP
                    join sv in _context.SinhViens.AsNoTracking() on dk.MaSV equals sv.MaSV
                    where lhp.MaMon == maMon 
                          && lhp.MaHocKy == maHocKy 
                          && dk.TrangThai == "DangHoc"
                          && !lhp.IsDeleted 
                          && !sv.IsDeleted
                    select new { dk, lhp, sv };

        if (!string.IsNullOrWhiteSpace(maLHP))
        {
            query = query.Where(x => x.dk.MaLHP == maLHP);
        }

        var list = await query
            .OrderBy(x => x.dk.MaLHP)
            .ThenBy(x => x.sv.MaSV)
            .Select(x => new
            {
                x.sv.MaSV,
                x.sv.HoTen,
                x.sv.LopSinhHoat,
                x.dk.MaLHP,
                x.lhp.MaGV
            })
            .ToListAsync();

        int stt = 1;
        return list.Select(x =>
        {
            string tenGv = "Chưa phân công";
            if (!string.IsNullOrWhiteSpace(x.MaGV))
            {
                tenGv = gvDict.TryGetValue(x.MaGV, out var name) ? name : x.MaGV;
            }

            return new DanhSachThiDto
            {
                Stt = stt++,
                MaSV = x.MaSV,
                HoTen = x.HoTen,
                LopSinhHoat = x.LopSinhHoat,
                MaLHP = x.MaLHP,
                TenGiangVien = tenGv
            };
        }).ToList();
    }

    public async Task<List<ThongKeMonDto>> ThongKeSoSvTheoMonAsync(string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maHocKy))
            return new List<ThongKeMonDto>();

        await using var _context = await _contextFactory.CreateDbContextAsync();

        var lhps = await _context.LopHocPhans.AsNoTracking()
            .Where(l => l.MaHocKy == maHocKy && !l.IsDeleted && (l.MonHoc == null || !l.MonHoc.IsDeleted))
            .Include(l => l.MonHoc)
            .Include(l => l.DangKyHocPhans.Where(dk => dk.TrangThai == "DangHoc"))
            .ToListAsync();

        var grouped = lhps
            .GroupBy(l => l.MaMon)
            .Select(g =>
            {
                var first = g.First();
                return new ThongKeMonDto
                {
                    MaMon = g.Key,
                    TenMon = first.MonHoc?.TenMon ?? g.Key,
                    SoLhpDangMo = g.Count(),
                    SoSinhVien = g.SelectMany(l => l.DangKyHocPhans)
                                  .Where(dk => dk.TrangThai == "DangHoc")
                                  .Select(dk => dk.MaSV)
                                  .Distinct()
                                  .Count()
                };
            })
            .OrderByDescending(x => x.SoSinhVien)
            .ThenBy(x => x.TenMon)
            .ToList();

        return grouped;
    }

    public async Task<PhieuDangKyDto?> LayPhieuDangKyAsync(string maSV, string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maSV) || string.IsNullOrWhiteSpace(maHocKy))
            return null;

        await using var _context = await _contextFactory.CreateDbContextAsync();

        var sv = await _context.SinhViens.AsNoTracking()
            .FirstOrDefaultAsync(s => s.MaSV == maSV && !s.IsDeleted);
        if (sv == null) return null;

        var hk = await _context.HocKys.AsNoTracking()
            .FirstOrDefaultAsync(h => h.MaHocKy == maHocKy);

        var gvDict = await GetGiangVienDictAsync(_context);

        var dks = await _context.DangKyHocPhans.AsNoTracking()
            .Where(dk => dk.MaSV == maSV 
                         && dk.LopHocPhan.MaHocKy == maHocKy 
                         && dk.TrangThai == "DangHoc"
                         && !dk.LopHocPhan.IsDeleted
                         && (dk.LopHocPhan.MonHoc == null || !dk.LopHocPhan.MonHoc.IsDeleted))
            .Include(dk => dk.LopHocPhan)
                .ThenInclude(lhp => lhp.MonHoc)
            .OrderBy(dk => dk.LopHocPhan.MaMon)
            .ToListAsync();

        var result = new PhieuDangKyDto
        {
            MaSV = sv.MaSV,
            HoTen = sv.HoTen,
            LopSinhHoat = sv.LopSinhHoat,
            KhoaHoc = sv.KhoaHoc,
            MaHocKy = maHocKy,
            TenHocKy = hk?.TenHocKy ?? maHocKy,
            NgayIn = System.DateTime.Now
        };

        int stt = 1;
        int tongTinChi = 0;

        foreach (var dk in dks)
        {
            int tc = dk.LopHocPhan.MonHoc != null
                ? (dk.LopHocPhan.MonHoc.SoTinChiLT + dk.LopHocPhan.MonHoc.SoTinChiTH)
                : 0;
            tongTinChi += tc;

            string tenGv = "Chưa phân công";
            if (!string.IsNullOrWhiteSpace(dk.LopHocPhan.MaGV))
            {
                tenGv = gvDict.TryGetValue(dk.LopHocPhan.MaGV, out var name) ? name : dk.LopHocPhan.MaGV;
            }

            result.DanhSachMon.Add(new PhieuDangKyMonDto
            {
                Stt = stt++,
                MaMon = dk.LopHocPhan.MaMon,
                TenMon = dk.LopHocPhan.MonHoc?.TenMon ?? dk.LopHocPhan.MaMon,
                SoTinChi = tc,
                MaLHP = dk.MaLHP,
                TenGiangVien = tenGv,
                DiemSo = dk.DiemSo,
                DiemChu = dk.DiemChu
            });
        }

        result.TongSoTinChi = tongTinChi;
        return result;
    }
}
