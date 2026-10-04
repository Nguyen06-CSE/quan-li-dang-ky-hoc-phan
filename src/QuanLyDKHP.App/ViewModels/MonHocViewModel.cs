// src/QuanLyDKHP.App/ViewModels/MonHocViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;                       // ✅ THÊM MỚI: để dispatch UI thread
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class MonHocViewModel : ObservableObject
{
    private readonly IMonHocService _monHocService;
    private readonly ICurrentUserService _currentUserService;
    private readonly Timer _debounceTimer;

    // ✅ THÊM MỚI: chặn debounce trong lúc khởi tạo để tránh gọi lọc khi _masterList còn rỗng
    private bool _dangKhoiTao = true;

    // ✅ THÊM MỚI: Bộ nhớ đệm chứa TOÀN BỘ dữ liệu gốc trên RAM
    private List<MonHocDto> _masterList = new();

    // ✅ THÊM MỚI: Comparer tiếng Việt (xử lý dấu đúng thứ tự alphabet)
    private static readonly StringComparer ViComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), ignoreCase: true);

    [ObservableProperty]
    private string? _tuKhoa;

    // ✅ ĐỔI MỚI: dùng SelectedSortIndex (int) để bind thẳng ComboBox, thay cho SelectedSortBy (string)
    //    Lý do: ComboBox trong Avalonia bind SelectedIndex dễ dàng hơn nhiều so với Tag/SelectedValue
    [ObservableProperty]
    private int _selectedSortIndex = 0;

    [ObservableProperty]
    private bool _canThemSuaXoa;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    // ✅ THÊM MỚI: cờ báo hiệu cho ProgressBar
    [ObservableProperty]
    private bool _isLoading;

    // ✅ ĐỔI MỚI QUAN TRỌNG: chuyển từ { get; } = new() sang [ObservableProperty]
    //    → cho phép gán mới TOÀN BỘ collection 1 lần (nhanh hơn .Add() từng dòng rất nhiều)
    [ObservableProperty]
    private ObservableCollection<MonHocDto> _danhSach = new();

    private readonly IExcelExportService _excelExportService;

    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    public MonHocViewModel(IMonHocService monHocService, ICurrentUserService currentUserService, IExcelExportService excelExportService)
    {
        _monHocService = monHocService;
        _currentUserService = currentUserService;
        _excelExportService = excelExportService;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudMonHoc, _currentUserService.CurrentUser.Role);

        // ✅ ĐỔI MỚI: timer giờ gọi hàm lọc in-memory (không gọi DB nữa)
        _debounceTimer = new Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => ApplyFiltersInMemory();

        _ = InitMasterDataAsync();
    }

    public MonHocViewModel()
    {
        _monHocService = null!;
        _currentUserService = null!;
        _excelExportService = null!;
        _debounceTimer = new Timer(300);
    }

    // ✅ THÊM MỚI: Load TOÀN BỘ dữ liệu 1 lần duy nhất từ DB vào _masterList
    // (thay cho LoadDataAsync cũ vừa query vừa filter phía server)
    private async Task InitMasterDataAsync()
    {
        try
        {
            IsLoading = true;

            // Truyền null cho từ khóa → lấy hết; sortParam mặc định "TenMon"
            var items = await _monHocService.LayDanhSachDtoAsync(null, "TenMon");
            _masterList = items.ToList();

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

    // ✅ ĐỔI MỚI: các partial method chỉ trigger debounce, KHÔNG gọi DB
    partial void OnTuKhoaChanged(string? value) => TriggerDebounce();
    partial void OnSelectedSortIndexChanged(int value) => TriggerDebounce();

    private void TriggerDebounce()
    {
        if (_dangKhoiTao) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    // ✅ THÊM MỚI: hàm lọc in-memory chạy trên Background Thread
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
                ShowMessage($"Đã thêm môn học {newMon.TenMon} ({newMon.MaMon}) thành công.", false);

                // ✅ ĐỔI MỚI: reload master list + lọc lại in-memory (không query DB 2 lần)
                await InitMasterDataAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task SuaAsync(MonHocDto dto)
    {
        if (ShowEditDialogFunc == null) return;
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
                ShowMessage($"Đã cập nhật môn học {updatedMon.MaMon} thành công.", false);

                // ✅ ĐỔI MỚI: reload master list
                await InitMasterDataAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task XoaAsync(MonHocDto dto)
    {
        if (ShowConfirmDeleteFunc == null) return;
        bool confirm = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn xóa môn học {dto.TenMon} ({dto.MaMon})?");
        if (confirm)
        {
            try
            {
                await _monHocService.XoaAsync(dto.MaMon);
                ShowMessage($"Đã xóa môn học {dto.MaMon} thành công.", false);

                // ✅ ĐỔI MỚI: reload master list
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
            // ✅ ĐỔI MỚI: dùng luôn DanhSach hiện tại (đã được lọc/sort in-memory) → không cần query DB lại
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
}