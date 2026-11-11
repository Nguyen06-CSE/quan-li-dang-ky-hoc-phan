// src/QuanLyDKHP.App/ViewModels/SinhVienViewModel.cs

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using QuanLyDKHP.App.Helpers;
using QuanLyDKHP.App.Messages;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class SinhVienViewModel : ObservableObject, IRecipient<DataImportedNotificationMessage>
{
    private readonly ISinhVienService _sinhVienService;
    private readonly ICurrentUserService _currentUserService;
    private readonly Timer _debounceTimer;
    private bool _dangKhoiTao = true;
    private bool _isUpdatingDropdownList;

    // Bộ nhớ đệm chứa toàn bộ danh sách gốc (Master List)
    private List<SinhVienDto> _masterList = new();
    private List<string> _allLopSinhHoat = new() { "Tất cả" };
    private List<string> _allKhoaHoc = new() { "Tất cả" };

    // Comparer tiếng Việt
    private static readonly StringComparer ViComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), ignoreCase: true);

    [ObservableProperty]
    private string? _tuKhoa;

    [ObservableProperty]
    private string? _lopFilter = "Tất cả";

    [ObservableProperty]
    private string? _khoaHocFilter = "Tất cả";

    // Từ khóa tìm nhanh bên trong dropdown Lớp sinh hoạt & Khóa học
    [ObservableProperty]
    private string? _timKiemLop;

    [ObservableProperty]
    private string? _timKiemKhoaHoc;

    // Lựa chọn sắp xếp trên Segmented Control (0: Tên A->Z, 1: Tên Z->A, 2: Mã SV, 3: Tín chỉ giảm dần)
    [ObservableProperty]
    private int _selectedSortIndex = 0;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _totalMasterItems;

    [ObservableProperty]
    private bool _canThemSuaXoa;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    // Thêm cờ trạng thái Loading
    [ObservableProperty]
    private bool _isLoading;

    // Chuyển thành ObservableProperty để gán nguyên danh sách mới 1 lần (Tối ưu tốc độ render)
    [ObservableProperty]
    private ObservableCollection<SinhVienDto> _danhSach = new();

    [ObservableProperty]
    private ObservableCollection<string> _dsLopSinhHoat = new() { "Tất cả" };

    [ObservableProperty]
    private ObservableCollection<string> _dsKhoaHoc = new() { "Tất cả" };

    public bool HasLopFilter => !string.IsNullOrWhiteSpace(LopFilter) && LopFilter != "Tất cả";
    public bool HasKhoaHocFilter => !string.IsNullOrWhiteSpace(KhoaHocFilter) && KhoaHocFilter != "Tất cả";
    public bool HasAnyFilter => !string.IsNullOrWhiteSpace(TuKhoa) || HasLopFilter || HasKhoaHocFilter;
    public string FilterSummaryText => $"Đang lọc: {TotalItems} / {TotalMasterItems}";

    // ============================================================
    // Quản lý chọn hàng loạt (Bulk Selection)
    // ============================================================
    private bool _isUpdatingSelection;

    [ObservableProperty]
    private bool? _isAllSelected = false;

    [ObservableProperty]
    private int _selectedCount;

    public bool HasSelectedItems => SelectedCount > 0;

    [RelayCommand]
    public void CapNhatSoLuongChon()
    {
        _isUpdatingSelection = true;
        try
        {
            SelectedCount = DanhSach.Count(x => x.IsSelected);
            OnPropertyChanged(nameof(HasSelectedItems));

            if (SelectedCount == 0)
                IsAllSelected = false;
            else if (SelectedCount == DanhSach.Count && DanhSach.Count > 0)
                IsAllSelected = true;
            else
                IsAllSelected = null;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    partial void OnIsAllSelectedChanged(bool? value)
    {
        if (_isUpdatingSelection || !value.HasValue) return;

        foreach (var item in DanhSach)
        {
            item.IsSelected = value.Value;
        }
        SelectedCount = value.Value ? DanhSach.Count : 0;
        OnPropertyChanged(nameof(HasSelectedItems));
    }

    [RelayCommand]
    private void BoChonTatCa()
    {
        IsAllSelected = false;
    }

    [RelayCommand]
    private async Task SaoChepCacMaSvDaChonAsync()
    {
        var cacMaSv = DanhSach.Where(x => x.IsSelected).Select(x => x.MaSV).ToList();
        if (cacMaSv.Count == 0) return;

        var text = string.Join(", ", cacMaSv);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
            ShowMessage($"Đã sao chép {cacMaSv.Count} mã sinh viên vào bộ nhớ tạm.", false);
        }
    }

    [RelayCommand]
    private async Task SaoChepMaSvAsync(object? param)
    {
        string? maSv = param switch
        {
            SinhVienDto dto => dto.MaSV,
            string s => s,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(maSv)) return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(maSv);
            ShowMessage($"Đã sao chép mã sinh viên: {maSv}", false);
        }
    }

    [RelayCommand]
    private async Task XoaCacSinhVienDaChonAsync()
    {
        var cacSvDuocChon = DanhSach.Where(x => x.IsSelected).ToList();
        if (cacSvDuocChon.Count == 0 || !CanThemSuaXoa) return;

        if (ShowConfirmDeleteFunc != null)
        {
            bool confirm = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn xóa {cacSvDuocChon.Count} sinh viên đã chọn?");
            if (!confirm) return;
        }

        try
        {
            foreach (var sv in cacSvDuocChon)
            {
                await _sinhVienService.XoaAsync(sv.MaSV);
                await _syncService.RemoveLocalEntityAsync<SinhVien>(sv.MaSV);
            }
            ShowMessage($"Đã xóa {cacSvDuocChon.Count} sinh viên thành công.", false);
            await InitFiltersAndLoadMasterDataAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private void XoaLocLop()
    {
        TimKiemLop = string.Empty;
        LopFilter = "Tất cả";
    }

    [RelayCommand]
    private void XoaLocKhoaHoc()
    {
        TimKiemKhoaHoc = string.Empty;
        KhoaHocFilter = "Tất cả";
    }

    [RelayCommand]
    private void DatLaiBoLoc()
    {
        TuKhoa = string.Empty;
        TimKiemLop = string.Empty;
        TimKiemKhoaHoc = string.Empty;
        LopFilter = "Tất cả";
        KhoaHocFilter = "Tất cả";
    }

    private readonly IExcelExportService _excelExportService;
    private readonly IMemoryCacheStore _cacheStore;
    private readonly ILocalReadService _localReadService;
    private readonly ISyncService _syncService;
    private readonly IImportExcelService? _importExcelService;

    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }
    public Func<Task<string?>>? OpenFileDialogFunc { get; set; }

    public SinhVienViewModel(
        ISinhVienService sinhVienService,
        ICurrentUserService currentUserService,
        IExcelExportService excelExportService,
        IMemoryCacheStore cacheStore,
        ILocalReadService localReadService,
        ISyncService syncService,
        IImportExcelService? importExcelService = null)
    {
        _sinhVienService = sinhVienService;
        _currentUserService = currentUserService;
        _excelExportService = excelExportService;
        _cacheStore = cacheStore;
        _localReadService = localReadService;
        _syncService = syncService;
        _importExcelService = importExcelService;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudSinhVien, _currentUserService.CurrentUser.Role);

        // Timer lọc dữ liệu sau khi người dùng ngừng gõ 250ms
        _debounceTimer = new Timer(250) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => ApplyFiltersInMemory();

        WeakReferenceMessenger.Default.RegisterAll(this);

        _ = InitFiltersAndLoadMasterDataAsync();
    }

    public void Receive(DataImportedNotificationMessage message)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await InitFiltersAndLoadMasterDataAsync();
        });
    }

    public SinhVienViewModel()
    {
        _sinhVienService = null!;
        _currentUserService = null!;
        _excelExportService = null!;
        _cacheStore = null!;
        _localReadService = null!;
        _syncService = null!;
        _debounceTimer = new Timer(250);
    }

    private async Task InitFiltersAndLoadMasterDataAsync()
    {
        try
        {
            IsLoading = true;
            if (!_cacheStore.IsInitialized)
            {
                await _cacheStore.InitializeAsync();
            }

            // Đọc 0ms từ SQLite Local DB thay vì remote Neon PostgreSQL
            var items = await _localReadService.GetSinhViensLocalAsync();
            _masterList = items;
            TotalMasterItems = items.Count;

            // Tổng hợp danh sách Lớp sinh hoạt & Khóa học từ Cache + MasterList
            var lopSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in _cacheStore.DanhSachLopSinhHoat)
            {
                if (!string.IsNullOrWhiteSpace(l)) lopSet.Add(l.Trim());
            }
            foreach (var sv in _masterList)
            {
                if (!string.IsNullOrWhiteSpace(sv.LopSinhHoat)) lopSet.Add(sv.LopSinhHoat.Trim());
            }

            var khoaSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in _cacheStore.DanhSachKhoaHoc)
            {
                if (!string.IsNullOrWhiteSpace(k)) khoaSet.Add(k.Trim());
            }
            foreach (var sv in _masterList)
            {
                if (!string.IsNullOrWhiteSpace(sv.KhoaHoc)) khoaSet.Add(sv.KhoaHoc.Trim());
            }

            _allLopSinhHoat = new List<string> { "Tất cả" };
            _allLopSinhHoat.AddRange(lopSet.OrderBy(x => x, ViComparer));

            _allKhoaHoc = new List<string> { "Tất cả" };
            _allKhoaHoc.AddRange(khoaSet.OrderBy(x => x, ViComparer));

            RefreshDropdownLopList();
            RefreshDropdownKhoaHocList();

            _dangKhoiTao = false;

            // Gọi hàm lọc (hiển thị dữ liệu lên view)
            ApplyFiltersInMemory();
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khởi tạo dữ liệu: {ex.Message}", true);
            IsLoading = false;
        }
    }

    // Lọc danh sách trong dropdown Lớp sinh hoạt khi người dùng gõ tìm kiếm (VD: "CNTT")
    partial void OnTimKiemLopChanged(string? value)
    {
        RefreshDropdownLopList();
    }

    // Lọc danh sách trong dropdown Khóa học khi người dùng gõ tìm kiếm
    partial void OnTimKiemKhoaHocChanged(string? value)
    {
        RefreshDropdownKhoaHocList();
    }

    private void RefreshDropdownLopList()
    {
        _isUpdatingDropdownList = true;
        try
        {
            var key = TimKiemLop?.Trim();
            IEnumerable<string> filtered = _allLopSinhHoat;

            if (!string.IsNullOrWhiteSpace(key))
            {
                filtered = _allLopSinhHoat.Where(x =>
                    x == "Tất cả" || x.Contains(key, StringComparison.OrdinalIgnoreCase));
            }

            DsLopSinhHoat = new ObservableCollection<string>(filtered);
        }
        finally
        {
            _isUpdatingDropdownList = false;
        }
    }

    private void RefreshDropdownKhoaHocList()
    {
        _isUpdatingDropdownList = true;
        try
        {
            var key = TimKiemKhoaHoc?.Trim();
            IEnumerable<string> filtered = _allKhoaHoc;

            if (!string.IsNullOrWhiteSpace(key))
            {
                filtered = _allKhoaHoc.Where(x =>
                    x == "Tất cả" || x.Contains(key, StringComparison.OrdinalIgnoreCase));
            }

            DsKhoaHoc = new ObservableCollection<string>(filtered);
        }
        finally
        {
            _isUpdatingDropdownList = false;
        }
    }

    // Các Trigger khi người dùng tương tác UI
    partial void OnTuKhoaChanged(string? value)
    {
        OnPropertyChanged(nameof(HasAnyFilter));
        TriggerDebounce();
    }

    partial void OnLopFilterChanged(string? value)
    {
        if (_isUpdatingDropdownList || string.IsNullOrWhiteSpace(value)) return;
        OnPropertyChanged(nameof(HasLopFilter));
        OnPropertyChanged(nameof(HasAnyFilter));
        TriggerDebounce();
    }

    partial void OnKhoaHocFilterChanged(string? value)
    {
        if (_isUpdatingDropdownList || string.IsNullOrWhiteSpace(value)) return;
        OnPropertyChanged(nameof(HasKhoaHocFilter));
        OnPropertyChanged(nameof(HasAnyFilter));
        TriggerDebounce();
    }

    partial void OnSelectedSortIndexChanged(int value) => TriggerDebounce();

    private void TriggerDebounce()
    {
        if (_dangKhoiTao) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    // Hàm lọc & sắp xếp in-memory siêu tốc
    private void ApplyFiltersInMemory()
    {
        // Chạy trên Background Thread để không làm giật khung hình giao diện khi gõ phím
        Task.Run(() =>
        {
            Dispatcher.UIThread.Invoke(() => IsLoading = true);

            var query = _masterList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TuKhoa))
            {
                var key = TuKhoa.Trim().ToLower();
                query = query.Where(s =>
                    (s.MaSV ?? string.Empty).ToLower().Contains(key) ||
                    (s.HoTen ?? string.Empty).ToLower().Contains(key) ||
                    (s.LopSinhHoat ?? string.Empty).ToLower().Contains(key));
            }

            if (!string.IsNullOrWhiteSpace(LopFilter) && LopFilter != "Tất cả")
            {
                query = query.Where(s => string.Equals(s.LopSinhHoat, LopFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(KhoaHocFilter) && KhoaHocFilter != "Tất cả")
            {
                query = query.Where(s => string.Equals(s.KhoaHoc, KhoaHocFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Sắp xếp in-memory theo lựa chọn trên Segmented Control
            query = SelectedSortIndex switch
            {
                1 => query.OrderByDescending(s => s, VietnameseNameComparer.Instance),
                2 => query.OrderBy(s => s.MaSV ?? string.Empty, ViComparer),
                3 => query.OrderByDescending(s => s.SoTinChiDangKy)
                          .ThenBy(s => s, VietnameseNameComparer.Instance),
                _ => query.OrderBy(s => s, VietnameseNameComparer.Instance)
            };

            var resultList = query.ToList();

            // Cập nhật lại UI trên Main Thread
            Dispatcher.UIThread.Invoke(() =>
            {
                // Gán mới toàn bộ collection giúp UI update 1 lần duy nhất, siêu mượt
                DanhSach = new ObservableCollection<SinhVienDto>(resultList);
                TotalItems = resultList.Count;
                OnPropertyChanged(nameof(FilterSummaryText));
                CapNhatSoLuongChon();
                IsLoading = false;
            });
        });
    }

    // Các Delegate UI
    public System.Func<SinhVien?, Task<SinhVien?>>? ShowEditDialogFunc { get; set; }
    public System.Func<string, Task<bool>>? ShowConfirmDeleteFunc { get; set; }
    public System.Func<Task>? ShowImportExcelDialogFunc { get; set; }

    [RelayCommand]
    private async Task ThemAsync()
    {
        if (ShowEditDialogFunc == null) return;
        var newSv = await ShowEditDialogFunc(null);
        if (newSv != null)
        {
            try
            {
                await _sinhVienService.ThemAsync(newSv);
                await _syncService.UpsertLocalEntityAsync(newSv);
                ShowMessage($"Đã thêm sinh viên {newSv.HoTen} ({newSv.MaSV}) thành công.", false);
                await InitFiltersAndLoadMasterDataAsync(); // Load lại Master Data
            }
            catch (System.Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task SuaAsync(SinhVienDto? dto)
    {
        if (dto == null || ShowEditDialogFunc == null) return;
        var existingSv = new SinhVien
        {
            MaSV = dto.MaSV,
            HoTen = dto.HoTen,
            LopSinhHoat = dto.LopSinhHoat,
            KhoaHoc = dto.KhoaHoc
        };

        var updatedSv = await ShowEditDialogFunc(existingSv);
        if (updatedSv != null)
        {
            try
            {
                await _sinhVienService.CapNhatAsync(updatedSv);
                await _syncService.UpsertLocalEntityAsync(updatedSv);
                ShowMessage($"Đã cập nhật sinh viên {updatedSv.MaSV} thành công.", false);
                await InitFiltersAndLoadMasterDataAsync(); // Load lại Master Data
            }
            catch (System.Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task XoaAsync(SinhVienDto? dto)
    {
        if (dto == null || ShowConfirmDeleteFunc == null) return;
        bool confirm = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn xóa sinh viên {dto.HoTen} ({dto.MaSV})?");
        if (confirm)
        {
            try
            {
                await _sinhVienService.XoaAsync(dto.MaSV);
                await _syncService.RemoveLocalEntityAsync<SinhVien>(dto.MaSV);
                ShowMessage($"Đã xóa sinh viên {dto.MaSV} thành công.", false);
                await InitFiltersAndLoadMasterDataAsync(); // Load lại Master Data
            }
            catch (System.Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        await ExportListToExcelAsync(DanhSach.ToList(), "DanhSachSinhVien");
    }

    [RelayCommand]
    private async Task ExportSelectedExcelAsync()
    {
        var selected = DanhSach.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0) return;
        await ExportListToExcelAsync(selected, "DanhSachSinhVienDaChon");
    }

    private async Task ExportListToExcelAsync(List<SinhVienDto> ds, string filePrefix)
    {
        if (_excelExportService == null || SaveFileDialogFunc == null) return;

        try
        {
            if (ds.Count == 0)
            {
                ShowMessage("Không có dữ liệu sinh viên để xuất Excel.", true);
                return;
            }

            var cotMap = new List<(string TieuDe, Func<SinhVienDto, object?> LayGiaTri)>
            {
                ("Mã SV", new Func<SinhVienDto, object?>(x => x.MaSV)),
                ("Họ và tên", new Func<SinhVienDto, object?>(x => x.HoTen)),
                ("Lớp sinh hoạt", new Func<SinhVienDto, object?>(x => x.LopSinhHoat ?? string.Empty)),
                ("Khóa học", new Func<SinhVienDto, object?>(x => x.KhoaHoc ?? string.Empty)),
                ("Số TC ĐK (HK hiện tại)", new Func<SinhVienDto, object?>(x => x.SoTinChiDangKy))
            };

            byte[] bytes = await _excelExportService.XuatExcelAsync<SinhVienDto>("SinhVien", ds, cotMap);
            string suggestedName = $"{filePrefix}_{System.DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string? saved = await SaveFileDialogFunc(suggestedName, "xlsx", bytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất {ds.Count} sinh viên ra Excel tại: {saved}", false);
            }
        }
        catch (System.Exception ex)
        {
            ShowMessage($"Lỗi xuất Excel: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        if (ShowImportExcelDialogFunc != null)
        {
            await ShowImportExcelDialogFunc();
            return;
        }

        if (OpenFileDialogFunc != null)
        {
            var path = await OpenFileDialogFunc();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

            if (_importExcelService != null)
            {
                try
                {
                    IsLoading = true;
                    ShowMessage("Đang đọc và import file Excel...", false);
                    await using var stream = File.OpenRead(path);
                    var result = await _importExcelService.ImportFileAsync(stream);
                    await InitFiltersAndLoadMasterDataAsync();
                    ShowMessage($"Import thành công: {result.SoDongThanhCong}/{result.TongSoDong} dòng ({result.SoSinhVienMoi} sinh viên mới)!", false);
                    return;
                }
                catch (Exception ex)
                {
                    ShowMessage($"Lỗi import Excel: {ex.Message}", true);
                    return;
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        WeakReferenceMessenger.Default.Send(new NavigateToImportExcelMessage());
    }

    [RelayCommand]
    private void DangKyHocPhan(SinhVienDto? sv)
    {
        if (sv == null) return;
        WeakReferenceMessenger.Default.Send(new NavigateToRegistrationMessage(sv.MaSV));
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}
