using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface ILopHocPhanRepository
{
    Task<List<LopHocPhan>> LayTheoHocKyAsync(string maHocKy, string? tuKhoa, string? maMon);
    Task<LopHocPhan?> GetByIdAsync(string maLHP);
    Task<int> DemSiSoDangKyAsync(string maLHP);
    Task ThemAsync(LopHocPhan lhp);
    Task CapNhatAsync(LopHocPhan lhp);
    Task XoaAsync(string maLHP);
    Task<bool> TonTaiAsync(string maLHP);
    Task<bool> CoDangKyHocPhanAsync(string maLHP);
}
