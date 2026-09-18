using System.Linq;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

/// <summary>
/// Repository Interface cho NguoiDung.
/// </summary>
public interface INguoiDungRepository
{
    Task<NguoiDung?> GetByTenDangNhapAsync(string tenDangNhap);
    Task<List<NguoiDung>> LayGiangVienAsync();
}
