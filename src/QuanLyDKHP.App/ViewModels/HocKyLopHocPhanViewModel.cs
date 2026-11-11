// src/QuanLyDKHP.App/ViewModels/HocKyLopHocPhanViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;                       // ✅ THÊM MỚI
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDKHP.App.Dtos;
using QuanLyDKHP.App.Messages;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.App.ViewModels;

public partial class HocKyLopHocPhanViewModel : ObservableObject, IRecipient<DataImportedNotificationMessage>
{
    private readonly IHocKyService _hocKyService;
    private readonly ILopHocPhanService _lhpService;
    private readonly IMonHocService _monHocService;
    private readonly INguoiDungRepository _nguoiDungRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMemoryCacheStore _cacheStore;
    private readonly ILocalReadService _localReadService;
    private readonly ISyncService _syncService;
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
    private int _selectedSortIndex = 0;

    public bool HasAnyFilter => !string.IsNullOrWhiteSpace(TuKhoaLHP) ||
                                (SelectedMonFilter != null && !string.IsNullOrEmpty(SelectedMonFilter.MaMon));

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

    // ==================== NÂNG CẤP 2: SỔ TAY ĐIỀU PHỐI LỚP HỌC PHẦN DÀNH CHO GIÁO VIÊN ====================
    private List<ClassRosterStudentDto> _allRosterStudents = new();

    [ObservableProperty]
    private bool _isRosterDrawerOpen;

    [ObservableProperty]
    private LopHocPhanDisplayDto? _activeRosterLhp;

    [ObservableProperty]
    private string? _rosterSearchQuery;

    [ObservableProperty]
    private string? _quickAddMaSv;

    [ObservableProperty]
    private LopHocPhanDisplayDto? _targetTransferLhp;

    [ObservableProperty]
    private string _rosterDemographicsText = "Chưa có sinh viên";

    [ObservableProperty]
    private int _rosterDebtCount;

    public ObservableCollection<ClassRosterStudentDto> RosterStudents { get; } = new();
    public ObservableCollection<LopHocPhanDisplayDto> AlternativeTransferClasses { get; } = new();
    public bool HasAlternativeTransferClasses => AlternativeTransferClasses.Count > 0;

    partial void OnRosterSearchQueryChanged(string? value)
    {
        ApplyRosterSearchFilter();
    }

    public HocKyLopHocPhanViewModel(
        IHocKyService hocKyService,
        ILopHocPhanService lhpService,
        IMonHocService monHocService,
        INguoiDungRepository nguoiDungRepository,
        ICurrentUserService currentUserService,
        IMemoryCacheStore cacheStore,
        ILocalReadService localReadService,
        ISyncService syncService)
    {
        _hocKyService = hocKyService;
        _lhpService = lhpService;
        _monHocService = monHocService;
        _nguoiDungRepository = nguoiDungRepository;
        _currentUserService = currentUserService;
        _cacheStore = cacheStore;
        _localReadService = localReadService;
        _syncService = syncService;

        CanThemSuaXoa = _currentUserService.CurrentUser != null &&
                       PermissionMatrix.HasPermission(ChucNang.CrudHocKyLopHocPhan, _currentUserService.CurrentUser.Role);

        // ✅ ĐỔI MỚI: timer giờ gọi hàm lọc in-memory, KHÔNG query DB nữa
        _debounceTimer = new Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => ApplyLhpFiltersInMemory();

        WeakReferenceMessenger.Default.RegisterAll(this);

        _ = InitDataAsync();
    }

