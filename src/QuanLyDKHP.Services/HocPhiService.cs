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
        // TODO: Giai đoạn sau hỗ trợ "khóa sổ" học kỳ đã qua để giữ nguyên đơn giá lịch sử.
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
        var result = new List<HocPhiTongHopDto>();
        foreach (var maSV in dsMaSV)
        {
            var chiTiet = await TinhHocPhiSinhVienAsync(maSV, maHocKy);
            result.Add(new HocPhiTongHopDto
            {
                MaSV = chiTiet.MaSV,
                HoTen = chiTiet.HoTen,
                LopSinhHoat = chiTiet.LopSinhHoat,
                TongSoTinChi = chiTiet.TongSoTinChi,
                TongHocPhi = chiTiet.TongHocPhi,
                DaDong = chiTiet.DaDong
            });
        }
        return result;
    }
}
