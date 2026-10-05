using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.Core.Interfaces;

public interface IHocPhiService
{
    Task<HocPhiChiTietDto> TinhHocPhiSinhVienAsync(string maSV, string maHocKy);
    Task TinhLaiHocPhiAsync(string maSV, string maHocKy);

    /// <summary>Tính lại học phí cho nhiều sinh viên cùng lúc (dùng khi import): ít truy vấn, ghi theo lô.</summary>
    Task TinhLaiHocPhiHangLoatAsync(IEnumerable<string> dsMaSV, string maHocKy);
    Task<List<HocPhiTongHopDto>> TinhHocPhiTheoDanhSachAsync(IEnumerable<string> dsMaSV, string maHocKy);
}
