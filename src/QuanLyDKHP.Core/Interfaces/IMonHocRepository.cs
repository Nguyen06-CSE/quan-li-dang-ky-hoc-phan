using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IMonHocRepository
{
    Task<List<MonHoc>> LayDanhSachAsync(string? tuKhoa);
    Task<MonHoc?> GetByIdAsync(string maMon);
    Task ThemAsync(MonHoc mon);
    Task CapNhatAsync(MonHoc mon);
    Task XoaMoiAsync(string maMon);
    Task<bool> TonTaiAsync(string maMon);
    Task<bool> CoLopHocPhanAsync(string maMon);
    Task<int> DemSoLhpDangMoAsync(string maMon, string? maHocKy);
}
