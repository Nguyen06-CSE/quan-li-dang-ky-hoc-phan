using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.App.Dtos;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class HocKyLopHocPhanViewModel : ObservableObject
{
    private readonly IHocKyService _hocKyService;
    private readonly ILopHocPhanService _lhpService;
    private readonly IMonHocService _monHocService;
    private readonly INguoiDungRepository _nguoiDungRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly Timer _debounceTimer;

    [ObservableProperty]
    private bool _canThemSuaXoa;

    [ObservableProperty]
    private HocKy? _selectedHocKy;

    [ObservableProperty]
    private string? _tuKhoaLHP;

    [ObservableProperty]
    private MonHoc? _selectedMonFilter;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    public ObservableCollection<HocKy> DanhSachHocKy { get; } = new();
    public ObservableCollection<LopHocPhanDisplayDto> DanhSachLHP { get; } = new();
    public ObservableCollection<MonHoc> DanhSachMonHocFilter { get; } = new();

    public HocKyLopHocPhanViewModel(
        IHocKyService hocKyService,
        ILopHocPhanService lhpService,
        IMonHocService monHocService,
        INguoiDungRepository nguoiDungRepository,
        ICurrentUserService currentUserService)
    {
        _hocKyService = hocKyService;
        _lhpService = lhpService;
        _monHocService = monHocService;
        _nguoiDungRepository = nguoiDungRepository;
        _currentUserService = currentUserService;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudHocKyLopHocPhan, _currentUserService.CurrentUser.Role);

        _debounceTimer = new Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => LoadLopHocPhanAsync());

        _ = InitDataAsync();
    }

    public HocKyLopHocPhanViewModel()
    {
        _hocKyService = null!;
        _lhpService = null!;
        _monHocService = null!;
        _nguoiDungRepository = null!;
        _currentUserService = null!;
        _debounceTimer = new Timer(300);
    }

    private async Task InitDataAsync()
    {
        if (_hocKyService == null) return;
        try
        {
            await LoadDanhSachHocKyAsync();
            await LoadDanhSachMonFilterAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    public async Task LoadDanhSachHocKyAsync()
    {
        var hks = await _hocKyService.LayTatCaAsync();
        DanhSachHocKy.Clear();
        foreach (var hk in hks)
        {
            DanhSachHocKy.Add(hk);
        }

        if (SelectedHocKy == null && DanhSachHocKy.Count > 0)
        {
            // Ưu tiên chọn học kỳ DangMo = true
            SelectedHocKy = DanhSachHocKy.FirstOrDefault(hk => hk.DangMo) ?? DanhSachHocKy.First();
        }
    }

    private async Task LoadDanhSachMonFilterAsync()
    {
        var mons = await _monHocService.LayDanhSachAsync(null, "TenMon");
        DanhSachMonHocFilter.Clear();
        // Item rỗng để chọn "Tất cả môn học"
        DanhSachMonHocFilter.Add(new MonHoc { MaMon = string.Empty, TenMon = "-- Tất cả môn học --" });
        foreach (var m in mons)
        {
            DanhSachMonHocFilter.Add(m);
        }
        SelectedMonFilter = DanhSachMonHocFilter.First();
    }

    partial void OnSelectedHocKyChanged(HocKy? value)
    {
        _ = LoadLopHocPhanAsync();
    }

    partial void OnTuKhoaLHPChanged(string? value)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    partial void OnSelectedMonFilterChanged(MonHoc? value)
    {
        _ = LoadLopHocPhanAsync();
    }

    [RelayCommand]
    public async Task LoadLopHocPhanAsync()
    {
        if (_lhpService == null || SelectedHocKy == null) return;

        try
        {
            string? maMon = string.IsNullOrWhiteSpace(SelectedMonFilter?.MaMon) ? null : SelectedMonFilter.MaMon;
            var lhps = await _lhpService.LayTheoHocKyAsync(SelectedHocKy.MaHocKy, TuKhoaLHP, maMon);

            var giangViens = await _nguoiDungRepository.LayGiangVienAsync();
            var gvDict = giangViens.ToDictionary(g => g.TenDangNhap, g => g.HoTen);

            DanhSachLHP.Clear();
            foreach (var lhp in lhps)
            {
                int siSoRealtime = await _lhpService.DemSiSoDangKyAsync(lhp.MaLHP);
                string gvName = !string.IsNullOrEmpty(lhp.MaGV) && gvDict.TryGetValue(lhp.MaGV, out var name)
                    ? $"{name} ({lhp.MaGV})"
                    : "Chưa phân công";

                DanhSachLHP.Add(new LopHocPhanDisplayDto
                {
                    MaLHP = lhp.MaLHP,
                    MaMon = lhp.MaMon,
                    TenMon = lhp.MonHoc?.TenMon ?? lhp.MaMon,
                    MaGV = lhp.MaGV,
                    TenGiangVien = gvName,
                    LoaiHinhDT = lhp.LoaiHinhDT,
                    SiSoToiDa = lhp.SiSoToiDa,
                    SiSoDangKy = siSoRealtime,
                    GiangDayOnline = lhp.GiangDayOnline,
                    Entity = lhp
                });
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    public Func<HocKy?, Task<HocKy?>>? ShowHocKyEditDialogFunc { get; set; }
    public Func<string, List<MonHoc>, List<NguoiDung>, LopHocPhan?, Task<LopHocPhan?>>? ShowLhpEditDialogFunc { get; set; }
    public Func<string, Task<bool>>? ShowConfirmDeleteFunc { get; set; }

    [RelayCommand]
    private async Task ThemHocKyAsync()
    {
        if (ShowHocKyEditDialogFunc == null) return;
        var newHk = await ShowHocKyEditDialogFunc(null);
        if (newHk != null)
        {
            try
            {
                await _hocKyService.ThemAsync(newHk);
                ShowMessage($"Đã thêm học kỳ {newHk.TenHocKy} ({newHk.MaHocKy}) thành công.", false);
                await LoadDanhSachHocKyAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    [RelayCommand]
    private async Task DatHocKyHienHanhAsync(HocKy hk)
    {
        if (hk == null) return;
        try
        {
            await _hocKyService.DatHocKyHienHanhAsync(hk.MaHocKy);
            ShowMessage($"Đã đặt học kỳ {hk.TenHocKy} làm học kỳ hiện hành.", false);
            await LoadDanhSachHocKyAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task ThemLhpAsync()
    {
        if (ShowLhpEditDialogFunc == null || SelectedHocKy == null) return;

        try
        {
            var mons = await _monHocService.LayDanhSachAsync(null, "TenMon");
            var gvs = await _nguoiDungRepository.LayGiangVienAsync();

            var newLhp = await ShowLhpEditDialogFunc(SelectedHocKy.MaHocKy, mons, gvs, null);
            if (newLhp != null)
            {
                await _lhpService.ThemAsync(newLhp);
                ShowMessage($"Đã thêm lớp học phần {newLhp.MaLHP} thành công.", false);
                await LoadLopHocPhanAsync();
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task SuaLhpAsync(LopHocPhanDisplayDto dto)
    {
        if (ShowLhpEditDialogFunc == null || SelectedHocKy == null || dto == null) return;

        try
        {
            var mons = await _monHocService.LayDanhSachAsync(null, "TenMon");
            var gvs = await _nguoiDungRepository.LayGiangVienAsync();

            var updatedLhp = await ShowLhpEditDialogFunc(SelectedHocKy.MaHocKy, mons, gvs, dto.Entity);
            if (updatedLhp != null)
            {
                await _lhpService.CapNhatAsync(updatedLhp);
                ShowMessage($"Đã cập nhật lớp học phần {updatedLhp.MaLHP} thành công.", false);
                await LoadLopHocPhanAsync();
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task XoaLhpAsync(LopHocPhanDisplayDto dto)
    {
        if (ShowConfirmDeleteFunc == null || dto == null) return;

        bool confirm = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn xóa lớp học phần {dto.MaLHP} - môn {dto.TenMon}?");
        if (confirm)
        {
            try
            {
                await _lhpService.XoaAsync(dto.MaLHP);
                ShowMessage($"Đã xóa lớp học phần {dto.MaLHP} thành công.", false);
                await LoadLopHocPhanAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, true);
            }
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}
