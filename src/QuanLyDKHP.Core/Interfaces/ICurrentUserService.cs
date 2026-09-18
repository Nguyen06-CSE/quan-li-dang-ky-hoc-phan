using QuanLyDKHP.Core.Enums;

namespace QuanLyDKHP.Core.Interfaces;

/// <summary>
/// Service lưu thông tin người dùng đang đăng nhập trong suốt phiên làm việc (Singleton).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Thông tin người dùng hiện tại. Null nếu chưa đăng nhập.
    /// </summary>
    CurrentUserInfo? CurrentUser { get; }

    /// <summary>
    /// Kiểm tra role hiện tại có phải là role truyền vào không.
    /// </summary>
    bool IsInRole(UserRole role);

    /// <summary>
    /// Kiểm tra người dùng hiện tại có quyền thực hiện chức năng hay không.
    /// </summary>
    /// <param name="chucNang">Tên chức năng (dùng constant từ <see cref="Authorization.ChucNang"/>).</param>
    bool HasPermission(string chucNang);

    /// <summary>
    /// Đặt thông tin user sau khi đăng nhập thành công.
    /// </summary>
    void SetCurrentUser(CurrentUserInfo user);

    /// <summary>
    /// Xóa thông tin user khi đăng xuất.
    /// </summary>
    void ClearCurrentUser();
}

/// <summary>
/// DTO chứa thông tin người dùng đang đăng nhập.
/// </summary>
public class CurrentUserInfo
{
    public int Id { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? MaGV { get; set; }
}
