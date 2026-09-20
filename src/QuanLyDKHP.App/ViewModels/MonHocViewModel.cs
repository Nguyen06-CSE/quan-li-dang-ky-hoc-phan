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

public partial class MonHocViewModel : ObservableObject
{
    private readonly IMonHocService _monHocService;
    private readonly ICurrentUserService _currentUserService;
    private readonly Timer _debounceTimer;

    [ObservableProperty]
    private string? _tuKhoa;

    [ObservableProperty]
    private string _selectedSortBy = "TenMonAZ"; // TenMonAZ, TenMonZA, MaMon

    [ObservableProperty]
    private bool _canThemSuaXoa;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    public ObservableCollection<MonHocDto> DanhSach { get; } = new();

    private readonly IExcelExportService _excelExportService;

    public System.Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    public MonHocViewModel(IMonHocService monHocService, ICurrentUserService currentUserService, IExcelExportService excelExportService)
    {
        _monHocService = monHocService;
        _currentUserService = currentUserService;
        _excelExportService = excelExportService;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudMonHoc, _currentUserService.CurrentUser.Role);

        _debounceTimer = new Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => LoadDataAsync());

        _ = LoadDataAsync();
    }

    public MonHocViewModel()
    {
        _monHocService = null!;
        _currentUserService = null!;
        _excelExportService = null!;
        _debounceTimer = new Timer(300);
    }

    partial void OnTuKhoaChanged(string? value)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    partial void OnSelectedSortByChanged(string value)
    {
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (_monHocService == null) return;

        try
        {
            string sortParam = SelectedSortBy switch
            {
                "TenMonZA" => "TenMonZToA",
                "MaMon" => "MaMon",
                _ => "TenMon"
            };

            var items = await _monHocService.LayDanhSachDtoAsync(TuKhoa, sortParam);
            DanhSach.Clear();
            foreach (var item in items)
            {
                DanhSach.Add(item);
            }
        }
        catch (System.Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

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
                await LoadDataAsync();
            }
            catch (System.Exception ex)
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
                await LoadDataAsync();
            }
            catch (System.Exception ex)
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
            string sortParam = SelectedSortBy switch
            {
                "TenMonZA" => "TenMonZToA",
                "MaMon" => "MaMon",
                _ => "TenMon"
            };

            var ds = await _monHocService.LayDanhSachDtoAsync(TuKhoa, sortParam);
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
            string suggestedName = $"DanhSachMonHoc_{System.DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string? saved = await SaveFileDialogFunc(suggestedName, "xlsx", bytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất danh sách Môn học thành công tại: {saved}", false);
            }
        }
        catch (System.Exception ex)
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
