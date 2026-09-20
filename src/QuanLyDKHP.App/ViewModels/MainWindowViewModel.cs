using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

/// <summary>
/// Mục menu hiển thị trên Sidebar/MenuStrip.
/// </summary>
public partial class MenuItemViewModel : ObservableObject
{
    public string Title { get; }
    public string ChucNang { get; }
    public string? Icon { get; }

    public MenuItemViewModel(string title, string chucNang, string? icon = null)
    {
        Title = title;
        ChucNang = chucNang;
        Icon = icon;
    }
}

/// <summary>
/// ViewModel chính cho MainWindow — quản lý điều hướng và lọc menu theo quyền.
/// Dùng ViewModel-first navigation: thay đổi CurrentViewModel → ContentControl hiển thị View tương ứng.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _currentUserDisplayName = string.Empty;

    [ObservableProperty]
    private string _currentUserRole = string.Empty;

    /// <summary>
    /// Danh sách menu đã lọc theo quyền của user đang đăng nhập.
    /// Role không có quyền → mục menu không hiển thị (không phải disable).
    /// </summary>
    public ObservableCollection<MenuItemViewModel> FilteredMenuItems { get; } = new();

    /// <summary>
    /// Toàn bộ menu khả dụng — mỗi mục map tới 1 chức năng trong PermissionMatrix.
    /// Thêm mục mới ở đây khi có màn hình mới.
    /// </summary>
    private static readonly (string Title, string ChucNang, string? Icon)[] AllMenuItems =
    {
        ("Trang chủ",            ChucNang.XemDashboard,         "🏠"),
        ("Sinh viên",            ChucNang.CrudSinhVien,         "🎓"),
        ("Môn học",              ChucNang.CrudMonHoc,           "📚"),
        ("Học kỳ / LHP",         ChucNang.CrudHocKyLopHocPhan,  "🗓️"),
        ("Đăng ký học phần",     ChucNang.DangKyHocPhan,        "📝"),
        ("Học phí",              ChucNang.XemHocPhi,            "💰"),
        ("Báo cáo & Thống kê",  ChucNang.ThongKeSvTheoMon,     "📊"),
        ("Import Excel",        ChucNang.ImportExcel,           "📥"),
        ("Cấu hình hệ thống",   ChucNang.CauHinhHeThong,       "⚙️"),
        ("Quản lý người dùng",  ChucNang.QuanLyNguoiDung,      "👥"),
    };

    private readonly Func<DashboardViewModel> _dashboardViewModelFactory;
    private readonly Func<SinhVienViewModel> _sinhVienViewModelFactory;
    private readonly Func<MonHocViewModel> _monHocViewModelFactory;
    private readonly Func<HocKyLopHocPhanViewModel> _hocKyLopHocPhanViewModelFactory;
    private readonly Func<DangKyHocPhanViewModel> _dangKyHocPhanViewModelFactory;
    private readonly Func<HocPhiViewModel> _hocPhiViewModelFactory;
    private readonly Func<BaoCaoViewModel> _baoCaoViewModelFactory;
    private readonly Func<CauHinhViewModel> _cauHinhViewModelFactory;
    private readonly Func<NguoiDungViewModel> _nguoiDungViewModelFactory;
    private readonly Func<ImportExcelViewModel> _importExcelViewModelFactory;

    public MainWindowViewModel(
        ICurrentUserService currentUserService, 
        Func<DashboardViewModel> dashboardViewModelFactory,
        Func<SinhVienViewModel> sinhVienViewModelFactory,
        Func<MonHocViewModel> monHocViewModelFactory,
        Func<HocKyLopHocPhanViewModel> hocKyLopHocPhanViewModelFactory,
        Func<DangKyHocPhanViewModel> dangKyHocPhanViewModelFactory,
        Func<HocPhiViewModel> hocPhiViewModelFactory,
        Func<BaoCaoViewModel> baoCaoViewModelFactory,
        Func<CauHinhViewModel> cauHinhViewModelFactory,
        Func<NguoiDungViewModel> nguoiDungViewModelFactory,
        Func<ImportExcelViewModel> importExcelViewModelFactory)
    {
        _currentUserService = currentUserService;
        _dashboardViewModelFactory = dashboardViewModelFactory;
        _sinhVienViewModelFactory = sinhVienViewModelFactory;
        _monHocViewModelFactory = monHocViewModelFactory;
        _hocKyLopHocPhanViewModelFactory = hocKyLopHocPhanViewModelFactory;
        _dangKyHocPhanViewModelFactory = dangKyHocPhanViewModelFactory;
        _hocPhiViewModelFactory = hocPhiViewModelFactory;
        _baoCaoViewModelFactory = baoCaoViewModelFactory;
        _cauHinhViewModelFactory = cauHinhViewModelFactory;
        _nguoiDungViewModelFactory = nguoiDungViewModelFactory;
        _importExcelViewModelFactory = importExcelViewModelFactory;
        RefreshMenuForCurrentUser();
        NavigateToHome();
    }

    private void NavigateToHome()
    {
        var homeMenu = FilteredMenuItems.FirstOrDefault(m => m.ChucNang == ChucNang.XemDashboard);
        if (homeMenu != null)
        {
            SelectedMenuItem = homeMenu;
            CurrentViewModel = _dashboardViewModelFactory();
        }
    }

    /// <summary>
    /// Lọc lại menu dựa trên role của user đang đăng nhập.
    /// Gọi lại mỗi khi đăng nhập/đăng xuất.
    /// </summary>
    public void RefreshMenuForCurrentUser()
    {
        FilteredMenuItems.Clear();

        var user = _currentUserService.CurrentUser;
        if (user == null) return;

        CurrentUserDisplayName = user.HoTen;
        CurrentUserRole = user.Role.ToString();

        foreach (var item in AllMenuItems)
        {
            if (PermissionMatrix.HasPermission(item.ChucNang, user.Role))
            {
                FilteredMenuItems.Add(new MenuItemViewModel(item.Title, item.ChucNang, item.Icon));
            }
        }
        NavigateToHome();
    }

    [ObservableProperty]
    private MenuItemViewModel? _selectedMenuItem;

    partial void OnSelectedMenuItemChanged(MenuItemViewModel? value)
    {
        if (value != null)
        {
            NavigateTo(value);
        }
    }

    /// <summary>
    /// Event phát ra khi user nhấn nút Đăng xuất.
    /// </summary>
    public event System.EventHandler? LogoutRequested;

    [RelayCommand]
    private void DangXuat()
    {
        _currentUserService.ClearCurrentUser();
        LogoutRequested?.Invoke(this, System.EventArgs.Empty);
    }

    [RelayCommand]
    private void NavigateTo(MenuItemViewModel menuItem)
    {
        if (menuItem.ChucNang == ChucNang.XemDashboard)
        {
            CurrentViewModel = _dashboardViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.CrudSinhVien)
        {
            CurrentViewModel = _sinhVienViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.CrudMonHoc)
        {
            CurrentViewModel = _monHocViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.CrudHocKyLopHocPhan)
        {
            CurrentViewModel = _hocKyLopHocPhanViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.DangKyHocPhan)
        {
            CurrentViewModel = _dangKyHocPhanViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.XemHocPhi)
        {
            CurrentViewModel = _hocPhiViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.ThongKeSvTheoMon)
        {
            CurrentViewModel = _baoCaoViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.CauHinhHeThong)
        {
            CurrentViewModel = _cauHinhViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.QuanLyNguoiDung)
        {
            CurrentViewModel = _nguoiDungViewModelFactory();
        }
        else if (menuItem.ChucNang == ChucNang.ImportExcel)
        {
            CurrentViewModel = _importExcelViewModelFactory();
        }
    }
}
