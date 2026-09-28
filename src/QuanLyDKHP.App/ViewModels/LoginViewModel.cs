using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.App.Services;
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

    [ObservableProperty]
    private bool _ghiNhoDangNhap;

    [ObservableProperty]
    private bool _hienMatKhau;

    /// <summary>
    /// Event phát ra khi đăng nhập thành công để App chuyển sang MainWindow.
    /// </summary>
    public event EventHandler? LoginSuccess;

    /// <summary>
    /// Event phát ra mỗi khi có lỗi cần "rung" ô nhập (View lắng nghe để chạy animation).
    /// </summary>
    public event EventHandler? ShakeRequested;

    public LoginViewModel(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;

        var (savedUser, ghiNho) = LocalSessionStore.Load();
        if (ghiNho && !string.IsNullOrWhiteSpace(savedUser))
        {
            TenDangNhap = savedUser;
            GhiNhoDangNhap = true;
        }
    }

    // Default constructor for designer/preview
    public LoginViewModel()
    {
        _authService = null!;
        _currentUserService = null!;
    }

    [RelayCommand]
    private void ToggleHienMatKhau()
    {
        HienMatKhau = !HienMatKhau;
    }

    [RelayCommand]
    private async Task DangNhapAsync()
    {
        LoiThongBao = string.Empty;

        // Validate định dạng trước khi gửi request — tránh gọi DB không cần thiết
        var tenDangNhap = TenDangNhap?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(tenDangNhap))
        {
            LoiThongBao = "Vui lòng nhập mã số sinh viên / tên đăng nhập.";
            ShakeRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (string.IsNullOrEmpty(MatKhau))
        {
            LoiThongBao = "Vui lòng nhập mật khẩu.";
            ShakeRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        DangXuLy = true;
        try
        {
            var user = await _authService.DangNhapAsync(tenDangNhap, MatKhau);

            if (user == null)
            {
                LoiThongBao = "Tên đăng nhập hoặc mật khẩu không đúng.";
                ShakeRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (user.TrangThai == "DaKhoa")
            {
                LoiThongBao = "Tài khoản đã bị khóa, liên hệ Admin.";
                ShakeRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (!Enum.TryParse<UserRole>(user.Role, ignoreCase: true, out var userRole) || !Enum.IsDefined(typeof(UserRole), userRole))
            {
                LoiThongBao = "Vai trò người dùng trong hệ thống không hợp lệ. Vui lòng liên hệ Admin.";
                ShakeRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            _currentUserService.SetCurrentUser(new CurrentUserInfo
            {
                Id = user.Id,
                TenDangNhap = user.TenDangNhap,
                HoTen = user.HoTen,
                Role = userRole,
                MaGV = user.MaGV
            });

            LocalSessionStore.Save(tenDangNhap, GhiNhoDangNhap);

            LoginSuccess?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            LoiThongBao = $"Lỗi kết nối hoặc hệ thống: {ex.Message}";
            ShakeRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            DangXuLy = false;
        }
    }
}