    public void Receive(DataImportedNotificationMessage message)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await InitDataAsync();
        });
    }

    public HocKyLopHocPhanViewModel()
    {
        _hocKyService = null!;
        _lhpService = null!;
        _monHocService = null!;
        _nguoiDungRepository = null!;
        _currentUserService = null!;
        _cacheStore = null!;
        _localReadService = null!;
        _syncService = null!;
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

    // ✅ ĐỌC TỪ LOCAL SQLITE: Load TOÀN BỘ LHP của mọi học kỳ vào cache 1 lần
    // - Enrich sẵn: tên giảng viên, sĩ số đăng ký (bulk count), tên môn
    private async Task LoadMasterLhpAsync()
    {
        try
        {
            // 1. Load giảng viên 1 lần, build dictionary đối chiếu cả TenDangNhap lẫn MaGV
            var giangViens = await _nguoiDungRepository.LayGiangVienAsync();
            var gvDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var gv in giangViens)
            {
                if (!string.IsNullOrWhiteSpace(gv.TenDangNhap))
                    gvDict[gv.TenDangNhap] = gv.HoTen;
                if (!string.IsNullOrWhiteSpace(gv.MaGV))
                    gvDict[gv.MaGV] = gv.HoTen;
            }

            // 2. Load ALL LHP của tất cả học kỳ từ Local SQLite DB (0ms)
            var allLhps = await _localReadService.GetLopHocPhansLocalAsync();

            // 3. Lấy sĩ số đăng ký hàng loạt qua local query (triệt tiêu vòng lặp N+1 query)
            var maLhpList = allLhps.Select(l => l.MaLHP).ToList();
            var siSoDict = await _localReadService.DemSiSoDangKyBulkLocalAsync(maLhpList);

            // 4. Build display list
            var displayList = new List<LopHocPhanDisplayDto>(allLhps.Count);
            foreach (var lhp in allLhps)
            {
                int siSo = siSoDict.TryGetValue(lhp.MaLHP, out var count) ? count : 0;
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
    partial void OnTuKhoaLHPChanged(string? value)
    {
        OnPropertyChanged(nameof(HasAnyFilter));
        TriggerDebounce();
    }
    partial void OnSelectedMonFilterChanged(MonHoc? value)
    {
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

            // --- Sắp xếp in-memory ---
            var viComparer = StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), ignoreCase: true);
            query = SelectedSortIndex switch
            {
                1 => query.OrderByDescending(x => x.TenMon ?? string.Empty, viComparer), // Tên môn Z → A
                2 => query.OrderBy(x => x.MaLHP ?? string.Empty, viComparer),            // Mã LHP
                3 => query.OrderByDescending(x => x.SiSoDangKy),                         // Sĩ số ↓
                _ => query.OrderBy(x => x.TenMon ?? string.Empty, viComparer)            // Tên môn A → Z
            };

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
    public Func<string, List<MonHoc>, List<NguoiDung>, LopHocPhan?, bool, Task<LopHocPhan?>>? ShowLhpEditDialogFunc { get; set; }
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
                await _syncService.UpsertLocalEntityAsync(newHk);
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
            await _syncService.SyncDeltaAsync();
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

            var newLhp = await ShowLhpEditDialogFunc(SelectedHocKy.MaHocKy, mons, gvs, null, false);
            if (newLhp != null)
            {
                await _lhpService.ThemAsync(newLhp);
                await _syncService.UpsertLocalEntityAsync(newLhp);
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

            var updatedLhp = await ShowLhpEditDialogFunc(SelectedHocKy.MaHocKy, mons, gvs, dto.Entity, dto.SiSoDangKy > 0);
            if (updatedLhp != null)
            {
                await _lhpService.CapNhatAsync(updatedLhp);
                await _syncService.UpsertLocalEntityAsync(updatedLhp);
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
                await _syncService.RemoveLocalEntityAsync<LopHocPhan>(dto.MaLHP);
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

    [RelayCommand]
    private void DatLaiBoLoc()
    {
        TuKhoaLHP = string.Empty;
        if (DanhSachMonHocFilter.Count > 0)
        {
            SelectedMonFilter = DanhSachMonHocFilter[0];
        }
        SelectedSortIndex = 0;
    }

    [RelayCommand]
    private async Task SaoChepMaLhpAsync(LopHocPhanDisplayDto? lhp)
    {
        if (lhp == null || string.IsNullOrWhiteSpace(lhp.MaLHP)) return;
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(lhp.MaLHP);
            ShowMessage($"Đã sao chép mã LHP: {lhp.MaLHP}", false);
        }
    }

    [RelayCommand]
    private void NapSinhVienSmart(LopHocPhanDisplayDto? lhp)
    {
        if (lhp == null) return;
        WeakReferenceMessenger.Default.Send(new NavigateToImportExcelMessage(SelectedHocKy?.MaHocKy, lhp.MaLHP));
    }

    // ==================== NÂNG CẤP 2: CÁC LỆNH ĐIỀU PHỐI LỚP HỌC PHẦN ====================

    [RelayCommand]
    public async Task MoBangDieuPhoiLopAsync(LopHocPhanDisplayDto? lhp)
    {
        if (lhp == null) return;
        ActiveRosterLhp = lhp;
        IsRosterDrawerOpen = true;
        RosterSearchQuery = string.Empty;
        QuickAddMaSv = string.Empty;
        await ReloadActiveRosterAsync();
    }

    [RelayCommand]
    public void DongBangDieuPhoiLop()
    {
        IsRosterDrawerOpen = false;
    }

    public async Task ReloadActiveRosterAsync()
    {
        if (ActiveRosterLhp == null || App.ServiceProvider == null) return;

        try
        {
            string maLhp = ActiveRosterLhp.MaLHP;
            string maMon = ActiveRosterLhp.MaMon;
            string maHk = ActiveRosterLhp.Entity?.MaHocKy ?? SelectedHocKy?.MaHocKy ?? string.Empty;

            // Cập nhật danh sách các nhóm lớp học phần khác cùng môn học để Giáo viên chuyển lớp 1-chạm
            AlternativeTransferClasses.Clear();
            foreach (var other in _masterLhpList.Where(x => x.Entity?.MaHocKy == maHk && x.MaMon == maMon && x.MaLHP != maLhp))
            {
                AlternativeTransferClasses.Add(other);
            }
            OnPropertyChanged(nameof(HasAlternativeTransferClasses));
            TargetTransferLhp = AlternativeTransferClasses.FirstOrDefault();

            var dbFactory = App.ServiceProvider.GetService<IDbContextFactory<AppDbContext>>();
            if (dbFactory == null) return;

            await using var db = await dbFactory.CreateDbContextAsync();

            var enrollments = await db.DangKyHocPhans.AsNoTracking()
                .Include(d => d.SinhVien)
                .Where(d => d.MaLHP == maLhp && d.TrangThai == "DangHoc" && d.SinhVien != null && !d.SinhVien.IsDeleted)
                .OrderBy(d => d.MaSV)
                .ToListAsync();

            var svIds = enrollments.Select(e => e.MaSV).ToList();
            var debtDict = await db.HocPhiHocKys.AsNoTracking()
                .Where(h => h.MaHocKy == maHk && svIds.Contains(h.MaSV))
                .ToDictionaryAsync(h => h.MaSV, h => h.ConNo);

            var list = new List<ClassRosterStudentDto>(enrollments.Count);
            int stt = 1;
            foreach (var dk in enrollments)
            {
                var sv = dk.SinhVien!;
                decimal conNo = debtDict.TryGetValue(sv.MaSV, out var no) ? no : 0m;
                list.Add(new ClassRosterStudentDto
                {
                    STT = stt++,
                    MaSV = sv.MaSV,
                    HoTen = sv.HoTen,
                    LopSinhHoat = sv.LopSinhHoat ?? "--",
                    KhoaHoc = sv.KhoaHoc ?? "--",
                    NgayDK = dk.NgayDK,
                    ConNoHocPhi = conNo
                });
            }

            _allRosterStudents = list;
            RosterDebtCount = list.Count(x => x.IsConNo);

            if (list.Count == 0)
            {
                RosterDemographicsText = "Chưa có sinh viên nào đăng ký lớp này.";
            }
            else
            {
                var groups = list
                    .GroupBy(x => x.LopSinhHoat)
                    .OrderByDescending(g => g.Count())
                    .Take(4)
                    .Select(g => $"{g.Key}: {g.Count()} SV");
                RosterDemographicsText = "Cơ cấu lớp: " + string.Join(" • ", groups);
            }

            // Đồng bộ lại sĩ số hiển thị trên thẻ ActiveRosterLhp
            ActiveRosterLhp.SiSoDangKy = list.Count;
            ApplyRosterSearchFilter();
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải danh sách lớp: {ex.Message}", true);
        }
    }

    private void ApplyRosterSearchFilter()
    {
        RosterStudents.Clear();
        var query = _allRosterStudents.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(RosterSearchQuery))
        {
            string key = RosterSearchQuery.Trim().ToLowerInvariant();
            query = query.Where(s =>
                s.MaSV.ToLowerInvariant().Contains(key) ||
                s.HoTen.ToLowerInvariant().Contains(key) ||
                s.LopSinhHoat.ToLowerInvariant().Contains(key));
        }

        int stt = 1;
        foreach (var item in query)
        {
            item.STT = stt++;
            RosterStudents.Add(item);
        }
    }

    [RelayCommand]
    public async Task ThemNhanhSvVaoLopAsync()
    {
        if (ActiveRosterLhp == null || string.IsNullOrWhiteSpace(QuickAddMaSv) || App.ServiceProvider == null) return;
        string maSv = QuickAddMaSv.Trim();
        string maLhp = ActiveRosterLhp.MaLHP;

        try
        {
            using var scope = App.ServiceProvider.CreateScope();
            var dkService = scope.ServiceProvider.GetRequiredService<IDangKyHocPhanService>();
            var result = await dkService.DangKyAsync(maSv, maLhp, boQuaCanhBaoTrungMon: false);

            if (!result.Success)
            {
                ShowMessage(result.Message, true);
                return;
            }

            QuickAddMaSv = string.Empty;
            ShowMessage($"✔ Đã thêm nhanh sinh viên {maSv} vào lớp {maLhp} thành công!", false);

            await LoadMasterLhpAsync();
            ApplyLhpFiltersInMemory();
            ActiveRosterLhp = _masterLhpList.FirstOrDefault(x => x.MaLHP == maLhp) ?? ActiveRosterLhp;
            await ReloadActiveRosterAsync();
            WeakReferenceMessenger.Default.Send(new DataImportedNotificationMessage(new QuanLyDKHP.Core.Dtos.KetQuaImportDto()));
        }
        catch (Exception ex)
        {
            ShowMessage($"Không thể thêm sinh viên: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task ChuyenNhomLopSvAsync(ClassRosterStudentDto? sv)
    {
        if (sv == null || ActiveRosterLhp == null || App.ServiceProvider == null) return;
        if (TargetTransferLhp == null)
        {
            ShowMessage("Vui lòng chọn nhóm lớp học phần đích cùng môn để chuyển sinh viên sang.", true);
            return;
        }

        string sourceLhp = ActiveRosterLhp.MaLHP;
        string targetLhp = TargetTransferLhp.MaLHP;

        try
        {
            using var scope = App.ServiceProvider.CreateScope();
            var dkService = scope.ServiceProvider.GetRequiredService<IDangKyHocPhanService>();

            // Bước 1: Hủy đăng ký ở lớp hiện tại
            await dkService.HuyDangKyAsync(sv.MaSV, sourceLhp);

            // Bước 2: Đăng ký vào lớp đích cùng môn
            var enrollResult = await dkService.DangKyAsync(sv.MaSV, targetLhp, boQuaCanhBaoTrungMon: true);
            if (!enrollResult.Success)
            {
                // Khôi phục lại lớp cũ nếu lớp mới bị trùng lịch hoặc đầy sĩ số
                await dkService.DangKyAsync(sv.MaSV, sourceLhp, boQuaCanhBaoTrungMon: true);
                ShowMessage($"⚠ Không thể chuyển sang {targetLhp}: {enrollResult.Message}. Đã giữ nguyên sinh viên ở lớp {sourceLhp}.", true);
                return;
            }

            ShowMessage($"✔ Đã chuyển sinh viên {sv.HoTen} ({sv.MaSV}) từ {sourceLhp} sang {targetLhp} thành công!", false);

            await LoadMasterLhpAsync();
            ApplyLhpFiltersInMemory();
            ActiveRosterLhp = _masterLhpList.FirstOrDefault(x => x.MaLHP == sourceLhp) ?? ActiveRosterLhp;
            await ReloadActiveRosterAsync();
            WeakReferenceMessenger.Default.Send(new DataImportedNotificationMessage(new QuanLyDKHP.Core.Dtos.KetQuaImportDto()));
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi chuyển nhóm lớp: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RutSvKhoiLopAsync(ClassRosterStudentDto? sv)
    {
        if (sv == null || ActiveRosterLhp == null || App.ServiceProvider == null) return;
        string maLhp = ActiveRosterLhp.MaLHP;

        if (ShowConfirmDeleteFunc != null)
        {
            bool ok = await ShowConfirmDeleteFunc($"Bạn có chắc chắn muốn rút sinh viên {sv.HoTen} ({sv.MaSV}) khỏi lớp {maLhp}?");
            if (!ok) return;
        }

        try
        {
            using var scope = App.ServiceProvider.CreateScope();
            var dkService = scope.ServiceProvider.GetRequiredService<IDangKyHocPhanService>();
            await dkService.HuyDangKyAsync(sv.MaSV, maLhp);

            ShowMessage($"✔ Đã rút sinh viên {sv.HoTen} ({sv.MaSV}) khỏi lớp {maLhp}.", false);

            await LoadMasterLhpAsync();
            ApplyLhpFiltersInMemory();
            ActiveRosterLhp = _masterLhpList.FirstOrDefault(x => x.MaLHP == maLhp) ?? ActiveRosterLhp;
            await ReloadActiveRosterAsync();
            WeakReferenceMessenger.Default.Send(new DataImportedNotificationMessage(new QuanLyDKHP.Core.Dtos.KetQuaImportDto()));
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi rút học phần: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void MoHoSoSvTrongDkhp(ClassRosterStudentDto? sv)
    {
        if (sv == null) return;
        WeakReferenceMessenger.Default.Send(new NavigateToRegistrationMessage(sv.MaSV));
    }

    [RelayCommand]
    public async Task SaoChepPhieuDiemDanhAsync()
    {
        if (ActiveRosterLhp == null || _allRosterStudents.Count == 0)
        {
            ShowMessage("Lớp học phần chưa có sinh viên để xuất danh sách điểm danh.", true);
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"PHIẾU ĐIỂM DANH LỚP HỌC PHẦN: {ActiveRosterLhp.MaLHP} - {ActiveRosterLhp.TenMon}");
        sb.AppendLine($"Giảng viên: {ActiveRosterLhp.TenGiangVien}\tLịch học: {ActiveRosterLhp.LichHocHienThi}\tSĩ số: {_allRosterStudents.Count} SV");
        sb.AppendLine("STT\tMã SV\tHọ và Tên\tLớp Sinh Hoạt\tKhóa\tBuổi 1\tBuổi 2\tBuổi 3\tBuổi 4\tBuổi 5\tGhi chú");

        int stt = 1;
        foreach (var sv in _allRosterStudents)
        {
            sb.AppendLine($"{stt++}\t{sv.MaSV}\t{sv.HoTen}\t{sv.LopSinhHoat}\t{sv.KhoaHoc}\t\t\t\t\t\t{(sv.IsConNo ? sv.TrangThaiHocPhiText : "")}");
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(sb.ToString());
            ShowMessage($"✔ Đã sao chép Phiếu điểm danh ({_allRosterStudents.Count} SV) vào Clipboard! Bạn có thể dán (Ctrl+V) ngay vào Excel/Word.", false);
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}