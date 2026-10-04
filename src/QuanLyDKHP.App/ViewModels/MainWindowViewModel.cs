using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Interfaces;
using Material.Icons;


namespace QuanLyDKHP.App.ViewModels;

/// <summary>
/// Mục menu hiển thị trên Sidebar/MenuStrip.
/// </summary>
public partial class MenuItemViewModel : ObservableObject
{
    public string Title { get; }
    public string ChucNang { get; }
    public MaterialIconKind Icon { get; }
    public MenuItemViewModel(string title, string chucNang, MaterialIconKind icon)
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

    /// <summary>
    /// Cache các ViewModel đã khởi tạo theo mã chức năng.
    /// Giúp điều hướng quay lại màn hình cũ gần như tức thời (0ms) thay vì tạo mới.
    /// </summary>
    private readonly Dictionary<string, ObservableObject> _viewModelCache = new();

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _currentUserDisplayName = string.Empty;

    [ObservableProperty]
    private string _currentUserRole = string.Empty;

    // ===== App Shell (Module 2) =====

    /// <summary>Chiều cao 1 mục menu — phải khớp style ListBoxItem.nav-item trong MainWindow.axaml.</summary>
    private const double NavItemHeight = 44;
    private const double SidebarExpandedWidth = 232;
    private const double SidebarCollapsedWidth = 68;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidebarWidth))]
    private bool _isSidebarExpanded = true;

    public double SidebarWidth => IsSidebarExpanded ? SidebarExpandedWidth : SidebarCollapsedWidth;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Chữ cái đầu của họ tên để vẽ avatar tròn.</summary>
    public string UserInitials
    {
        get
        {
            var parts = (CurrentUserDisplayName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
            return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
        }
    }

    /// <summary>Vị trí dải màu (indicator) trượt theo mục đang chọn.</summary>
    public Thickness IndicatorMargin
    {
        get
        {
            var idx = SelectedMenuItem == null ? 0 : FilteredMenuItems.IndexOf(SelectedMenuItem);
            if (idx < 0) idx = 0;
            return new Thickness(0, idx * NavItemHeight + (NavItemHeight - 24) / 2, 0, 0);
        }
    }

    public bool IsIndicatorVisible => SelectedMenuItem != null && FilteredMenuItems.Contains(SelectedMenuItem);

    public string BreadcrumbCurrent => SelectedMenuItem?.Title ?? string.Empty;

    /// <summary>Đang ở Trang chủ thì breadcrumb chỉ hiện "Trang chủ", không lặp "Trang chủ > Trang chủ".</summary>
    public bool ShowBreadcrumbTail => SelectedMenuItem != null && SelectedMenuItem.ChucNang != ChucNang.XemDashboard;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    [RelayCommand]
    private void ToggleTheme() => IsDarkMode = !IsDarkMode;

    /// <summary>
    /// Tìm nhanh: gõ tên màn hình (không cần dấu) rồi Enter để nhảy tới mục đầu tiên khớp.
    /// Chỉ tìm trong các màn hình user có quyền — không tìm dữ liệu bên trong từng màn hình.
    /// </summary>
    [RelayCommand]
    private void QuickSearch()
    {
        var key = RemoveDiacritics(SearchText?.Trim() ?? string.Empty);
        if (key.Length == 0) return;

        var match = FilteredMenuItems.FirstOrDefault(m => RemoveDiacritics(m.Title).Contains(key));
        if (match != null)
        {
            SelectedMenuItem = match;
            SearchText = string.Empty;
        }
    }

    private static string RemoveDiacritics(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();
    }

    /// <summary>
    /// Danh sách menu đã lọc theo quyền của user đang đăng nhập.
    /// Role không có quyền → mục menu không hiển thị (không phải disable).
    /// </summary>
    public ObservableCollection<MenuItemViewModel> FilteredMenuItems { get; } = new();

    /// <summary>
    /// Toàn bộ menu khả dụng — mỗi mục map tới 1 chức năng trong PermissionMatrix.
    /// Thêm mục mới ở đây khi có màn hình mới.
    /// </summary>
    private static readonly (string Title, string ChucNang, MaterialIconKind Icon)[] AllMenuItems =
{
    ("Trang chủ",           ChucNang.XemDashboard,        MaterialIconKind.HomeOutline),
    ("Sinh viên",           ChucNang.CrudSinhVien,        MaterialIconKind.SchoolOutline),
    ("Môn học",             ChucNang.CrudMonHoc,          MaterialIconKind.BookOpenPageVariantOutline),
    ("Học kỳ / LHP",        ChucNang.CrudHocKyLopHocPhan, MaterialIconKind.CalendarMonthOutline),
    ("Đăng ký học phần",    ChucNang.DangKyHocPhan,       MaterialIconKind.ClipboardEditOutline),
    ("Học phí",             ChucNang.XemHocPhi,           MaterialIconKind.CashMultiple),
    ("Báo cáo & Thống kê", ChucNang.ThongKeSvTheoMon,    MaterialIconKind.ChartBar),
    ("Import Excel",       ChucNang.ImportExcel,          MaterialIconKind.FileExcelOutline),
    ("Cấu hình hệ thống",  ChucNang.CauHinhHeThong,      MaterialIconKind.CogOutline),
    ("Quản lý người dùng", ChucNang.QuanLyNguoiDung,     MaterialIconKind.AccountGroupOutline),
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
            // Đổi SelectedMenuItem sẽ kích hoạt OnSelectedMenuItemChanged -> NavigateTo (chỉ tạo Dashboard 1 lần)
            SelectedMenuItem = homeMenu;
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
        OnPropertyChanged(nameof(UserInitials));

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
    [NotifyPropertyChangedFor(nameof(IndicatorMargin))]
    [NotifyPropertyChangedFor(nameof(IsIndicatorVisible))]
    [NotifyPropertyChangedFor(nameof(BreadcrumbCurrent))]
    [NotifyPropertyChangedFor(nameof(ShowBreadcrumbTail))]
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
        _viewModelCache.Clear();
        _currentUserService.ClearCurrentUser();
        LogoutRequested?.Invoke(this, System.EventArgs.Empty);
    }

    /// <summary>
    /// Điều hướng tới màn hình tương ứng với mục menu.
    /// Sử dụng cache để tái sử dụng ViewModel đã khởi tạo — điều hướng quay lại gần như tức thời.
    /// </summary>
    private void NavigateTo(MenuItemViewModel menuItem)
    {
        if (menuItem == null) return;

        // Kiểm tra nếu đã có trong cache thì lấy ra dùng lại ngay (0ms)
        if (_viewModelCache.TryGetValue(menuItem.ChucNang, out var cachedVm))
        {
            CurrentViewModel = cachedVm;
            return;
        }

        // Nếu chưa có, tạo lần đầu tiên và nhét vào cache
        ObservableObject? newVm = menuItem.ChucNang switch
        {
            ChucNang.XemDashboard         => _dashboardViewModelFactory(),
            ChucNang.CrudSinhVien         => _sinhVienViewModelFactory(),
            ChucNang.CrudMonHoc           => _monHocViewModelFactory(),
            ChucNang.CrudHocKyLopHocPhan  => _hocKyLopHocPhanViewModelFactory(),
            ChucNang.DangKyHocPhan        => _dangKyHocPhanViewModelFactory(),
            ChucNang.XemHocPhi            => _hocPhiViewModelFactory(),
            ChucNang.ThongKeSvTheoMon     => _baoCaoViewModelFactory(),
            ChucNang.ImportExcel          => _importExcelViewModelFactory(),
            ChucNang.CauHinhHeThong       => _cauHinhViewModelFactory(),
            ChucNang.QuanLyNguoiDung      => _nguoiDungViewModelFactory(),
            _ => null
        };

        if (newVm != null)
        {
            _viewModelCache[menuItem.ChucNang] = newVm;
            CurrentViewModel = newVm;
        }
    }
}