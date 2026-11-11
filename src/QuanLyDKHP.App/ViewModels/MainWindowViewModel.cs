using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Material.Icons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDKHP.App.Messages;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

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
/// ViewModel chính cho MainWindow — quản lý điều hướng, lọc menu theo quyền
/// và vận hành Universal Command Palette &amp; Live X-Ray Spotlight (Ctrl + K / ⌘K).
/// </summary>
public partial class MainWindowViewModel : ObservableObject,
    IRecipient<NavigateToRegistrationMessage>,
    IRecipient<NavigateToImportExcelMessage>,
    IRecipient<NavigateToHocPhiMessage>,
    IRecipient<DataImportedNotificationMessage>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDbContextFactory<AppDbContext>? _dbContextFactory;

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

    // ===== Universal Command Palette & X-Ray Spotlight (Ctrl + K) =====

    private CancellationTokenSource? _paletteSearchCts;

    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    [ObservableProperty]
    private string _paletteQuery = string.Empty;

    [ObservableProperty]
    private int _paletteFilterIndex; // 0: Tất cả, 1: Sinh viên, 2: Lớp HP & Môn, 3: Truy vấn & Lệnh, 4: Điều hướng

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStudentXRayVisible))]
    [NotifyPropertyChangedFor(nameof(IsClassXRayVisible))]
    [NotifyPropertyChangedFor(nameof(IsActionXRayVisible))]
    private CommandPaletteItemViewModel? _selectedPaletteItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStudentXRayVisible))]
    [NotifyPropertyChangedFor(nameof(IsActionXRayVisible))]
    private SpotlightStudentXRayDto? _studentXRay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsClassXRayVisible))]
    [NotifyPropertyChangedFor(nameof(IsActionXRayVisible))]
    private SpotlightClassXRayDto? _classXRay;

    [ObservableProperty]
    private string? _paletteStatusToast;

    [ObservableProperty]
    private int _paletteResultCount;

    public ObservableCollection<CommandPaletteItemViewModel> PaletteItems { get; } = new();

    public bool IsStudentXRayVisible => SelectedPaletteItem?.ItemKind == CommandPaletteItemKind.Student && StudentXRay != null;
    public bool IsClassXRayVisible => (SelectedPaletteItem?.ItemKind == CommandPaletteItemKind.ClassSection || SelectedPaletteItem?.ItemKind == CommandPaletteItemKind.Course) && ClassXRay != null;
    public bool IsActionXRayVisible => SelectedPaletteItem != null && !IsStudentXRayVisible && !IsClassXRayVisible;

    /// <summary>Event yêu cầu View focus vào ô tìm kiếm của Command Palette khi vừa mở.</summary>
    public event EventHandler? CommandPaletteOpened;

    partial void OnIsCommandPaletteOpenChanged(bool value)
    {
        if (value)
        {
            PaletteStatusToast = null;
            _ = RefreshPaletteResultsAsync();
            CommandPaletteOpened?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnPaletteQueryChanged(string value)
    {
        if (IsCommandPaletteOpen)
        {
            _ = RefreshPaletteResultsAsync();
        }
    }

    partial void OnPaletteFilterIndexChanged(int value)
    {
        if (IsCommandPaletteOpen)
        {
            _ = RefreshPaletteResultsAsync();
        }
    }

    partial void OnSelectedPaletteItemChanged(CommandPaletteItemViewModel? value)
    {
        _ = LoadXRayForSelectedItemAsync(value);
    }

    [RelayCommand]
    public void OpenCommandPalette()
    {
        IsCommandPaletteOpen = true;
    }

    [RelayCommand]
    public void CloseCommandPalette()
    {
        IsCommandPaletteOpen = false;
    }

    [RelayCommand]
    public void ToggleCommandPalette()
    {
        IsCommandPaletteOpen = !IsCommandPaletteOpen;
    }

    [RelayCommand]
    public void SetPaletteFilter(string indexStr)
    {
        if (int.TryParse(indexStr, out int idx))
        {
            PaletteFilterIndex = idx;
        }
    }

    [RelayCommand]
    public void SelectSmartQueryTag(string tagQuery)
    {
        PaletteFilterIndex = 0;
        PaletteQuery = tagQuery;
    }

    [RelayCommand]
    public void MovePaletteSelectionDown()
    {
        if (PaletteItems.Count == 0) return;
        int currentIndex = SelectedPaletteItem != null ? PaletteItems.IndexOf(SelectedPaletteItem) : -1;
        int nextIndex = (currentIndex + 1) % PaletteItems.Count;
        SelectedPaletteItem = PaletteItems[nextIndex];
    }

    [RelayCommand]
    public void MovePaletteSelectionUp()
    {
        if (PaletteItems.Count == 0) return;
        int currentIndex = SelectedPaletteItem != null ? PaletteItems.IndexOf(SelectedPaletteItem) : 0;
        int prevIndex = (currentIndex - 1 + PaletteItems.Count) % PaletteItems.Count;
        SelectedPaletteItem = PaletteItems[prevIndex];
    }

    [RelayCommand]
    public async Task ExecuteSelectedPaletteItemAsync()
    {
        var item = SelectedPaletteItem;
        if (item?.ExecuteAsync != null)
        {
            await item.ExecuteAsync();
        }
    }

    [RelayCommand]
    public void OpenSelectedStudentRegistration()
    {
        if (StudentXRay == null) return;
        string maSv = StudentXRay.MaSV;
        IsCommandPaletteOpen = false;
        WeakReferenceMessenger.Default.Send(new NavigateToRegistrationMessage(maSv));
    }

    [RelayCommand]
    public void OpenSelectedStudentTuition()
    {
        if (StudentXRay == null) return;
        string maSv = StudentXRay.MaSV;
        IsCommandPaletteOpen = false;
        WeakReferenceMessenger.Default.Send(new NavigateToHocPhiMessage(maSv));
    }

    [RelayCommand]
    public async Task CopySelectedStudentSummaryAsync()
    {
        if (StudentXRay == null) return;
        try
        {
            var summary = $"[HỒ SƠ SINH VIÊN] {StudentXRay.HoTen} ({StudentXRay.MaSV}) | Lớp: {StudentXRay.LopSinhHoat} | Khóa: {StudentXRay.KhoaHoc} | ĐK kỳ {StudentXRay.MaHocKy}: {StudentXRay.TongTinChi} TC ({StudentXRay.SoLopDangHoc} lớp) | Học phí: {StudentXRay.TongHocPhiFormat} (Còn nợ: {StudentXRay.ConNoFormat})";
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.Clipboard is IClipboard clipboard)
            {
                await clipboard.SetTextAsync(summary);
                PaletteStatusToast = $"✔ Đã sao chép hồ sơ {StudentXRay.MaSV} vào Clipboard!";
            }
        }
        catch (Exception ex)
        {
            PaletteStatusToast = $"Lỗi sao chép: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SmartEnrollSelectedClass()
    {
        if (ClassXRay == null) return;
        string maHk = ClassXRay.MaHocKy;
        string maLhp = ClassXRay.MaLHP;
        IsCommandPaletteOpen = false;
        WeakReferenceMessenger.Default.Send(new NavigateToImportExcelMessage(maHk, maLhp));
    }

    [RelayCommand]
    public void OpenSelectedClassManagement()
    {
        IsCommandPaletteOpen = false;
        var menu = FilteredMenuItems.FirstOrDefault(m => m.ChucNang == ChucNang.CrudHocKyLopHocPhan);
        if (menu != null)
        {
            SelectedMenuItem = menu;
        }
    }

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
    /// Khi người dùng nhấn Enter ở thanh Topbar hoặc gõ tìm kiếm, mở ngay Universal Command Palette.
    /// </summary>
    [RelayCommand]
    private void QuickSearch()
    {
        var key = SearchText?.Trim() ?? string.Empty;
        PaletteQuery = key;
        SearchText = string.Empty;
        IsCommandPaletteOpen = true;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
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
        Func<ImportExcelViewModel> importExcelViewModelFactory,
        IDbContextFactory<AppDbContext>? dbContextFactory = null)
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
        _dbContextFactory = dbContextFactory ?? App.ServiceProvider?.GetService<IDbContextFactory<AppDbContext>>();

        // Đăng ký nhận message điều hướng liên module
        WeakReferenceMessenger.Default.RegisterAll(this);

        RefreshMenuForCurrentUser();
        NavigateToHome();
    }

    /// <summary>
    /// Xử lý message điều hướng từ tab Sinh Viên hoặc Command Palette sang tab Đăng Ký Học Phần.
    /// </summary>
    public void Receive(NavigateToRegistrationMessage message)
    {
        var regMenu = FilteredMenuItems.FirstOrDefault(m => m.ChucNang == ChucNang.DangKyHocPhan);
        if (regMenu != null)
        {
            SelectedMenuItem = regMenu;
            if (CurrentViewModel is DangKyHocPhanViewModel dkVm)
            {
                _ = dkVm.ChonSinhVienTheoMaAsync(message.MaSV);
            }
        }
    }

    /// <summary>
    /// Xử lý message điều hướng sang tab Import Excel / Smart Studio.
    /// </summary>
    public void Receive(NavigateToImportExcelMessage message)
    {
        var importMenu = FilteredMenuItems.FirstOrDefault(m => m.ChucNang == ChucNang.ImportExcel);
        if (importMenu != null)
        {
            SelectedMenuItem = importMenu;
            if (CurrentViewModel is ImportExcelViewModel importVm && !string.IsNullOrWhiteSpace(message.MaLHP))
            {
                importVm.Receive(message);
            }
        }
    }

    /// <summary>
    /// Xử lý message điều hướng sang tab Học Phí và chọn sẵn sinh viên.
    /// </summary>
    public void Receive(NavigateToHocPhiMessage message)
    {
        var hpMenu = FilteredMenuItems.FirstOrDefault(m => m.ChucNang == ChucNang.XemHocPhi);
        if (hpMenu != null)
        {
            SelectedMenuItem = hpMenu;
            if (CurrentViewModel is HocPhiViewModel hpVm)
            {
                _ = hpVm.ChonSinhVienTheoMaAsync(message.MaSV);
            }
        }
    }

    /// <summary>
    /// Xử lý message thông báo import Excel thành công:
    /// Xóa cache các ViewModel khác để khi chuyển tab sẽ tải lại dữ liệu mới nhất từ CSDL.
    /// </summary>
    public void Receive(DataImportedNotificationMessage message)
    {
        var currentImportVm = _viewModelCache.TryGetValue(ChucNang.ImportExcel, out var vm) ? vm : null;
        _viewModelCache.Clear();
        if (currentImportVm != null)
        {
            _viewModelCache[ChucNang.ImportExcel] = currentImportVm;
        }
    }

    // ==================== COMMAND PALETTE & LIVE X-RAY ENGINE ====================

    private async Task RefreshPaletteResultsAsync()
    {
        _paletteSearchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _paletteSearchCts = cts;
        var token = cts.Token;

        try
        {
            string rawQuery = (PaletteQuery ?? string.Empty).Trim();
            string normQuery = RemoveDiacritics(rawQuery);
            int filterIdx = PaletteFilterIndex;

            var results = new List<CommandPaletteItemViewModel>();

            // 1. NHÓM TRUY VẤN THÔNG MINH (AI / SMART INTENT QUERIES)
            bool isUnpaidQuery = rawQuery.Equals("!no-hoc-phi", StringComparison.OrdinalIgnoreCase) ||
                                 normQuery.Contains("no hoc phi") || normQuery.Contains("con no") || normQuery.Contains("chua dong");
            bool isCapacityQuery = rawQuery.Equals("!lop-sap-day", StringComparison.OrdinalIgnoreCase) ||
                                   normQuery.Contains("sap day") || normQuery.Contains("qua tai") || normQuery.Contains("day lop");
            bool isZeroCreditQuery = rawQuery.Equals("!chua-dang-ky", StringComparison.OrdinalIgnoreCase) ||
                                     normQuery.Contains("chua dang ky") || normQuery.Contains("0 tin chi") || normQuery.Contains("0 tc");

            if (filterIdx == 0 || filterIdx == 3)
            {
                if (string.IsNullOrEmpty(rawQuery) || (!isUnpaidQuery && !isCapacityQuery && !isZeroCreditQuery))
                {
                    var smartQueries = GetSmartQueryTemplates();
                    foreach (var sq in smartQueries)
                    {
                        if (string.IsNullOrEmpty(normQuery) ||
                            RemoveDiacritics(sq.Title).Contains(normQuery) ||
                            RemoveDiacritics(sq.Subtitle).Contains(normQuery))
                        {
                            results.Add(sq);
                        }
                    }
                }
            }

            // 2. XỬ LÝ DỮ LIỆU THỰC TẾ TRÊN SQLITE LOCAL (< 2ms)
            if (_dbContextFactory != null && !token.IsCancellationRequested)
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync(token);
                var activeHk = await db.HocKys.AsNoTracking()
                    .OrderByDescending(h => h.DangMo)
                    .ThenByDescending(h => h.MaHocKy)
                    .FirstOrDefaultAsync(token);
                string activeMaHk = activeHk?.MaHocKy ?? string.Empty;

                // 2A. Nếu người dùng kích hoạt Truy vấn thông minh: Sinh viên nợ học phí
                if (isUnpaidQuery && !string.IsNullOrEmpty(activeMaHk))
                {
                    var debtRecords = await db.HocPhiHocKys.AsNoTracking()
                        .Include(h => h.SinhVien)
                        .Where(h => h.MaHocKy == activeMaHk && h.ConNo > 0 && h.SinhVien != null && !h.SinhVien.IsDeleted)
                        .OrderByDescending(h => h.ConNo)
                        .Take(25)
                        .ToListAsync(token);

                    foreach (var h in debtRecords)
                    {
                        var sv = h.SinhVien!;
                        results.Add(CreateStudentPaletteItem(
                            sv.MaSV,
                            sv.HoTen,
                            sv.LopSinhHoat,
                            sv.KhoaHoc,
                            $"Nợ {h.ConNo:N0} đ",
                            "Danger",
                            $"MSSV: {sv.MaSV} • Lớp: {sv.LopSinhHoat ?? "--"} • Đã đóng: {h.DaDong:N0} đ / {h.TongHocPhi:N0} đ"));
                    }
                }
                // 2B. Nếu người dùng kích hoạt Truy vấn thông minh: Lớp học phần sắp đầy / quá tải
                else if (isCapacityQuery && !string.IsNullOrEmpty(activeMaHk))
                {
                    var classes = await db.LopHocPhans.AsNoTracking()
                        .Include(l => l.MonHoc)
                        .Where(l => !l.IsDeleted && l.MaHocKy == activeMaHk)
                        .ToListAsync(token);

                    var counts = await db.DangKyHocPhans.AsNoTracking()
                        .Where(d => d.TrangThai == "DangHoc" && d.LopHocPhan.MaHocKy == activeMaHk)
                        .GroupBy(d => d.MaLHP)
                        .Select(g => new { MaLHP = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.MaLHP, x => x.Count, token);

                    var highOcc = classes
                        .Select(l =>
                        {
                            int cur = counts.TryGetValue(l.MaLHP, out int c) ? c : 0;
                            int max = l.SiSoToiDa ?? 50;
                            double pct = max > 0 ? cur * 100.0 / max : 0;
                            return (Lop: l, Cur: cur, Max: max, Pct: pct);
                        })
                        .OrderByDescending(x => x.Pct)
                        .ThenByDescending(x => x.Cur)
                        .Take(25)
                        .ToList();

                    foreach (var item in highOcc)
                    {
                        string status = item.Pct >= 100 ? "Danger" : (item.Pct >= 75 ? "Warning" : "Success");
                        results.Add(CreateClassPaletteItem(
                            item.Lop.MaLHP,
                            item.Lop.MaMon,
                            item.Lop.MonHoc?.TenMon ?? item.Lop.MaMon,
                            item.Lop.MaHocKy,
                            $"{item.Cur}/{item.Max} SV ({item.Pct:0}%)",
                            status,
                            FormatLichHoc(item.Lop.Thu, item.Lop.TietBatDau, item.Lop.SoTiet, item.Lop.Phong)));
                    }
                }
                // 2C. Nếu người dùng kích hoạt Truy vấn thông minh: Sinh viên chưa đăng ký môn nào (0 TC)
                else if (isZeroCreditQuery && !string.IsNullOrEmpty(activeMaHk))
                {
                    var registeredSvIds = await db.DangKyHocPhans.AsNoTracking()
                        .Where(d => d.TrangThai == "DangHoc" && d.LopHocPhan.MaHocKy == activeMaHk)
                        .Select(d => d.MaSV)
                        .Distinct()
                        .ToListAsync(token);
                    var regSet = new HashSet<string>(registeredSvIds, StringComparer.OrdinalIgnoreCase);

                    var allStudents = await db.SinhViens.AsNoTracking()
                        .Where(s => !s.IsDeleted)
                        .OrderBy(s => s.LopSinhHoat)
                        .ThenBy(s => s.MaSV)
                        .ToListAsync(token);

                    foreach (var sv in allStudents.Where(s => !regSet.Contains(s.MaSV)).Take(25))
                    {
                        results.Add(CreateStudentPaletteItem(
                            sv.MaSV,
                            sv.HoTen,
                            sv.LopSinhHoat,
                            sv.KhoaHoc,
                            "0 Tín chỉ",
                            "Warning",
                            $"MSSV: {sv.MaSV} • Lớp: {sv.LopSinhHoat ?? "--"} • Chưa đăng ký học phần nào kỳ {activeMaHk}"));
                    }
                }
                // 2D. Tìm kiếm đa thực thể thông thường (Sinh viên & Lớp học phần)
                else
                {
                    // Tìm Sinh viên
                    if (filterIdx == 0 || filterIdx == 1)
                    {
                        var svList = await db.SinhViens.AsNoTracking()
                            .Where(s => !s.IsDeleted)
                            .OrderBy(s => s.MaSV)
                            .ToListAsync(token);

                        IEnumerable<Core.Entities.SinhVien> matchedSv;
                        if (string.IsNullOrEmpty(normQuery))
                        {
                            matchedSv = svList.Take(filterIdx == 1 ? 25 : 5);
                        }
                        else
                        {
                            matchedSv = svList.Where(s =>
                                s.MaSV.Contains(rawQuery, StringComparison.OrdinalIgnoreCase) ||
                                RemoveDiacritics(s.HoTen).Contains(normQuery) ||
                                (s.LopSinhHoat != null && RemoveDiacritics(s.LopSinhHoat).Contains(normQuery)) ||
                                (s.KhoaHoc != null && RemoveDiacritics(s.KhoaHoc).Contains(normQuery)))
                                .Take(20);
                        }

                        foreach (var sv in matchedSv)
                        {
                            results.Add(CreateStudentPaletteItem(
                                sv.MaSV,
                                sv.HoTen,
                                sv.LopSinhHoat,
                                sv.KhoaHoc,
                                sv.LopSinhHoat ?? sv.MaSV,
                                "Primary",
                                $"MSSV: {sv.MaSV} • Lớp sinh hoạt: {sv.LopSinhHoat ?? "--"} • Khóa: {sv.KhoaHoc ?? "--"}"));
                        }
                    }

                    // Tìm Lớp học phần & Môn học
                    if (filterIdx == 0 || filterIdx == 2)
                    {
                        var lhpList = await db.LopHocPhans.AsNoTracking()
                            .Include(l => l.MonHoc)
                            .Where(l => !l.IsDeleted && (string.IsNullOrEmpty(activeMaHk) || l.MaHocKy == activeMaHk))
                            .OrderBy(l => l.MaLHP)
                            .ToListAsync(token);

                        IEnumerable<Core.Entities.LopHocPhan> matchedLhp;
                        if (string.IsNullOrEmpty(normQuery))
                        {
                            matchedLhp = lhpList.Take(filterIdx == 2 ? 25 : 4);
                        }
                        else
                        {
                            matchedLhp = lhpList.Where(l =>
                                l.MaLHP.Contains(rawQuery, StringComparison.OrdinalIgnoreCase) ||
                                l.MaMon.Contains(rawQuery, StringComparison.OrdinalIgnoreCase) ||
                                (l.MonHoc != null && RemoveDiacritics(l.MonHoc.TenMon).Contains(normQuery)))
                                .Take(15);
                        }

                        var lhpCodes = matchedLhp.Select(l => l.MaLHP).ToList();
                        var counts = await db.DangKyHocPhans.AsNoTracking()
                            .Where(d => d.TrangThai == "DangHoc" && lhpCodes.Contains(d.MaLHP))
                            .GroupBy(d => d.MaLHP)
                            .Select(g => new { MaLHP = g.Key, Count = g.Count() })
                            .ToDictionaryAsync(x => x.MaLHP, x => x.Count, token);

                        foreach (var l in matchedLhp)
                        {
                            int cur = counts.TryGetValue(l.MaLHP, out int c) ? c : 0;
                            int max = l.SiSoToiDa ?? 50;
                            string badgeStatus = cur >= max ? "Danger" : (cur >= max * 0.8 ? "Warning" : "Success");
                            results.Add(CreateClassPaletteItem(
                                l.MaLHP,
                                l.MaMon,
                                l.MonHoc?.TenMon ?? l.MaMon,
                                l.MaHocKy,
                                $"{cur}/{max} SV",
                                badgeStatus,
                                FormatLichHoc(l.Thu, l.TietBatDau, l.SoTiet, l.Phong)));
                        }
                    }
                }
            }

            // 3. NHÓM LỆNH THỰC THI NHANH (QUICK ACTIONS)
            if ((filterIdx == 0 || filterIdx == 3) && !isUnpaidQuery && !isCapacityQuery && !isZeroCreditQuery)
            {
                foreach (var act in GetQuickActions())
                {
                    if (string.IsNullOrEmpty(normQuery) ||
                        RemoveDiacritics(act.Title).Contains(normQuery) ||
                        RemoveDiacritics(act.Subtitle).Contains(normQuery))
                    {
                        results.Add(act);
                    }
                }
            }

            // 4. NHÓM ĐIỀU HƯỚNG MÀN HÌNH (NAVIGATION)
            if ((filterIdx == 0 || filterIdx == 4) && !isUnpaidQuery && !isCapacityQuery && !isZeroCreditQuery)
            {
                foreach (var menu in FilteredMenuItems)
                {
                    if (string.IsNullOrEmpty(normQuery) || RemoveDiacritics(menu.Title).Contains(normQuery))
                    {
                        var targetMenu = menu;
                        results.Add(new CommandPaletteItemViewModel
                        {
                            Id = $"nav-{targetMenu.ChucNang}",
                            Title = $"Mở màn hình: {targetMenu.Title}",
                            Subtitle = $"Điều hướng tức thì (0ms Cache) tới phân hệ {targetMenu.Title}",
                            CategoryLabel = "MÀN HÌNH",
                            BadgeText = "Điều hướng",
                            BadgeStatus = "Info",
                            Icon = targetMenu.Icon,
                            ShortcutHint = "Enter ↵",
                            ItemKind = CommandPaletteItemKind.Navigation,
                            EntityKey = targetMenu.ChucNang,
                            DetailedDescription = $"Chuyển ngay sang màn hình làm việc '{targetMenu.Title}' mà không cần rời tay khỏi bàn phím.",
                            ExecuteAsync = () =>
                            {
                                SelectedMenuItem = targetMenu;
                                IsCommandPaletteOpen = false;
                                return Task.CompletedTask;
                            }
                        });
                    }
                }
            }

            if (token.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PaletteItems.Clear();
                foreach (var r in results)
                {
                    PaletteItems.Add(r);
                }
                PaletteResultCount = PaletteItems.Count;
                SelectedPaletteItem = PaletteItems.FirstOrDefault();
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandPalette Error]: {ex.Message}");
        }
    }

    private async Task LoadXRayForSelectedItemAsync(CommandPaletteItemViewModel? item)
    {
        if (item == null || _dbContextFactory == null)
        {
            StudentXRay = null;
            ClassXRay = null;
            return;
        }

        try
        {
            if (item.ItemKind == CommandPaletteItemKind.Student && !string.IsNullOrWhiteSpace(item.EntityKey))
            {
                ClassXRay = null;
                string maSv = item.EntityKey;

                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var sv = await db.SinhViens.AsNoTracking().FirstOrDefaultAsync(s => s.MaSV == maSv);
                if (sv == null) return;

                var activeHk = await db.HocKys.AsNoTracking()
                    .OrderByDescending(h => h.DangMo)
                    .ThenByDescending(h => h.MaHocKy)
                    .FirstOrDefaultAsync();
                string maHk = activeHk?.MaHocKy ?? string.Empty;
                string tenHk = activeHk?.TenHocKy ?? "Học kỳ hiện hành";

                var dkList = await db.DangKyHocPhans.AsNoTracking()
                    .Include(d => d.LopHocPhan)
                        .ThenInclude(l => l.MonHoc)
                    .Where(d => d.MaSV == maSv && d.TrangThai == "DangHoc" && (string.IsNullOrEmpty(maHk) || d.LopHocPhan.MaHocKy == maHk))
                    .ToListAsync();

                var courses = dkList.Select(d =>
                {
                    var mh = d.LopHocPhan?.MonHoc;
                    int tc = (mh?.SoTinChiLT ?? 0) + (mh?.SoTinChiTH ?? 0);
                    return new SpotlightStudentCourseDto
                    {
                        MaLHP = d.MaLHP,
                        TenMon = mh?.TenMon ?? d.MaLHP,
                        SoTinChi = tc,
                        LichHocText = FormatLichHoc(d.LopHocPhan?.Thu, d.LopHocPhan?.TietBatDau, d.LopHocPhan?.SoTiet, d.LopHocPhan?.Phong)
                    };
                }).ToList();

                int tongTc = courses.Sum(c => c.SoTinChi);

                var hpRecord = await db.HocPhiHocKys.AsNoTracking()
                    .FirstOrDefaultAsync(h => h.MaSV == maSv && h.MaHocKy == maHk);

                decimal tongHp = hpRecord?.TongHocPhi ?? 0m;
                decimal daDong = hpRecord?.DaDong ?? 0m;
                decimal conNo = hpRecord?.ConNo ?? 0m;

                if (hpRecord == null && tongTc > 0)
                {
                    var cauHinhDict = await db.CauHinhHeThongs.AsNoTracking()
                        .ToDictionaryAsync(c => c.Key, c => c.Value);
                    decimal donGiaLt = cauHinhDict.TryGetValue("DonGiaTinChiLT", out var ltVal) && decimal.TryParse(ltVal, out var lt) ? lt : 450000m;
                    decimal donGiaTh = cauHinhDict.TryGetValue("DonGiaTinChiTH", out var thVal) && decimal.TryParse(thVal, out var th) ? th : 600000m;
                    int tcLt = dkList.Sum(d => d.LopHocPhan?.MonHoc?.SoTinChiLT ?? 0);
                    int tcTh = dkList.Sum(d => d.LopHocPhan?.MonHoc?.SoTinChiTH ?? 0);
                    tongHp = tcLt * donGiaLt + tcTh * donGiaTh;
                    conNo = tongHp;
                }

                StudentXRay = new SpotlightStudentXRayDto
                {
                    MaSV = sv.MaSV,
                    HoTen = sv.HoTen,
                    LopSinhHoat = sv.LopSinhHoat ?? "--",
                    KhoaHoc = sv.KhoaHoc ?? "--",
                    MaHocKy = maHk,
                    TenHocKy = tenHk,
                    TongTinChi = tongTc,
                    SoLopDangHoc = courses.Count,
                    TongHocPhi = tongHp,
                    DaDong = daDong,
                    ConNo = conNo,
                    Courses = courses
                };
            }
            else if (item.ItemKind == CommandPaletteItemKind.ClassSection && !string.IsNullOrWhiteSpace(item.EntityKey))
            {
                StudentXRay = null;
                string maLhp = item.EntityKey;

                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var lhp = await db.LopHocPhans.AsNoTracking()
                    .Include(l => l.MonHoc)
                    .FirstOrDefaultAsync(l => l.MaLHP == maLhp);
                if (lhp == null) return;

                string gvName = lhp.MaGV ?? "Chưa phân công";
                if (!string.IsNullOrWhiteSpace(lhp.MaGV))
                {
                    var gv = await db.NguoiDungs.AsNoTracking()
                        .FirstOrDefaultAsync(u => !u.IsDeleted && (u.MaGV == lhp.MaGV || u.TenDangNhap == lhp.MaGV));
                    if (gv != null) gvName = $"{gv.HoTen} ({lhp.MaGV})";
                }

                var enrolledStudents = await db.DangKyHocPhans.AsNoTracking()
                    .Include(d => d.SinhVien)
                    .Where(d => d.MaLHP == maLhp && d.TrangThai == "DangHoc" && d.SinhVien != null && !d.SinhVien.IsDeleted)
                    .OrderBy(d => d.MaSV)
                    .Select(d => $"{d.MaSV} — {d.SinhVien!.HoTen} ({d.SinhVien.LopSinhHoat ?? "--"})")
                    .ToListAsync();

                int tc = (lhp.MonHoc?.SoTinChiLT ?? 0) + (lhp.MonHoc?.SoTinChiTH ?? 0);

                ClassXRay = new SpotlightClassXRayDto
                {
                    MaLHP = lhp.MaLHP,
                    MaMon = lhp.MaMon,
                    TenMon = lhp.MonHoc?.TenMon ?? lhp.MaMon,
                    MaHocKy = lhp.MaHocKy,
                    SoTinChi = tc,
                    GiangVien = gvName,
                    LichHocText = FormatLichHoc(lhp.Thu, lhp.TietBatDau, lhp.SoTiet, lhp.Phong),
                    PhongHoc = lhp.Phong ?? "Chưa xếp phòng",
                    SiSoHienTai = enrolledStudents.Count,
                    SiSoToiDa = lhp.SiSoToiDa ?? 50,
                    TopStudentsPreview = enrolledStudents.Take(6).ToList()
                };
            }
            else
            {
                StudentXRay = null;
                ClassXRay = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadXRay Error]: {ex.Message}");
        }
    }

    private CommandPaletteItemViewModel CreateStudentPaletteItem(
        string maSv, string hoTen, string? lopSh, string? khoaHoc, string badgeText, string badgeStatus, string subtitle)
    {
        return new CommandPaletteItemViewModel
        {
            Id = $"sv-{maSv}",
            Title = hoTen,
            Subtitle = subtitle,
            CategoryLabel = "SINH VIÊN",
            BadgeText = badgeText,
            BadgeStatus = badgeStatus,
            Icon = MaterialIconKind.AccountSchool,
            ShortcutHint = "Enter ↵ ĐKHP",
            ItemKind = CommandPaletteItemKind.Student,
            EntityKey = maSv,
            SecondaryKey = lopSh,
            ExecuteAsync = () =>
            {
                IsCommandPaletteOpen = false;
                WeakReferenceMessenger.Default.Send(new NavigateToRegistrationMessage(maSv));
                return Task.CompletedTask;
            }
        };
    }

    private CommandPaletteItemViewModel CreateClassPaletteItem(
        string maLhp, string maMon, string tenMon, string maHocKy, string badgeText, string badgeStatus, string lichHoc)
    {
        return new CommandPaletteItemViewModel
        {
            Id = $"lhp-{maLhp}",
            Title = $"{maLhp} — {tenMon}",
            Subtitle = $"Mã môn: {maMon} • Học kỳ: {maHocKy} • {lichHoc}",
            CategoryLabel = "LỚP HỌC PHẦN",
            BadgeText = badgeText,
            BadgeStatus = badgeStatus,
            Icon = MaterialIconKind.GoogleClassroom,
            ShortcutHint = "Enter ↵ Smart Import",
            ItemKind = CommandPaletteItemKind.ClassSection,
            EntityKey = maLhp,
            SecondaryKey = maHocKy,
            ExecuteAsync = () =>
            {
                IsCommandPaletteOpen = false;
                WeakReferenceMessenger.Default.Send(new NavigateToImportExcelMessage(maHocKy, maLhp));
                return Task.CompletedTask;
            }
        };
    }

    private IEnumerable<CommandPaletteItemViewModel> GetSmartQueryTemplates()
    {
        yield return new CommandPaletteItemViewModel
        {
            Id = "sq-unpaid",
            Title = "⚡ Truy vấn AI: Tìm sinh viên còn nợ học phí học kỳ này",
            Subtitle = "Quét công nợ thời gian thực, xếp hạng theo số tiền còn nợ từ cao xuống thấp",
            CategoryLabel = "TRUY VẤN THÔNG MINH",
            BadgeText = "Smart Query",
            BadgeStatus = "Warning",
            Icon = MaterialIconKind.Sparkles,
            ShortcutHint = "Enter ↵ Phân tích",
            ItemKind = CommandPaletteItemKind.SmartQuery,
            DetailedDescription = "Tự động lọc toàn bộ sinh viên có dư nợ học phí > 0đ trong học kỳ đang mở. Bạn có thể xem ngay bảng X-Ray công nợ ở cột bên phải và nhảy thẳng tới màn hình thu tiền.",
            ExecuteAsync = () =>
            {
                PaletteQuery = "!no-hoc-phi";
                return Task.CompletedTask;
            }
        };

        yield return new CommandPaletteItemViewModel
        {
            Id = "sq-capacity",
            Title = "🔥 Truy vấn AI: Các lớp học phần sắp đầy / quá tải sĩ số",
            Subtitle = "Phân tích tỷ lệ lấp đầy phòng học thời gian thực để điều phối hoặc mở thêm lớp",
            CategoryLabel = "TRUY VẤN THÔNG MINH",
            BadgeText = "Capacity Radar",
            BadgeStatus = "Danger",
            Icon = MaterialIconKind.ChartBar,
            ShortcutHint = "Enter ↵ Phân tích",
            ItemKind = CommandPaletteItemKind.SmartQuery,
            DetailedDescription = "Xếp hạng các lớp học phần trong kỳ hiện hành theo tỷ lệ lấp đầy (Sĩ số hiện tại / Sĩ số tối đa) từ cao nhất xuống thấp nhất.",
            ExecuteAsync = () =>
            {
                PaletteQuery = "!lop-sap-day";
                return Task.CompletedTask;
            }
        };

        yield return new CommandPaletteItemViewModel
        {
            Id = "sq-zero-credit",
            Title = "⚠️ Truy vấn AI: Sinh viên chưa đăng ký môn học nào (0 Tín chỉ)",
            Subtitle = "Phát hiện sớm danh sách sinh viên chưa phát sinh tín chỉ trong học kỳ hiện hành",
            CategoryLabel = "TRUY VẤN THÔNG MINH",
            BadgeText = "Academic Alert",
            BadgeStatus = "Info",
            Icon = MaterialIconKind.AccountSearchOutline,
            ShortcutHint = "Enter ↵ Phân tích",
            ItemKind = CommandPaletteItemKind.SmartQuery,
            DetailedDescription = "Đối chiếu toàn bộ danh sách sinh viên đang hoạt động với bảng Đăng ký học phần kỳ hiện tại để tìm ra những sinh viên đang có 0 tín chỉ.",
            ExecuteAsync = () =>
            {
                PaletteQuery = "!chua-dang-ky";
                return Task.CompletedTask;
            }
        };
    }

    private IEnumerable<CommandPaletteItemViewModel> GetQuickActions()
    {
        yield return new CommandPaletteItemViewModel
        {
            Id = "act-smart-import",
            Title = "Nạp sinh viên thông minh từ Clipboard (Smart Import Studio)",
            Subtitle = "Dán trực tiếp vùng dữ liệu copy từ Excel / Google Sheets / Zalo và kiểm tra trùng lịch",
            CategoryLabel = "LỆNH THỰC THI",
            BadgeText = "Ctrl + V Studio",
            BadgeStatus = "Primary",
            Icon = MaterialIconKind.ContentPaste,
            ShortcutHint = "Enter ↵ Mở ngay",
            ItemKind = CommandPaletteItemKind.Action,
            DetailedDescription = "Mở ngay Smart Import Studio để dán danh sách sinh viên từ bộ nhớ tạm, tự động nhận diện cột MSSV, Họ tên, Lớp sinh hoạt và kiểm tra xung đột thời khóa biểu.",
            ExecuteAsync = () =>
            {
                IsCommandPaletteOpen = false;
                WeakReferenceMessenger.Default.Send(new NavigateToImportExcelMessage());
                return Task.CompletedTask;
            }
        };

        yield return new CommandPaletteItemViewModel
        {
            Id = "act-git-backup",
            Title = "Tạo điểm sao lưu Git tức thì (Time-Travel Snapshot)",
            Subtitle = "Ghi nhận trạng thái CSDL SQLite hiện tại vào kho lưu trữ Git nội bộ (LibGit2Sharp)",
            CategoryLabel = "LỆNH THỰC THI",
            BadgeText = "Git Snapshot",
            BadgeStatus = "Success",
            Icon = MaterialIconKind.ClipboardCheckOutline,
            ShortcutHint = "Enter ↵ Sao lưu",
            ItemKind = CommandPaletteItemKind.Action,
            DetailedDescription = "Kích hoạt cơ chế sao lưu vi sai Canonical JSONL + SQLite Hot Snapshot ngay lập tức mà không cần rời màn hình đang làm việc.",
            ExecuteAsync = async () =>
            {
                try
                {
                    PaletteStatusToast = "⏳ Đang tạo điểm sao lưu Git toàn hệ thống...";
                    if (App.ServiceProvider != null)
                    {
                        using var scope = App.ServiceProvider.CreateScope();
                        var backupSvc = scope.ServiceProvider.GetRequiredService<IBackupService>();
                        await backupSvc.CreateLocalGitBackupAsync("Spotlight-InstantSnapshot", skipIfNoChanges: false);
                        PaletteStatusToast = "✔ Đã tạo bản sao lưu Git (Time-Travel Snapshot) thành công!";
                    }
                }
                catch (Exception ex)
                {
                    PaletteStatusToast = $"⚠ Không thể tạo bản sao lưu: {ex.Message}";
                }
            }
        };

        yield return new CommandPaletteItemViewModel
        {
            Id = "act-toggle-theme",
            Title = IsDarkMode ? "Chuyển sang Giao diện Sáng (Light Mode)" : "Chuyển sang Giao diện Tối (Dark Mode)",
            Subtitle = "Thay đổi bảng màu hiển thị với hiệu ứng chuyển cảnh mượt 300ms",
            CategoryLabel = "LỆNH THỰC THI",
            BadgeText = IsDarkMode ? "Light UI" : "Dark UI",
            BadgeStatus = "Info",
            Icon = MaterialIconKind.Sparkles,
            ShortcutHint = "Enter ↵ Đổi màu",
            ItemKind = CommandPaletteItemKind.Action,
            DetailedDescription = "Chuyển đổi tức thì giữa chế độ Sáng (Emerald Mint) và chế độ Tối (Deep Obsidian) bảo vệ mắt khi làm việc ban đêm.",
            ExecuteAsync = () =>
            {
                IsDarkMode = !IsDarkMode;
                PaletteStatusToast = IsDarkMode ? "🌙 Đã chuyển sang chế độ Giao diện Tối!" : "☀️ Đã chuyển sang chế độ Giao diện Sáng!";
                return Task.CompletedTask;
            }
        };

        yield return new CommandPaletteItemViewModel
        {
            Id = "act-refresh-cache",
            Title = "Đồng bộ & Làm mới Bộ nhớ đệm RAM (0ms Cache Reload)",
            Subtitle = "Tải lại danh mục Học kỳ, Môn học, Lớp sinh hoạt vào bộ nhớ tốc độ cao",
            CategoryLabel = "LỆNH THỰC THI",
            BadgeText = "RAM Cache",
            BadgeStatus = "Primary",
            Icon = MaterialIconKind.Refresh,
            ShortcutHint = "Enter ↵ Làm mới",
            ItemKind = CommandPaletteItemKind.Action,
            DetailedDescription = "Làm tươi toàn bộ dữ liệu trong IMemoryCacheStore và xóa cache các màn hình để nạp lại số liệu mới nhất từ SQLite.",
            ExecuteAsync = async () =>
            {
                PaletteStatusToast = "⏳ Đang làm mới bộ nhớ đệm RAM...";
                if (App.ServiceProvider != null)
                {
                    var cache = App.ServiceProvider.GetService<IMemoryCacheStore>();
                    if (cache != null)
                    {
                        await cache.InitializeAsync(forceReload: true);
                    }
                }
                _viewModelCache.Clear();
                if (SelectedMenuItem != null)
                {
                    NavigateTo(SelectedMenuItem);
                }
                PaletteStatusToast = "✔ Đã đồng bộ và làm mới toàn bộ Cache thành công!";
            }
        };
    }

    private static string FormatLichHoc(int? thu, int? tietBd, int? soTiet, string? phong)
    {
        if (!thu.HasValue || !tietBd.HasValue || !soTiet.HasValue)
            return "Chưa xếp lịch";
        string thuStr = thu.Value == 8 ? "Chủ nhật" : $"Thứ {thu.Value}";
        int tietKt = tietBd.Value + soTiet.Value - 1;
        string phongStr = string.IsNullOrWhiteSpace(phong) ? "" : $" • Phòng {phong}";
        return $"{thuStr} (Tiết {tietBd.Value}–{tietKt}){phongStr}";
    }

    private void NavigateToHome()
    {
        var homeMenu = FilteredMenuItems.FirstOrDefault(m => m.ChucNang == ChucNang.XemDashboard);
        if (homeMenu != null)
        {
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
    public event EventHandler? LogoutRequested;

    [RelayCommand]
    private void DangXuat()
    {
        _viewModelCache.Clear();
        _currentUserService.ClearCurrentUser();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Điều hướng tới màn hình tương ứng với mục menu.
    /// Sử dụng cache để tái sử dụng ViewModel đã khởi tạo — điều hướng quay lại gần như tức thời.
    /// </summary>
    private void NavigateTo(MenuItemViewModel menuItem)
    {
        if (menuItem == null) return;

        if (_viewModelCache.TryGetValue(menuItem.ChucNang, out var cachedVm))
        {
            CurrentViewModel = cachedVm;
            return;
        }

        try
        {
            ObservableObject? newVm = menuItem.ChucNang switch
            {
                ChucNang.XemDashboard         => CreateDashboardViewModel(),
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
        catch (Exception ex)
        {
            Console.WriteLine($"=== LỖI ĐIỀU HƯỚNG / KHỞI TẠO VIEWMODEL [{menuItem.ChucNang}] ===");
            Console.WriteLine(ex.ToString());
        }
    }

    private DashboardViewModel CreateDashboardViewModel()
    {
        var vm = _dashboardViewModelFactory();
        vm.NavigationRequested += OnDashboardNavigationRequested;
        return vm;
    }

    private void OnDashboardNavigationRequested(object? sender, string chucNang)
    {
        var target = FilteredMenuItems.FirstOrDefault(m => m.ChucNang == chucNang);
        if (target != null) SelectedMenuItem = target;
    }
}
