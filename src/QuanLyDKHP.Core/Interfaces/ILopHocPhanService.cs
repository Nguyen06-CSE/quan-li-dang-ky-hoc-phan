using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface ILopHocPhanService
{
    Task<List<LopHocPhan>> LayTheoHocKyAsync(string maHocKy, string? tuKhoa, string? maMon);
    Task<int> DemSiSoDangKyAsync(string maLHP);
    Task ThemAsync(LopHocPhan lhp);
    Task CapNhatAsync(LopHocPhan lhp);
    Task XoaAsync(string maLHP);
}
