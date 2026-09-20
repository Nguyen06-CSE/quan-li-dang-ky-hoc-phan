using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface IBaoCaoRepository
{
    Task<List<SinhVienTheoMonDto>> LayDsSvTheoMonAsync(string maMon, string maHocKy);
    Task<List<DanhSachThiDto>> LayDsThiAsync(string maMon, string maHocKy, string? maLHP);
    Task<List<ThongKeMonDto>> ThongKeSoSvTheoMonAsync(string maHocKy);
    Task<PhieuDangKyDto?> LayPhieuDangKyAsync(string maSV, string maHocKy);
}
