using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IDangKyHocPhanService
{
    Task<DangKyResult> DangKyAsync(string maSV, string maLHP, bool boQuaCanhBaoTrungMon = false);
    Task HuyDangKyAsync(string maSV, string maLHP);
    Task<List<DangKyHocPhan>> LayDanhSachDangKyAsync(string maSV, string maHocKy);
    Task<int> TinhTongTinChiHienTaiAsync(string maSV, string maHocKy);
}
