// src/QuanLyDKHP.App/ViewModels/MonHocViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;                        // ✅ THÊM MỚI: cho StringBuilder/Encoding khi cần
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;                  // ✅ để dispatch UI thread
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia;

namespace QuanLyDKHP.App.ViewModels;

public partial class MonHocViewModel : ObservableObject
{
    private readonly IMonHocService _monHocService;
    private readonly ICurrentUserService _currentUserService;
    private readonly Timer _debounceTimer;

    // ✅ Chặn debounce trong lúc khởi tạo để tránh gọi lọc khi _masterList còn rỗng
    private bool _dangKhoiTao = true;

    // ✅ Bộ nhớ đệm chứa TOÀN BỘ dữ liệu gốc trên RAM
    private List<MonHocDto> _masterList = new();

    // ✅ Comparer tiếng Việt (xử lý dấu đúng thứ tự alphabet)
    private static readonly StringComparer ViComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), ignoreCase: true);

    [ObservableProperty]
    private string? _tuKhoa;

    // ✅ Dùng SelectedSortIndex (int) để bind thẳng ComboBox
    [ObservableProperty]
    private int _selectedSortIndex = 0;

    [ObservableProperty]
    private bool _canThemSuaXoa;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    // ✅ Cờ báo hiệu cho ProgressBar
    [ObservableProperty]
    private bool _isLoading;

    // ✅ Cho phép gán mới TOÀN BỘ collection 1 lần (nhanh hơn .Add() từng dòng)
    [ObservableProperty]
    private ObservableCollection<MonHocDto> _danhSach = new();

    // ============================================================
    // ✅ Quản lý chọn hàng loạt (Bulk Selection)
    // ============================================================
    private bool _isUpdatingSelection;

    [ObservableProperty]
    private bool? _isAllSelected = false;

    [ObservableProperty]
    private int _selectedCount;

    public bool HasSelectedItems => SelectedCount > 0;

    // Lệnh cập nhật số lượng khi tick từng dòng
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

    // Toggle khi click checkbox chọn tất cả ở Header
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
    private async Task SaoChepCacMaMonDaChonAsync()
    {
        var cacMaMon = DanhSach.Where(x => x.IsSelected).Select(x => x.MaMon).ToList();
        if (cacMaMon.Count == 0) return;

        var text = string.Join(", ", cacMaMon);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            && desktop.MainWindow?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
            ShowMessage($"Đã sao chép {cacMaMon.Count} mã môn vào bộ nhớ tạm", false);
        }
    }

    [RelayCommand]
    private async Task XoaCacMonDaChonAsync()
    {
        var cacMonDuocChon = DanhSach.Where(x => x.IsSelected).ToList();
        if (cacMonDuocChon.Count == 0 || !CanThemSuaXoa) return;

        if (ShowConfirmDeleteFunc != null)
        {
            bool confirm = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn xóa {cacMonDuocChon.Count} môn học đã chọn?");
            if (!confirm) return;
        }

        try
        {
            foreach (var mon in cacMonDuocChon)
            {
                await _monHocService.XoaAsync(mon.MaMon);
                await _syncService.RemoveLocalEntityAsync<MonHoc>(mon.MaMon);
            }
            ShowMessage($"Đã xóa {cacMonDuocChon.Count} môn học thành công.", false);
            await InitMasterDataAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }
    // ============================================================
    // Services & Delegate dialog
    // ============================================================

    private readonly IExcelExportService _excelExportService;
    private readonly ILocalReadService _localReadService;
    private readonly ISyncService _syncService;

    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    public MonHocViewModel(
        IMonHocService monHocService,
        ICurrentUserService currentUserService,
        IExcelExportService excelExportService,
        ILocalReadService localReadService,
        ISyncService syncService)
    {
        try
        {
            _monHocService = monHocService;
            _currentUserService = currentUserService;
            _excelExportService = excelExportService;
            _localReadService = localReadService;
            _syncService = syncService;

            CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                           PermissionMatrix.HasPermission(ChucNang.CrudMonHoc, _currentUserService.CurrentUser.Role);

            // Timer giờ gọi hàm lọc in-memory (không gọi DB nữa)
            _debounceTimer = new Timer(300) { AutoReset = false };
            _debounceTimer.Elapsed += (s, e) => ApplyFiltersInMemory();

            _ = InitMasterDataAsync();
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine("=== LỖI KHỞI TẠO MONHOCVIEWMODEL ===");
            System.Console.WriteLine(ex.ToString());
            throw;
        }
    }

    public MonHocViewModel()
    {
        _monHocService = null!;
        _currentUserService = null!;
        _excelExportService = null!;
        _localReadService = null!;
        _syncService = null!;
        _debounceTimer = new Timer(300);
    }

    // Đọc TOÀN BỘ dữ liệu môn học 0ms từ SQLite Local DB
    private async Task InitMasterDataAsync()
    {
        try
        {
            IsLoading = true;

            var items = await _localReadService.GetMonHocsLocalAsync();
            _masterList = items;

            _dangKhoiTao = false;

            // Sau khi có master list → áp dụng filter ngay để hiển thị lần đầu
            ApplyFiltersInMemory();
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải dữ liệu: {ex.Message}", true);
            IsLoading = false;
        }
    }

    // Các partial method chỉ trigger debounce, KHÔNG gọi DB
    partial void OnTuKhoaChanged(string? value) => TriggerDebounce();
    partial void OnSelectedSortIndexChanged(int value) => TriggerDebounce();

    private void TriggerDebounce()
    {
        if (_dangKhoiTao) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    // Hàm lọc in-memory chạy trên Background Thread
    private void ApplyFiltersInMemory()
    {
        Task.Run(() =>
        {
            // Bật loading trên UI thread
            Dispatcher.UIThread.Invoke(() => IsLoading = true);

            var query = _masterList.AsEnumerable();

            // --- BƯỚC 1: Lọc theo từ khóa (Mã môn HOẶC Tên môn) ---
            if (!string.IsNullOrWhiteSpace(TuKhoa))
            {
                var key = TuKhoa.Trim().ToLower();
                query = query.Where(m =>
                    (m.MaMon ?? string.Empty).ToLower().Contains(key) ||
                    (m.TenMon ?? string.Empty).ToLower().Contains(key));
            }

            // --- BƯỚC 2: Sắp xếp in-memory theo lựa chọn ComboBox ---
            query = SelectedSortIndex switch
            {
                1 => query.OrderByDescending(m => m.TenMon ?? string.Empty, ViComparer), // Z → A
                2 => query.OrderBy(m => m.MaMon ?? string.Empty, ViComparer),            // Mã môn
                _ => query.OrderBy(m => m.TenMon ?? string.Empty, ViComparer)            // A → Z
            };

            var resultList = query.ToList();

            // --- BƯỚC 3: Cập nhật UI trên Main Thread (gán mới 1 lần, siêu mượt) ---
            Dispatcher.UIThread.Invoke(() =>
            {
                DanhSach = new ObservableCollection<MonHocDto>(resultList);
                CapNhatSoLuongChon();
                IsLoading = false;
            });
        });
    }

    // ===== Các delegate dialog (giữ nguyên) =====
    public System.Func<MonHoc?, Task<MonHoc?>>? ShowEditDialogFunc { get; set; }
    public System.Func<string, Task<bool>>? ShowConfirmDeleteFunc { get; set; }
    public System.Func<Task>? ExportExcelAction { get; set; }

    [RelayCommand]
    private async Task ThemAsync()
    {
        if (ShowEditDialogFunc == null) return;
        var newMon = await ShowEditDialogFunc(null);
        if (newMon != null)
        {
            try
            {
                await _monHocService.ThemAsync(newMon);
                await _syncService.UpsertLocalEntityAsync(newMon);
                ShowMessage($"Đã thêm môn học {newMon.TenMon} ({newMon.MaMon}) thành công.", false);

                await InitMasterDataAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task SuaAsync(MonHocDto? dto)
    {
        if (dto == null || ShowEditDialogFunc == null) return;
        var existingMon = new MonHoc
        {
            MaMon = dto.MaMon,
            TenMon = dto.TenMon,
            SoTinChiLT = dto.SoTinChiLT,
            SoTinChiTH = dto.SoTinChiTH,
            BacDaoTao = dto.BacDaoTao
        };

        var updatedMon = await ShowEditDialogFunc(existingMon);
        if (updatedMon != null)
        {
            try
            {
                await _monHocService.CapNhatAsync(updatedMon);
                await _syncService.UpsertLocalEntityAsync(updatedMon);
                ShowMessage($"Đã cập nhật môn học {updatedMon.MaMon} thành công.", false);

                await InitMasterDataAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task XoaAsync(MonHocDto? dto)
    {
        if (dto == null || ShowConfirmDeleteFunc == null) return;
        bool confirm = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn xóa môn học {dto.TenMon} ({dto.MaMon})?");
        if (confirm)
        {
            try
            {
                await _monHocService.XoaAsync(dto.MaMon);
                await _syncService.RemoveLocalEntityAsync<MonHoc>(dto.MaMon);
                ShowMessage($"Đã xóa môn học {dto.MaMon} thành công.", false);

                await InitMasterDataAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (_excelExportService == null || SaveFileDialogFunc == null) return;

        try
        {
            var ds = DanhSach.ToList();
            if (ds.Count == 0)
            {
                ShowMessage("Không có dữ liệu môn học để xuất Excel.", true);
                return;
            }

            var cotMap = new List<(string TieuDe, Func<MonHocDto, object?> LayGiaTri)>
            {
                ("Mã môn", new Func<MonHocDto, object?>(x => x.MaMon)),
                ("Tên môn học", new Func<MonHocDto, object?>(x => x.TenMon)),
                ("Số TC LT", new Func<MonHocDto, object?>(x => x.SoTinChiLT)),
                ("Số TC TH", new Func<MonHocDto, object?>(x => x.SoTinChiTH)),
                ("Tổng số TC", new Func<MonHocDto, object?>(x => x.TongTinChi)),
                ("Bậc đào tạo", new Func<MonHocDto, object?>(x => x.BacDaoTao ?? string.Empty)),
                ("Số LHP đang mở", new Func<MonHocDto, object?>(x => x.SoLhpDangMo))
            };

            byte[] bytes = await _excelExportService.XuatExcelAsync<MonHocDto>("MonHoc", ds, cotMap);
            string suggestedName = $"DanhSachMonHoc_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string? saved = await SaveFileDialogFunc(suggestedName, "xlsx", bytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất danh sách Môn học thành công tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi xuất Excel: {ex.Message}", true);
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }

    [RelayCommand]
    private async Task SaoChepMaMonAsync(object? param)
    {
        string? maMon = param switch
        {
            MonHocDto dto => dto.MaMon,
            string s => s,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(maMon)) return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(maMon);
            ShowMessage($"Đã sao chép mã môn: {maMon}", false);
        }
    }
}