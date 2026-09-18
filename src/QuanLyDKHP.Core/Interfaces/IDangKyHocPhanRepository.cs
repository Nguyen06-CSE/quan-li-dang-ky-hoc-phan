using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IDangKyHocPhanRepository
{
    Task<List<DangKyHocPhan>> LayTheoMaSVVaMaHocKyAsync(string maSV, string maHocKy);
    Task<DangKyHocPhan?> LayTheoMaSVVaMaLHPAsync(string maSV, string maLHP);
    Task<DangKyHocPhan?> LayDangKyCungMonDangHocAsync(string maSV, string maMon, string maHocKy);
    Task<int> DemSiSoDangKyAsync(string maLHP);
    Task ThemAsync(DangKyHocPhan dk);
    Task CapNhatAsync(DangKyHocPhan dk);
}
