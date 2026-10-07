// src/QuanLyDKHP.Services/HocPhiService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

public class HocPhiService : IHocPhiService
{
    private readonly IDangKyHocPhanRepository _dangKyRepo;
    private readonly ICauHinhService _cauHinhService;
    private readonly ISinhVienRepository _sinhVienRepo;
    private readonly IHocKyRepository _hocKyRepo;

    public HocPhiService(
        IDangKyHocPhanRepository dangKyRepo,
        ICauHinhService cauHinhService,
        ISinhVienRepository sinhVienRepo,
        IHocKyRepository hocKyRepo)
    {
        _dangKyRepo = dangKyRepo;
        _cauHinhService = cauHinhService;
        _sinhVienRepo = sinhVienRepo;
        _hocKyRepo = hocKyRepo;
    }

    public async Task TinhLaiHocPhiAsync(string maSV, string maHocKy)
    {
        decimal donGiaLT = await _cauHinhService.GetDecimal("DonGiaTinChiLT");
        decimal donGiaTH = await _cauHinhService.GetDecimal("DonGiaTinChiTH");

        var danhSachDangKy = await _dangKyRepo.LayTheoMaSVVaMaHocKyAsync(maSV, maHocKy);

        foreach (var dk in danhSachDangKy)
        {
            if (dk.TrangThai == "DangHoc" && dk.LopHocPhan?.MonHoc != null)
            {
                var mon = dk.LopHocPhan.MonHoc;
                decimal hocPhi = (mon.SoTinChiLT * donGiaLT) + (mon.SoTinChiTH * donGiaTH);
                dk.SoTienPhaiDong = hocPhi;
            }
            else
            {
                dk.SoTienPhaiDong = 0;
            }
            await _dangKyRepo.CapNhatAsync(dk);
        }
    }

    public async Task TinhLaiHocPhiHangLoatAsync(IEnumerable<string> dsMaSV, string maHocKy)
    {
        var danhSach = dsMaSV.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
        if (danhSach.Count == 0) return;

        decimal donGiaLT = await _cauHinhService.GetDecimal("DonGiaTinChiLT");
        decimal donGiaTH = await _cauHinhService.GetDecimal("DonGiaTinChiTH");

        const int kichThuocLo = 500;
        for (int i = 0; i < danhSach.Count; i += kichThuocLo)
        {
            var lo = danhSach.Skip(i).Take(kichThuocLo).ToList();
            var dangKys = await _dangKyRepo.LayTheoDanhSachMaSVVaMaHocKyAsync(lo, maHocKy);

            var capNhat = new List<(Guid Id, decimal SoTien)>(dangKys.Count);
            foreach (var dk in dangKys)
            {
                decimal soTien = 0;
                if (dk.TrangThai == "DangHoc" && dk.LopHocPhan?.MonHoc != null)
                {
                    var mon = dk.LopHocPhan.MonHoc;
                    soTien = (mon.SoTinChiLT * donGiaLT) + (mon.SoTinChiTH * donGiaTH);
                }
                capNhat.Add((dk.Id, soTien));
            }

            await _dangKyRepo.CapNhatSoTienPhaiDongAsync(capNhat);
        }
    }

    public async Task<HocPhiChiTietDto> TinhHocPhiSinhVienAsync(string maSV, string maHocKy)
    {
        decimal donGiaLT = await _cauHinhService.GetDecimal("DonGiaTinChiLT");
        decimal donGiaTH = await _cauHinhService.GetDecimal("DonGiaTinChiTH");

        var sv = await _sinhVienRepo.GetByIdAsync(maSV);
        var hk = await _hocKyRepo.GetByIdAsync(maHocKy);
        var danhSachDangKy = await _dangKyRepo.LayTheoMaSVVaMaHocKyAsync(maSV, maHocKy);

        var chiTiet = new HocPhiChiTietDto
        {
            MaSV = maSV,
            HoTen = sv?.HoTen ?? string.Empty,
            LopSinhHoat = sv?.LopSinhHoat,
            MaHocKy = maHocKy,
            TenHocKy = hk?.TenHocKy ?? maHocKy
        };

        foreach (var dk in danhSachDangKy.Where(d => d.TrangThai == "DangHoc"))
        {
            if (dk.LopHocPhan?.MonHoc != null)
            {
                var mon = dk.LopHocPhan.MonHoc;
                decimal thanhTien = (mon.SoTinChiLT * donGiaLT) + (mon.SoTinChiTH * donGiaTH);
                chiTiet.DanhSachMon.Add(new HocPhiChiTietMonDto
                {
                    MaLHP = dk.MaLHP,
                    MaMon = mon.MaMon,
                    TenMon = mon.TenMon,
                    SoTinChiLT = mon.SoTinChiLT,
                    SoTinChiTH = mon.SoTinChiTH,
                    DonGiaLT = donGiaLT,
                    DonGiaTH = donGiaTH,
                    ThanhTien = thanhTien
                });

                chiTiet.TongSoTinChi += (mon.SoTinChiLT + mon.SoTinChiTH);
                chiTiet.TongHocPhi += thanhTien;
                chiTiet.DaDong += dk.SoTienDaDong;
            }
        }

        return chiTiet;
    }

