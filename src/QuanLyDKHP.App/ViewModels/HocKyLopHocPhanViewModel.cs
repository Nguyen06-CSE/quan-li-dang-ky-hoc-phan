// src/QuanLyDKHP.App/ViewModels/HocKyLopHocPhanViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;                       // ✅ THÊM MỚI
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
    private readonly IMemoryCacheStore _cacheStore;
    private readonly Timer _debounceTimer;

    // ✅ THÊM MỚI: chặn debounce trong lúc khởi tạo
    private bool _dangKhoiTao = true;

    // ✅ THÊM MỚI: cache TOÀN BỘ LHP của TẤT CẢ học kỳ (dạng display DTO đã enrich)
    private List<LopHocPhanDisplayDto> _masterLhpList = new();

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

    // ✅ THÊM MỚI: cờ loading cho ProgressBar
    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<HocKy> DanhSachHocKy { get; } = new();
    public ObservableCollection<MonHoc> DanhSachMonHocFilter { get; } = new();

    // ✅ ĐỔI MỚI QUAN TRỌNG: chuyển từ { get; } sang [ObservableProperty]
    //    → cho phép gán mới TOÀN BỘ collection 1 lần (nhanh hơn .Clear()/.Add() từng dòng)
    [ObservableProperty]
    private ObservableCollection<LopHocPhanDisplayDto> _danhSachLHP = new();

    public HocKyLopHocPhanViewModel(
        IHocKyService hocKyService,
        ILopHocPhanService lhpService,
        IMonHocService monHocService,
        INguoiDungRepository nguoiDungRepository,
        ICurrentUserService currentUserService,
        IMemoryCacheStore cacheStore)
    {
        _hocKyService = hocKyService;
        _lhpService = lhpService;
        _monHocService = monHocService;
        _nguoiDungRepository = nguoiDungRepository;
        _currentUserService = currentUserService;
        _cacheStore = cacheStore;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudHocKyLopHocPhan, _currentUserService.CurrentUser.Role);

        // ✅ ĐỔI MỚI: timer giờ gọi hàm lọc in-memory, KHÔNG query DB nữa
        _debounceTimer = new Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => ApplyLhpFiltersInMemory();

        _ = InitDataAsync();
    }

    public HocKyLopHocPhanViewModel()
    {
        _hocKyService = null!;
        _lhpService = null!;
        _monHocService = null!;
        _nguoiDungRepository = null!;
        _currentUserService = null!;
        _cacheStore = null!;
        _debounceTimer = new Timer(300);
    }

    // ✅ THÊM MỚI: Init data load 1 lần duy nhất (học kỳ + môn filter + master LHP)
    private async Task InitDataAsync()
    {
        if (_hocKyService == null) return;
        try
        {
            IsLoading = true;

            if (!_cacheStore.IsInitialized)
                await _cacheStore.InitializeAsync();

            LoadDanhSachHocKyFromCache();
            LoadDanhSachMonFilterFromCache();

            // ✅ THÊM MỚI: load toàn bộ LHP 1 lần vào cache
            await LoadMasterLhpAsync();

            _dangKhoiTao = false;

            // Áp dụng filter ngay lần đầu
            ApplyLhpFiltersInMemory();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
            IsLoading = false;
        }
    }

    public async Task LoadDanhSachHocKyAsync()
    {
        await _cacheStore.InitializeAsync(forceReload: true);
        LoadDanhSachHocKyFromCache();

        // ✅ ĐỔI MỚI: sau khi reload học kỳ, cũng phải reload master LHP
        //             (vì có thể có HK mới được thêm)
        await LoadMasterLhpAsync();
        ApplyLhpFiltersInMemory();
    }

    private void LoadDanhSachHocKyFromCache()
    {
        var hks = _cacheStore.DanhSachHocKy;
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

    private void LoadDanhSachMonFilterFromCache()
    {
        var mons = _cacheStore.DanhSachMonHoc;
        DanhSachMonHocFilter.Clear();
        // Item rỗng để chọn "Tất cả môn học"
        DanhSachMonHocFilter.Add(new MonHoc { MaMon = string.Empty, TenMon = "-- Tất cả môn học --" });
        foreach (var m in mons)
        {
            DanhSachMonHocFilter.Add(m);
        }
        SelectedMonFilter = DanhSachMonHocFilter.First();
    }

    // ✅ THÊM MỚI: Load TOÀN BỘ LHP của mọi học kỳ vào cache 1 lần
    // - Enrich sẵn: tên giảng viên, sĩ số đăng ký, tên môn
    private async Task LoadMasterLhpAsync()
    {
        try
        {
            // 1. Load giảng viên 1 lần, build dictionary
            var giangViens = await _nguoiDungRepository.LayGiangVienAsync();
            var gvDict = giangViens.ToDictionary(g => g.TenDangNhap, g => g.HoTen);

            // 2. Load ALL LHP của tất cả học kỳ
            var allLhps = new List<LopHocPhan>();
            foreach (var hk in DanhSachHocKy)
            {
                var lhps = await _lhpService.LayTheoHocKyAsync(hk.MaHocKy, null, null);
                allLhps.AddRange(lhps);
            }

            // 3. Build display list (enrich 1 lần duy nhất, không lặp lại khi filter)
            var displayList = new List<LopHocPhanDisplayDto>(allLhps.Count);
            foreach (var lhp in allLhps)
            {
                int siSo = await _lhpService.DemSiSoDangKyAsync(lhp.MaLHP);
                string gvName = !string.IsNullOrEmpty(lhp.MaGV) && gvDict.TryGetValue(lhp.MaGV, out var name)
                    ? $"{name} ({lhp.MaGV})"
                    : "Chưa phân công";

                displayList.Add(new LopHocPhanDisplayDto
                {
                    MaLHP = lhp.MaLHP,
                    MaMon = lhp.MaMon,
                    TenMon = lhp.MonHoc?.TenMon ?? lhp.MaMon,
                    MaGV = lhp.MaGV,
                    TenGiangVien = gvName,
                    LoaiHinhDT = lhp.LoaiHinhDT,
                    SiSoToiDa = lhp.SiSoToiDa,
                    SiSoDangKy = siSo,
                    GiangDayOnline = lhp.GiangDayOnline,
                    Entity = lhp
                });
            }

            _masterLhpList = displayList;
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải danh sách LHP: {ex.Message}", true);
        }
    }

    // ✅ ĐỔI MỚI: các partial method giờ chỉ trigger debounce, KHÔNG query DB
    partial void OnSelectedHocKyChanged(HocKy? value) => TriggerDebounce();
    partial void OnTuKhoaLHPChanged(string? value) => TriggerDebounce();
    partial void OnSelectedMonFilterChanged(MonHoc? value) => TriggerDebounce();

    private void TriggerDebounce()
    {
        if (_dangKhoiTao) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    // ✅ THÊM MỚI: hàm lọc in-memory chạy trên Background Thread
    private void ApplyLhpFiltersInMemory()
    {
        Task.Run(() =>
        {
            Dispatcher.UIThread.Invoke(() => IsLoading = true);

            // Nếu chưa chọn học kỳ → trả về list rỗng
            if (SelectedHocKy == null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    DanhSachLHP = new ObservableCollection<LopHocPhanDisplayDto>();
                    IsLoading = false;
                });
                return;
            }

            var maHocKy = SelectedHocKy.MaHocKy;
            var query = _masterLhpList.Where(x => x.Entity?.MaHocKy == maHocKy);

            // --- Lọc theo từ khóa (Mã LHP HOẶC Tên môn) ---
            if (!string.IsNullOrWhiteSpace(TuKhoaLHP))
            {
                var key = TuKhoaLHP.Trim().ToLower();
                query = query.Where(x =>
                    (x.MaLHP ?? string.Empty).ToLower().Contains(key) ||
                    (x.TenMon ?? string.Empty).ToLower().Contains(key));
            }

            // --- Lọc theo môn học ---
            if (SelectedMonFilter != null && !string.IsNullOrWhiteSpace(SelectedMonFilter.MaMon))
            {
                query = query.Where(x => x.MaMon == SelectedMonFilter.MaMon);
            }

            var resultList = query.ToList();

            // --- Cập nhật UI trên Main Thread (gán mới 1 lần, siêu mượt) ---
            Dispatcher.UIThread.Invoke(() =>
            {
                DanhSachLHP = new ObservableCollection<LopHocPhanDisplayDto>(resultList);
                IsLoading = false;
            });
        });
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

                // ✅ ĐỔI MỚI: reload học kỳ + master LHP + filter
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

                // ✅ ĐỔI MỚI: reload master LHP + filter (không query lại per-item)
                await LoadMasterLhpAsync();
                ApplyLhpFiltersInMemory();
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

                // ✅ ĐỔI MỚI
                await LoadMasterLhpAsync();
                ApplyLhpFiltersInMemory();
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

                // ✅ ĐỔI MỚI
                await LoadMasterLhpAsync();
                ApplyLhpFiltersInMemory();
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