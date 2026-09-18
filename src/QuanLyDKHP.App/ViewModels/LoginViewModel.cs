using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty]
    private string _tenDangNhap = string.Empty;

    [ObservableProperty]
    private string _matKhau = string.Empty;

    [ObservableProperty]
    private string _loiThongBao = string.Empty;

    [ObservableProperty]
    private bool _dangXuLy;

    /// <summary>
    /// Event phát ra khi đăng nhập thành công để App chuyển sang MainWindow.
    /// </summary>
    public event EventHandler? LoginSuccess;

    public LoginViewModel(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    // Default constructor for designer/preview
    public LoginViewModel()
    {
        _authService = null!;
        _currentUserService = null!;
    }

    [RelayCommand]
    private async Task DangNhapAsync()
    {
        LoiThongBao = string.Empty;

        if (string.IsNullOrWhiteSpace(TenDangNhap) || string.IsNullOrWhiteSpace(MatKhau))
        {
            LoiThongBao = "Vui lòng nhập tên đăng nhập và mật khẩu.";
            return;
        }

        DangXuLy = true;
        try
        {
            var user = await _authService.DangNhapAsync(TenDangNhap.Trim(), MatKhau);

            if (user == null)
            {
                LoiThongBao = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return;
            }

            if (user.TrangThai == "DaKhoa")
            {
                LoiThongBao = "Tài khoản đã bị khóa, liên hệ Admin.";
                return;
            }

            if (!Enum.TryParse<UserRole>(user.Role, out var userRole))
            {
                userRole = UserRole.GiangVien;
            }

            // Lưu thông tin vào CurrentUserService
            _currentUserService.SetCurrentUser(new CurrentUserInfo
            {
                Id = user.Id,
                TenDangNhap = user.TenDangNhap,
                HoTen = user.HoTen,
                Role = userRole,
                MaGV = user.MaGV
            });

            // Phát sự kiện chuyển màn hình
            LoginSuccess?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            LoiThongBao = $"Lỗi kết nối hoặc hệ thống: {ex.Message}";
        }
        finally
        {
            DangXuLy = false;
        }
    }
}
