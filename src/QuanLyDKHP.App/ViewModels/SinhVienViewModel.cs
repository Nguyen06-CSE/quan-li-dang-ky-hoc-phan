using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Timers;
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

    [ObservableProperty]
    private string? _tuKhoa;

    [ObservableProperty]
    private string? _lopFilter = "Tất cả";

    [ObservableProperty]
    private string? _khoaHocFilter = "Tất cả";

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private bool _canThemSuaXoa;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    public ObservableCollection<SinhVienDto> DanhSach { get; } = new();
    public ObservableCollection<string> DsLopSinhHoat { get; } = new();
    public ObservableCollection<string> DsKhoaHoc { get; } = new();

    private readonly IExcelExportService _excelExportService;

    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    public SinhVienViewModel(ISinhVienService sinhVienService, ICurrentUserService currentUserService, IExcelExportService excelExportService)
    {
        _sinhVienService = sinhVienService;
        _currentUserService = currentUserService;
        _excelExportService = excelExportService;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudSinhVien, _currentUserService.CurrentUser.Role);

        _debounceTimer = new Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => LoadDataAsync());

        _ = InitFiltersAndLoadAsync();
    }

    public SinhVienViewModel()
    {
        _sinhVienService = null!;
        _currentUserService = null!;
        _excelExportService = null!;
        _debounceTimer = new Timer(300);
    }

    private async Task InitFiltersAndLoadAsync()
    {
        try
        {
            var lops = await _sinhVienService.GetDanhSachLopSinhHoatAsync();
            DsLopSinhHoat.Clear();
            DsLopSinhHoat.Add("Tất cả");
            foreach (var l in lops) DsLopSinhHoat.Add(l);

            var khoas = await _sinhVienService.GetDanhSachKhoaHocAsync();
            DsKhoaHoc.Clear();
            DsKhoaHoc.Add("Tất cả");
            foreach (var k in khoas) DsKhoaHoc.Add(k);

            LopFilter = "Tất cả";
            KhoaHocFilter = "Tất cả";

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khởi tạo dữ liệu: {ex.Message}", true);
        }
    }

    partial void OnTuKhoaChanged(string? value)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    partial void OnLopFilterChanged(string? value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnKhoaHocFilterChanged(string? value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (_sinhVienService == null) return;

        try
        {
            string? lop = (string.IsNullOrWhiteSpace(LopFilter) || LopFilter == "Tất cả") ? null : LopFilter;
            string? khoa = (string.IsNullOrWhiteSpace(KhoaHocFilter) || KhoaHocFilter == "Tất cả") ? null : KhoaHocFilter;

            var result = await _sinhVienService.TimKiemAsync(TuKhoa, lop, khoa, CurrentPage, PageSize);
            DanhSach.Clear();
            foreach (var item in result.Items)
            {
                DanhSach.Add(item);
            }

            TotalItems = result.TotalItems;
            TotalPages = result.TotalPages > 0 ? result.TotalPages : 1;
        }
        catch (System.Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            _ = LoadDataAsync();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            _ = LoadDataAsync();
        }
    }

    public System.Func<SinhVien?, Task<SinhVien?>>? ShowEditDialogFunc { get; set; }
    public System.Func<string, Task<bool>>? ShowConfirmDeleteFunc { get; set; }
    public System.Func<Task>? ShowImportExcelDialogFunc { get; set; }
    public System.Func<Task>? ExportExcelAction { get; set; }

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
                await LoadDataAsync();
                await InitFiltersAndLoadAsync();
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
                await LoadDataAsync();
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
                await LoadDataAsync();
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
