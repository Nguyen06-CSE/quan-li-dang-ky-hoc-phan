// src/QuanLyDKHP.App/ViewModels/SinhVienViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class SinhVienViewModel : ObservableObject
{
    private readonly ISinhVienService _sinhVienService;
    private readonly ICurrentUserService _currentUserService;
    private readonly Timer _debounceTimer;
    private bool _dangKhoiTao = true;

    // Bộ nhớ đệm chứa toàn bộ danh sách gốc (Master List)
    private List<SinhVienDto> _masterList = new();

    [ObservableProperty]
    private string? _tuKhoa;

    [ObservableProperty]
    private string? _lopFilter = "Tất cả";

    [ObservableProperty]
    private string? _khoaHocFilter = "Tất cả";

    [ObservableProperty]
    private int _totalItems;

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

    public ObservableCollection<string> DsLopSinhHoat { get; } = new();
    public ObservableCollection<string> DsKhoaHoc { get; } = new();

    private readonly IExcelExportService _excelExportService;
    private readonly IMemoryCacheStore _cacheStore;

    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    public SinhVienViewModel(ISinhVienService sinhVienService, ICurrentUserService currentUserService, IExcelExportService excelExportService, IMemoryCacheStore cacheStore)
    {
        _sinhVienService = sinhVienService;
        _currentUserService = currentUserService;
        _excelExportService = excelExportService;
        _cacheStore = cacheStore;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudSinhVien, _currentUserService.CurrentUser.Role);

        // Timer lọc dữ liệu sau khi người dùng ngừng gõ 300ms
        _debounceTimer = new Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => ApplyFiltersInMemory();

        DsLopSinhHoat = new ObservableCollection<string> { "Tất cả" };
        DsKhoaHoc = new ObservableCollection<string> { "Tất cả" };

        _ = InitFiltersAndLoadMasterDataAsync();
    }

    public SinhVienViewModel()
    {
        _sinhVienService = null!;
        _currentUserService = null!;
        _excelExportService = null!;
        _cacheStore = null!;
        _debounceTimer = new Timer(300);
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

            DsLopSinhHoat.Clear();
            DsLopSinhHoat.Add("Tất cả");
            foreach (var l in _cacheStore.DanhSachLopSinhHoat) DsLopSinhHoat.Add(l);

            DsKhoaHoc.Clear();
            DsKhoaHoc.Add("Tất cả");
            foreach (var k in _cacheStore.DanhSachKhoaHoc) DsKhoaHoc.Add(k);

            // Load TOÀN BỘ dữ liệu 1 lần duy nhất từ DB (Set pageSize cực lớn, ví dụ 100.000)
            // Lời khuyên: Về sau bạn nên tạo 1 hàm GetAll() trả về thẳng List<SinhVienDto> ở Repository
            var result = await _sinhVienService.TimKiemAsync(null, null, null, 1, 100000);
            _masterList = result.Items.ToList();

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

    // Các Trigger khi người dùng tương tác UI
    partial void OnTuKhoaChanged(string? value) => TriggerDebounce();
    partial void OnLopFilterChanged(string? value) => TriggerDebounce();
    partial void OnKhoaHocFilterChanged(string? value) => TriggerDebounce();

    private void TriggerDebounce()
    {
        if (_dangKhoiTao) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    // Hàm lọc in-memory siêu tốc
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
                query = query.Where(s => s.MaSV.ToLower().Contains(key) || s.HoTen.ToLower().Contains(key));
            }

            if (!string.IsNullOrWhiteSpace(LopFilter) && LopFilter != "Tất cả")
            {
                query = query.Where(s => s.LopSinhHoat == LopFilter);
            }

            if (!string.IsNullOrWhiteSpace(KhoaHocFilter) && KhoaHocFilter != "Tất cả")
            {
                query = query.Where(s => s.KhoaHoc == KhoaHocFilter);
            }

            var resultList = query.ToList();

            // Cập nhật lại UI trên Main Thread
            Dispatcher.UIThread.Invoke(() =>
            {
                // Gán mới toàn bộ collection giúp UI update 1 lần duy nhất, siêu mượt
                DanhSach = new ObservableCollection<SinhVienDto>(resultList);
                TotalItems = resultList.Count;
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
    private async Task SuaAsync(SinhVienDto dto)
    {
        if (ShowEditDialogFunc == null) return;
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
    private async Task XoaAsync(SinhVienDto dto)
    {
        if (ShowConfirmDeleteFunc == null) return;
        bool confirm = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn xóa sinh viên {dto.HoTen} ({dto.MaSV})?");
        if (confirm)
        {
            try
            {
                await _sinhVienService.XoaAsync(dto.MaSV);
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
        if (_excelExportService == null || SaveFileDialogFunc == null) return;

        try
        {
            var ds = await _sinhVienService.LayDanhSachAsync(TuKhoa, LopFilter, KhoaHocFilter);
            if (ds.Count == 0)
            {
                ShowMessage("Không có dữ liệu sinh viên để xuất Excel.", true);
                return;
            }

            var cotMap = new List<(string TieuDe, Func<SinhVien, object?> LayGiaTri)>
            {
                ("Mã SV", new Func<SinhVien, object?>(x => x.MaSV)),
                ("Họ và tên", new Func<SinhVien, object?>(x => x.HoTen)),
                ("Lớp sinh hoạt", new Func<SinhVien, object?>(x => x.LopSinhHoat ?? string.Empty)),
                ("Khóa học", new Func<SinhVien, object?>(x => x.KhoaHoc ?? string.Empty))
            };

            byte[] bytes = await _excelExportService.XuatExcelAsync<SinhVien>("SinhVien", ds, cotMap);
            string suggestedName = $"DanhSachSinhVien_{System.DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string? saved = await SaveFileDialogFunc(suggestedName, "xlsx", bytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất danh sách Sinh viên thành công tại: {saved}", false);
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
        }
        else
        {
            ShowMessage("Mô-đun Import Excel đang được tích hợp (015-Spec).", false);
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}