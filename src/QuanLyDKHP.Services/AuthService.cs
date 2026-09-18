using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

/// <summary>
/// Service xử lý xác thực tài khoản và kiểm tra BCrypt password.
/// </summary>
public class AuthService : IAuthService
{
    private readonly INguoiDungRepository _nguoiDungRepository;

    public AuthService(INguoiDungRepository nguoiDungRepository)
    {
        _nguoiDungRepository = nguoiDungRepository;
    }

    public async Task<NguoiDung?> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
        {
            return null;
        }

        // Tìm người dùng qua Repository
        var user = await _nguoiDungRepository.GetByTenDangNhapAsync(tenDangNhap.Trim());

        if (user == null)
        {
            return null;
        }

        // Kiểm tra mật khẩu bằng BCrypt
        bool isValid = BCrypt.Net.BCrypt.Verify(matKhau, user.MatKhauHash);
        if (!isValid)
        {
            return null;
        }

        return user;
    }
}
