using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

/// <summary>
/// Interface cho AuthService phục vụ xác thực người dùng.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng nhập bằng tên đăng nhập và mật khẩu.
    /// Trả về NguoiDung nếu thành công, null nếu thất bại.
    /// </summary>
    Task<NguoiDung?> DangNhapAsync(string tenDangNhap, string matKhau);
}