    public async Task<List<HocPhiTongHopDto>> TinhHocPhiTheoDanhSachAsync(IEnumerable<string> dsMaSV, string maHocKy)
    {
        var listMaSV = dsMaSV.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
        if (listMaSV.Count == 0) return [];

        // 1. Lấy đơn giá 1 lần duy nhất cho toàn bộ danh sách
        decimal donGiaLT = await _cauHinhService.GetDecimal("DonGiaTinChiLT");
        decimal donGiaTH = await _cauHinhService.GetDecimal("DonGiaTinChiTH");

        // 2. Kéo toàn bộ đăng ký trong 1 lượt query gộp duy nhất qua Repository
        var allDangKy = await _dangKyRepo.LayTheoDanhSachMaSVVaMaHocKyAsync(listMaSV, maHocKy);

        // 3. Gom nhóm đăng ký theo từng sinh viên
        var dangKyTheoSV = allDangKy
            .Where(dk => dk.TrangThai == "DangHoc")
            .GroupBy(dk => dk.MaSV)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4. Lấy thông tin sinh viên (ưu tiên từ navigation property nếu repo đã include)
        var mapSinhVien = new Dictionary<string, (string HoTen, string LopSinhHoat)>();
        foreach (var dk in allDangKy)
        {
            if (dk.SinhVien != null && !mapSinhVien.ContainsKey(dk.MaSV))
            {
                mapSinhVien[dk.MaSV] = (dk.SinhVien.HoTen, dk.SinhVien.LopSinhHoat ?? string.Empty);
            }
        }

        // Với những sinh viên chưa có thông tin (chưa đăng ký môn nào), tải thông tin song song qua Task.WhenAll
        var svChuaCoInfo = listMaSV.Where(m => !mapSinhVien.ContainsKey(m)).ToList();
        if (svChuaCoInfo.Count > 0)
        {
            var tasks = svChuaCoInfo.Select(async ma =>
            {
                var sv = await _sinhVienRepo.GetByIdAsync(ma);
                return (MaSV: ma, HoTen: sv?.HoTen ?? ma, Lop: sv?.LopSinhHoat ?? string.Empty);
            });
            var ketQuaSVs = await Task.WhenAll(tasks);
            foreach (var item in ketQuaSVs)
            {
                mapSinhVien[item.MaSV] = (item.HoTen, item.Lop);
            }
        }

        // 5. Tính toán tổng hợp trên bộ nhớ RAM
        var result = new List<HocPhiTongHopDto>(listMaSV.Count);
        foreach (var maSV in listMaSV)
        {
            mapSinhVien.TryGetValue(maSV, out var svInfo);
            dangKyTheoSV.TryGetValue(maSV, out var dks);

            int tongTinChi = 0;
            decimal tongHocPhi = 0;
            decimal daDong = 0;

            if (dks != null)
            {
                foreach (var dk in dks)
                {
                    if (dk.LopHocPhan?.MonHoc != null)
                    {
                        var mon = dk.LopHocPhan.MonHoc;
                        tongTinChi += (mon.SoTinChiLT + mon.SoTinChiTH);
                        tongHocPhi += (mon.SoTinChiLT * donGiaLT) + (mon.SoTinChiTH * donGiaTH);
                    }
                    else if (dk.SoTienPhaiDong.HasValue)
                    {
                        tongHocPhi += dk.SoTienPhaiDong.Value;
                    }

                    daDong += dk.SoTienDaDong;
                }
            }

            result.Add(new HocPhiTongHopDto
            {
                MaSV = maSV,
                HoTen = svInfo.HoTen ?? maSV,
                LopSinhHoat = svInfo.LopSinhHoat ?? string.Empty,
                TongSoTinChi = tongTinChi,
                TongHocPhi = tongHocPhi,
                DaDong = daDong
            });
        }

        return result;
    }
}