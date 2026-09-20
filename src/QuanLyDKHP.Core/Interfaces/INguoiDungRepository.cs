using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

/// <summary>
/// Repository Interface cho NguoiDung.
/// </summary>
public interface INguoiDungRepository
{
    Task<List<NguoiDung>> LayTatCaAsync();
    Task<NguoiDung?> GetByIdAsync(int id);
    Task<NguoiDung?> GetByTenDangNhapAsync(string tenDangNhap);
    Task<List<NguoiDung>> LayGiangVienAsync();
    Task ThemAsync(NguoiDung nd);
    Task CapNhatAsync(NguoiDung nd);
    Task XoaAsync(int id);
}
