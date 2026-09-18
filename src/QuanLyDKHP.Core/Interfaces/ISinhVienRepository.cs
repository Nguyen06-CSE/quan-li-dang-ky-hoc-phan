using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface ISinhVienRepository
{
    Task<PagedResult<SinhVienDto>> TimKiemAsync(string? tuKhoa, string? lop, string? khoaHoc, int page, int pageSize);
    Task<List<SinhVien>> LayDanhSachAsync(string? tuKhoa = null, string? lop = null, string? khoaHoc = null);
    Task<List<string>> GetDanhSachLopSinhHoatAsync();
    Task<List<string>> GetDanhSachKhoaHocAsync();
    Task<SinhVien?> GetByIdAsync(string maSV);
    Task ThemAsync(SinhVien sv);
    Task CapNhatAsync(SinhVien sv);
    Task XoaMoiAsync(string maSV);
    Task<bool> TonTaiAsync(string maSV);
    Task<bool> CoDangKyHocPhanDangHocAsync(string maSV);
}
