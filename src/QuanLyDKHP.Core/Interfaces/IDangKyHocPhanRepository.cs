using System;
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

    /// <summary>Lấy đăng ký của NHIỀU sinh viên trong 1 học kỳ bằng số ít truy vấn (tránh N+1).</summary>
    Task<List<DangKyHocPhan>> LayTheoDanhSachMaSVVaMaHocKyAsync(IEnumerable<string> dsMaSV, string maHocKy);

    /// <summary>Cập nhật riêng cột SoTienPhaiDong cho nhiều đăng ký, gom thành ít lần SaveChanges.</summary>
    Task CapNhatSoTienPhaiDongAsync(IEnumerable<(Guid Id, decimal SoTien)> capNhat);
}
