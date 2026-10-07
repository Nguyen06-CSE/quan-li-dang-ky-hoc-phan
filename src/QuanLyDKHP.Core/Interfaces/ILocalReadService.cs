using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface ILocalReadService
{
    Task<List<SinhVienDto>> GetSinhViensLocalAsync(string? tuKhoa = null, string? lop = null, string? khoaHoc = null);
    Task<List<MonHocDto>> GetMonHocsLocalAsync(string? tuKhoa = null);
    Task<List<LopHocPhan>> GetLopHocPhansLocalAsync(string? maHocKy = null, string? tuKhoa = null, string? maMon = null);
    Task<List<DangKyHocPhan>> GetDangKyLocalAsync(string maSV, string maHocKy);
    Task<List<DangKyHocPhan>> GetDangKyNhieuSvLocalAsync(IEnumerable<string> dsMaSV, string maHocKy);
    Task<int> DemSiSoDangKyLocalAsync(string maLHP);
    Task<Dictionary<string, int>> DemSiSoDangKyBulkLocalAsync(IEnumerable<string> dsMaLHP);
    Task<List<SinhVienTheoMonDto>> GetDsSinhVienTheoMonLocalAsync(string maMon, string maHocKy);
    Task<DashboardStatsDto> GetDashboardStatsLocalAsync(string? maHocKy);
    Task<List<HocPhiTongHopDto>> GetHocPhiTheoDanhSachLocalAsync(IEnumerable<string> dsMaSV, string maHocKy);
}
