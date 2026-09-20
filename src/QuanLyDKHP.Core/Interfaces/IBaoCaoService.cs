using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IBaoCaoService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(string? maHocKy);
    Task<List<HocKy>> GetDanhSachHocKyAsync();
    Task<HocKy?> GetHocKyMacDinhAsync();
    Task<List<GiangVienLhpDto>> GetLhpGiangVienAsync(string maGV, string? maHocKy);

    // Spec 012
    Task<List<SinhVienTheoMonDto>> DsSinhVienTheoMonAsync(string maMon, string maHocKy);
    Task<List<DanhSachThiDto>> DsThiTheoMonAsync(string maMon, string maHocKy, string? maLHP);
    Task<List<ThongKeMonDto>> ThongKeSoLuongTheoMonAsync(string maHocKy);
    Task<PhieuDangKyDto> LayPhieuDangKyAsync(string maSV, string maHocKy);
}
