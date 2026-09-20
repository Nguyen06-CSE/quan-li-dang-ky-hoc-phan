using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface IPdfExportService
{
    Task<byte[]> XuatPhieuHocPhiPdfAsync(HocPhiChiTietDto chiTiet);
    Task<byte[]> XuatDanhSachThiPdfAsync(List<DanhSachThiDto> danhSach, string tenMon, string? maLHP, string tenHocKy);
    Task<byte[]> XuatPhieuKetQuaDKHPPdfAsync(PhieuDangKyDto phieu);
}
