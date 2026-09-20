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
    private readonly AppDbContext _context;

    public BaoCaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SinhVienTheoMonDto>> LayDsSvTheoMonAsync(string maMon, string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(maHocKy))
            return new List<SinhVienTheoMonDto>();

        var query = from dk in _context.DangKyHocPhans.AsNoTracking()
                    join lhp in _context.LopHocPhans.AsNoTracking() on dk.MaLHP equals lhp.MaLHP
                    join sv in _context.SinhViens.AsNoTracking() on dk.MaSV equals sv.MaSV
                    where lhp.MaMon == maMon && lhp.MaHocKy == maHocKy
                    orderby dk.MaLHP, sv.MaSV
                    select new SinhVienTheoMonDto
                    {
                        MaSV = sv.MaSV,
                        HoTen = sv.HoTen,
                        LopSinhHoat = sv.LopSinhHoat,
                        MaLHP = dk.MaLHP,
                        TenGiangVien = lhp.MaGV, // Mã/Tên GV
                        TrangThai = dk.TrangThai
                    };

        return await query.ToListAsync();
    }

    public async Task<List<DanhSachThiDto>> LayDsThiAsync(string maMon, string maHocKy, string? maLHP)
    {
        if (string.IsNullOrWhiteSpace(maMon) || string.IsNullOrWhiteSpace(maHocKy))
            return new List<DanhSachThiDto>();

        var query = from dk in _context.DangKyHocPhans.AsNoTracking()
                    join lhp in _context.LopHocPhans.AsNoTracking() on dk.MaLHP equals lhp.MaLHP
                    join sv in _context.SinhViens.AsNoTracking() on dk.MaSV equals sv.MaSV
                    where lhp.MaMon == maMon && lhp.MaHocKy == maHocKy && dk.TrangThai == "DangHoc"
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
                x.dk.MaLHP
            })
            .ToListAsync();

        int stt = 1;
        return list.Select(x => new DanhSachThiDto
        {
            Stt = stt++,
            MaSV = x.MaSV,
            HoTen = x.HoTen,
            LopSinhHoat = x.LopSinhHoat,
            MaLHP = x.MaLHP
        }).ToList();
    }

    public async Task<List<ThongKeMonDto>> ThongKeSoSvTheoMonAsync(string maHocKy)
    {
        if (string.IsNullOrWhiteSpace(maHocKy))
            return new List<ThongKeMonDto>();

        // Lấy tất cả môn học có LHP trong học kỳ này hoặc tất cả môn học
        var lhps = await _context.LopHocPhans.AsNoTracking()
            .Where(l => l.MaHocKy == maHocKy)
            .Include(l => l.MonHoc)
            .Include(l => l.DangKyHocPhans.Where(dk => dk.TrangThai == "DangHoc"))
            .ToListAsync();

        var grouped = lhps
            .GroupBy(l => l.MonHoc)
            .Select(g => new ThongKeMonDto
            {
                MaMon = g.Key.MaMon,
                TenMon = g.Key.TenMon,
                SoLhpDangMo = g.Count(),
                // Đếm distinct MaSV qua các LHP của môn đó
                SoSinhVien = g.SelectMany(l => l.DangKyHocPhans).Select(dk => dk.MaSV).Distinct().Count()
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

        var sv = await _context.SinhViens.AsNoTracking()
            .FirstOrDefaultAsync(s => s.MaSV == maSV);
        if (sv == null) return null;

        var hk = await _context.HocKys.AsNoTracking()
            .FirstOrDefaultAsync(h => h.MaHocKy == maHocKy);

        var dks = await _context.DangKyHocPhans.AsNoTracking()
            .Where(dk => dk.MaSV == maSV && dk.LopHocPhan.MaHocKy == maHocKy && dk.TrangThai == "DangHoc")
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
            int tc = (dk.LopHocPhan.MonHoc.SoTinChiLT + dk.LopHocPhan.MonHoc.SoTinChiTH);
            tongTinChi += tc;

            result.DanhSachMon.Add(new PhieuDangKyMonDto
            {
                Stt = stt++,
                MaMon = dk.LopHocPhan.MaMon,
                TenMon = dk.LopHocPhan.MonHoc.TenMon,
                SoTinChi = tc,
                MaLHP = dk.MaLHP,
                TenGiangVien = dk.LopHocPhan.MaGV,
                DiemSo = dk.DiemSo,
                DiemChu = dk.DiemChu
            });
        }

        result.TongSoTinChi = tongTinChi;
        return result;
    }
}
