// src/QuanLyDKHP.App/ViewModels/DangKyHocPhanViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using QuanLyDKHP.App.Dtos;
using QuanLyDKHP.App.Helpers;
using QuanLyDKHP.App.Messages;
using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

/// <summary>
/// Data Model cho từng sinh viên trong chế độ So Sánh (State 1)
/// </summary>
public class StudentComparisonItem
{
    public SinhVienDto StudentInfo { get; set; } = null!;
    public ObservableCollection<DangKyHocPhanDisplayDto> RegisteredCourses { get; set; } = [];
    public int TongTinChi => RegisteredCourses.Where(c => c.IsDangHoc).Sum(c => c.TongTinChi);
    public bool IsDuTinChi => TongTinChi >= 15;
}

/// <summary>
/// Các trạng thái giao diện chi tiết (Detail Pane) theo mô hình Advanced Master-Detail.
/// </summary>
public enum DetailState
{
    None,      // State 0: Chưa chọn ai
    Compare,   // State 1: Chọn nhiều SV (So sánh)
    View,      // State 2: Chọn 1 SV (Xem tổng quan & chi tiết)
    Edit       // State 3: Chỉnh sửa đăng ký học phần
}

/// <summary>
/// Tùy chọn lọc trạng thái đăng ký của môn học tại cột Master.
/// </summary>
public enum RegistrationStatusFilter
{
    All,
    Registered,
    Unregistered
}

/// <summary>
/// ViewModel quản lý đăng ký và điều chỉnh học phần theo Advanced Master-Detail Pattern.
/// - Cột trái (Master): Bộ lọc xếp tầng (Khóa học -> Lớp -> Môn kèm trạng thái ĐK), tìm kiếm in-memory debounce 300ms, danh sách sinh viên multi-select.
/// - Cột phải (Detail): 4 trạng thái (None, Compare, View, Edit).
/// - Giao tiếp liên module: Nhận NavigateToRegistrationMessage để tự động chuyển trạng thái và load sinh viên.
/// </summary>
public partial class DangKyHocPhanViewModel : ObservableRecipient, IRecipient<NavigateToRegistrationMessage>
{
    private readonly IDangKyHocPhanService _dangKyService;
    private readonly IHocKyService _hocKyService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IMonHocService _monHocService;
    private readonly ILopHocPhanService _lopHocPhanService;
    private readonly ICauHinhService _cauHinhService;
    private readonly INguoiDungRepository _nguoiDungRepository;
    private readonly IMemoryCacheStore _cacheStore;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBaoCaoService? _baoCaoService;
    private readonly ILocalReadService _localReadService;
    private readonly ISyncService _syncService;

    // Timer debounce 300ms và CancellationTokenSource cho tìm kiếm in-memory
    private readonly System.Timers.Timer _debounceTimer;
    private CancellationTokenSource? _filterCts;
    private bool _dangKhoiTao = true;

    // Cache toàn bộ sinh viên trên RAM (Master list)
    private List<SinhVienDto> _masterList = [];

    // Tập hợp MaSV đã đăng ký môn học đang chọn lọc (nếu có)
    private HashSet<string> _registeredStudentIdsForFilter = [];

    // Trạng thái hiển thị Detail Pane
    [ObservableProperty]
    private DetailState _currentState = DetailState.None;

    [ObservableProperty]
    private HocKy? _hocKyHienHanh;

    [ObservableProperty]
    private bool _canEdit;

    [ObservableProperty]
    private bool _isLoading;

    // Master Filters
    [ObservableProperty]
    private string? _tuKhoa;

    [ObservableProperty]
    private string? _khoaHocFilter = "Tất cả";

    [ObservableProperty]
    private string? _lopFilter = "Tất cả";

    [ObservableProperty]
    private MonHoc? _monHocFilter;

    [ObservableProperty]
    private bool _isTrangThaiDangKyFilterEnabled;

    [ObservableProperty]
    private bool _isFilterDaDangKy;

    [ObservableProperty]
    private bool _isFilterChuaDangKy;

    [ObservableProperty]
    private bool _isFilterTatCaDangKy = true;

    [ObservableProperty]
    private int _totalStudentsCount;

    // Detail Pane - View/Edit Mode properties cho 1 sinh viên
    [ObservableProperty]
    private SinhVienDto? _sinhVienDangChon;

    [ObservableProperty]
    private int _tongTinChiHienTai;

    [ObservableProperty]
    private int _tinChiToiDa = 25;

    [ObservableProperty]
    private int _tinChiToiThieu = 10;

    [ObservableProperty]
    private double _progressBarValue;

    [ObservableProperty]
    private string _progressBarColor = "#ACD26B"; // BrushPrimary

    [ObservableProperty]
    private bool _isChuaDatToiThieu;

    [ObservableProperty]
    private string _canhBaoToiThieuText = string.Empty;

    // Edit Form properties
    [ObservableProperty]
    private MonHoc? _monDangChon;

    [ObservableProperty]
    private LopHocPhanDisplayDto? _lHPDangChon;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    // Master Collections
    [ObservableProperty]
    private ObservableCollection<SinhVienDto> _danhSachSinhVien = [];

    [ObservableProperty]
    private ObservableCollection<SinhVienDto> _selectedStudents = [];

    public ObservableCollection<string> DsKhoaHocFilter { get; } = [];
    public ObservableCollection<string> DsLopSinhHoatFilter { get; } = [];
    public ObservableCollection<MonHoc> DsMonHocFilter { get; } = [];

    // Detail View/Edit Collections
    [ObservableProperty]
    private ObservableCollection<DangKyHocPhanDisplayDto> _dsDaDangKy = [];

    [ObservableProperty]
    private ObservableCollection<MonHoc> _dsMonHoc = [];

    [ObservableProperty]
    private ObservableCollection<LopHocPhanDisplayDto> _dsLHPTheoMon = [];

    // Detail Compare Collections & Properties
    [ObservableProperty]
    private bool _chiHienThiMonChung;

    [ObservableProperty]
    private string _kieuSapXep = "Tên (A-Z)";
    [ObservableProperty]
    private bool _isDataReady;

    [ObservableProperty]
    private double _loadingPercent;

    [ObservableProperty]
    private string _loadingMessage = "Đang khởi tạo...";

    private string? _pendingMaSvToSelect;

    public ObservableCollection<string> DsKieuSapXep { get; } =
    [
        "Tên (A-Z)",
        "Tên (Z-A)",
        "Số tín chỉ (Thấp -> Cao)",
        "Số tín chỉ (Cao -> Thấp)"
    ];

    [ObservableProperty]
    private ObservableCollection<StudentComparisonItem> _studentComparisonList = [];

    private List<StudentComparisonItem> _rawComparisonItems = [];
    private readonly VietnameseNameComparer _nameComparer = new();

    public ObservableCollection<SinhVienDto> CompareStudentsSummary { get; } = [];
    public ObservableCollection<DangKyHocPhanDisplayDto> CompareCoursesSummary { get; } = [];

    // UI Delegate Actions
    public Func<string, Task<bool>>? ShowConfirmFunc { get; set; }
    public Func<string, Task<bool>>? ShowWarningConfirmFunc { get; set; }
    public Action<List<SinhVienDto>>? RequestSelectStudents { get; set; }

public DangKyHocPhanViewModel(
        IDangKyHocPhanService dangKyService,
        IHocKyService hocKyService,
        ISinhVienService sinhVienService,
        IMonHocService monHocService,
        ILopHocPhanService lopHocPhanService,
        ICauHinhService cauHinhService,
        INguoiDungRepository nguoiDungRepository,
        IMemoryCacheStore cacheStore,
        ICurrentUserService currentUserService,
        ILocalReadService localReadService,
        ISyncService syncService,
        IBaoCaoService? baoCaoService = null)
    {
        _dangKyService = dangKyService;
        _hocKyService = hocKyService;
        _sinhVienService = sinhVienService;
        _monHocService = monHocService;
        _lopHocPhanService = lopHocPhanService;
        _cauHinhService = cauHinhService;
        _nguoiDungRepository = nguoiDungRepository;
        _localReadService = localReadService;
        _syncService = syncService;
        
        // GÁN THAM SỐ VÀO BIẾN TRƯỜNG TRƯỚC TIÊN
        _cacheStore = cacheStore;

        _cacheStore.ProgressChanged += (percent, msg) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                LoadingPercent = percent;
                LoadingMessage = msg;
                if (percent >= 100)
                {
                    IsDataReady = true;
                    // Nếu có sinh viên đang đợi load từ double-click lúc chưa xong cache
                    if (!string.IsNullOrEmpty(_pendingMaSvToSelect))
                    {
                        _ = ChonSinhVienTheoMaAsync(_pendingMaSvToSelect);
                        _pendingMaSvToSelect = null;
                    }
                }
            });
        };
        IsDataReady = _cacheStore.IsInitialized;

        _currentUserService = currentUserService;
        _baoCaoService = baoCaoService;

        // Kiểm tra quyền chỉnh sửa
        CanEdit = _currentUserService.CurrentUser != null &&
                  _currentUserService.HasPermission(ChucNang.DangKyHocPhan);

        // Đăng ký nhận message và kích hoạt
        IsActive = true;

        // Timer debounce 300ms
        _debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) => ApplyFiltersInMemory();

        _ = InitDataAsync();
    }

    public DangKyHocPhanViewModel()
    {
        _dangKyService = null!;
        _hocKyService = null!;
        _sinhVienService = null!;
        _monHocService = null!;
        _lopHocPhanService = null!;
        _cauHinhService = null!;
        _nguoiDungRepository = null!;
        _cacheStore = null!;
        _currentUserService = null!;
        _localReadService = null!;
        _syncService = null!;
        _debounceTimer = new System.Timers.Timer(300);
    }

    /// <summary>
    /// Nhận message điều hướng từ tab Sinh Viên
    /// </summary>
    public void Receive(NavigateToRegistrationMessage message)
    {
        if (!_cacheStore.IsInitialized)
        {
            // Cache chưa xong -> lưu lại, chờ ProgressChanged 100% sẽ tự chọn
            _pendingMaSvToSelect = message.MaSV;
            IsDataReady = false;
            _ = _cacheStore.InitializeAsync();   // đảm bảo cache đang chạy
            return;
        }

        _ = ChonSinhVienTheoMaAsync(message.MaSV);
    }

    /// <summary>
    /// Khởi tạo dữ liệu Master Data, cấu hình tín chỉ và cache
    /// </summary>
    private async Task InitDataAsync()
    {
        try
        {
            IsLoading = true;
            if (!_cacheStore.IsInitialized)
            {
                await _cacheStore.InitializeAsync();
            }

            LoadHocKyHienHanh();
            await LoadCauHinhTinChiAsync();
            LoadDanhSachMonHoc();

            // Nạp bộ lọc Master
            DsKhoaHocFilter.Clear();
            DsKhoaHocFilter.Add("Tất cả");
            foreach (var kh in _cacheStore.DanhSachKhoaHoc) DsKhoaHocFilter.Add(kh);

            DsLopSinhHoatFilter.Clear();
            DsLopSinhHoatFilter.Add("Tất cả");
            foreach (var lop in _cacheStore.DanhSachLopSinhHoat) DsLopSinhHoatFilter.Add(lop);

            DsMonHocFilter.Clear();
            foreach (var mh in _cacheStore.DanhSachMonHoc) DsMonHocFilter.Add(mh);

            // Nạp toàn bộ danh sách sinh viên vào RAM từ SQLite Local DB (0ms)
            var items = await _localReadService.GetSinhViensLocalAsync();
            _masterList = items;

            _dangKhoiTao = false;

            // Thực hiện lọc lần đầu
            ApplyFiltersInMemory();
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khởi tạo dữ liệu: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadHocKyHienHanh()
    {
        var hks = _cacheStore.DanhSachHocKy;
        HocKyHienHanh = hks.FirstOrDefault(hk => hk.DangMo) ?? hks.FirstOrDefault();
    }

    private async Task LoadCauHinhTinChiAsync()
    {
        TinChiToiDa = await _cauHinhService.GetInt("SoTinChiToiDa");
        if (TinChiToiDa <= 0) TinChiToiDa = 25;

        TinChiToiThieu = await _cauHinhService.GetInt("SoTinChiToiThieu");
        if (TinChiToiThieu <= 0) TinChiToiThieu = 10;
    }

    private void LoadDanhSachMonHoc()
    {
        DsMonHoc = new ObservableCollection<MonHoc>(_cacheStore.DanhSachMonHoc);
    }

    // ================== CASCADING FILTER TRIGGERS ==================

    partial void OnTuKhoaChanged(string? value) => TriggerDebounce();

    partial void OnKhoaHocFilterChanged(string? value)
    {
        if (_dangKhoiTao) return;

        // Cập nhật lại danh sách Lớp sinh hoạt tương ứng theo Khóa học
        DsLopSinhHoatFilter.Clear();
        DsLopSinhHoatFilter.Add("Tất cả");

        if (string.IsNullOrWhiteSpace(value) || value == "Tất cả")
        {
            foreach (var lop in _cacheStore.DanhSachLopSinhHoat) DsLopSinhHoatFilter.Add(lop);
        }
        else
        {
            var lopsTheoKhoa = _masterList
                .Where(s => s.KhoaHoc == value && !string.IsNullOrEmpty(s.LopSinhHoat))
                .Select(s => s.LopSinhHoat!)
                .Distinct()
                .OrderBy(l => l);
            foreach (var lop in lopsTheoKhoa) DsLopSinhHoatFilter.Add(lop);
        }

        LopFilter = "Tất cả";
        TriggerDebounce();
    }

    partial void OnLopFilterChanged(string? value) => TriggerDebounce();

    partial void OnMonHocFilterChanged(MonHoc? value)
    {
        if (_dangKhoiTao) return;

        if (value == null)
        {
            IsTrangThaiDangKyFilterEnabled = false;
            IsFilterTatCaDangKy = true;
            IsFilterDaDangKy = false;
            IsFilterChuaDangKy = false;
            _registeredStudentIdsForFilter.Clear();
            TriggerDebounce();
        }
        else
        {
            IsTrangThaiDangKyFilterEnabled = true;
            _ = CapNhatSinhVienTheoMonFilterAsync(value);
        }
    }

    partial void OnIsFilterDaDangKyChanged(bool value)
    {
        if (value)
        {
            IsFilterTatCaDangKy = false;
            IsFilterChuaDangKy = false;
            TriggerDebounce();
        }
    }

    partial void OnIsFilterChuaDangKyChanged(bool value)
    {
        if (value)
        {
            IsFilterTatCaDangKy = false;
            IsFilterDaDangKy = false;
            TriggerDebounce();
        }
    }

    partial void OnIsFilterTatCaDangKyChanged(bool value)
    {
        if (value)
        {
            IsFilterDaDangKy = false;
            IsFilterChuaDangKy = false;
            TriggerDebounce();
        }
    }

    [RelayCommand]
    private void ClearMonHocFilter()
    {
        MonHocFilter = null;
    }

    private async Task CapNhatSinhVienTheoMonFilterAsync(MonHoc mon)
    {
        try
        {
            if (HocKyHienHanh != null)
            {
                Dispatcher.UIThread.Invoke(() => IsLoading = true);
                var ds = await _localReadService.GetDsSinhVienTheoMonLocalAsync(mon.MaMon, HocKyHienHanh.MaHocKy);
                _registeredStudentIdsForFilter = ds
                    .Where(x => x.TrangThai == "DangHoc")
                    .Select(x => x.MaSV)
                    .ToHashSet();
            }
            else
            {
                _registeredStudentIdsForFilter.Clear();
            }
        }
        catch
        {
            _registeredStudentIdsForFilter.Clear();
        }
        finally
        {
            Dispatcher.UIThread.Invoke(() => IsLoading = false);
            TriggerDebounce();
        }
    }

    partial void OnKieuSapXepChanged(string value) => TriggerDebounce();

    partial void OnChiHienThiMonChungChanged(bool value) => ApDungLocMonChung();

    private void TriggerDebounce()
    {
        if (_dangKhoiTao) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    /// <summary>
    /// Áp dụng bộ lọc in-memory trên background thread bằng CancellationTokenSource, không block UI thread.
    /// </summary>
    private void ApplyFiltersInMemory()
    {
        _filterCts?.Cancel();
        _filterCts?.Dispose();
        _filterCts = new CancellationTokenSource();
        var ct = _filterCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => IsLoading = true);
                if (ct.IsCancellationRequested) return;

                var query = _masterList.AsEnumerable();

                // Lọc theo từ khóa
                if (!string.IsNullOrWhiteSpace(TuKhoa))
                {
                    var key = TuKhoa.Trim().ToLower();
                    query = query.Where(s => s.MaSV.ToLower().Contains(key) || s.HoTen.ToLower().Contains(key));
                }

                // Lọc theo khóa học
                if (!string.IsNullOrWhiteSpace(KhoaHocFilter) && KhoaHocFilter != "Tất cả")
                {
                    query = query.Where(s => s.KhoaHoc == KhoaHocFilter);
                }

                // Lọc theo lớp sinh hoạt
                if (!string.IsNullOrWhiteSpace(LopFilter) && LopFilter != "Tất cả")
                {
                    query = query.Where(s => s.LopSinhHoat == LopFilter);
                }

                // Lọc theo môn học & trạng thái ĐK
                if (MonHocFilter != null && IsTrangThaiDangKyFilterEnabled)
                {
                    if (IsFilterDaDangKy)
                    {
                        query = query.Where(s => _registeredStudentIdsForFilter.Contains(s.MaSV));
                    }
                    else if (IsFilterChuaDangKy)
                    {
                        query = query.Where(s => !_registeredStudentIdsForFilter.Contains(s.MaSV));
                    }
                }

                // Sắp xếp danh sách sinh viên In-Memory
                var vietnameseComparer = Comparer<SinhVienDto>.Create((a, b) => _nameComparer.Compare(a, b));
                query = KieuSapXep switch
                {
                    "Tên (Z-A)" => query.OrderByDescending(s => s, vietnameseComparer),
                    "Số tín chỉ (Thấp -> Cao)" => query.OrderBy(s => s.SoTinChiDangKy).ThenBy(s => s, vietnameseComparer),
                    "Số tín chỉ (Cao -> Thấp)" => query.OrderByDescending(s => s.SoTinChiDangKy).ThenBy(s => s, vietnameseComparer),
                    _ => query.OrderBy(s => s, vietnameseComparer)
                };

                var resultList = query.ToList();
                if (ct.IsCancellationRequested) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!ct.IsCancellationRequested)
                    {
                        DanhSachSinhVien = new ObservableCollection<SinhVienDto>(resultList);
                        TotalStudentsCount = resultList.Count;
                        IsLoading = false;
                    }
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowMessage($"Lỗi lọc dữ liệu: {ex.Message}", true);
                    IsLoading = false;
                });
            }
        }, ct);
    }

    // ================== MASTER-DETAIL SELECTION & STATE MACHINE ==================

    /// <summary>
    /// Cập nhật danh sách sinh viên được chọn từ ListBox (SelectionMode="Multiple")
    /// Điều phối chuyển đổi trạng thái (DetailState machine):
    /// - 0 SV -> State 0: None
    /// - 1 SV -> State 2: View (xem chi tiết)
    /// - Nhiều SV -> State 1: Compare (so sánh nhóm)
    /// </summary>
    /// <summary>
    /// Xử lý thay đổi Selection từ ListBox bên trái qua Code-behind (Fix 1)
    /// </summary>
    public async Task HandleSelectionChanged(IReadOnlyList<SinhVienDto> selectedList)
    {
        SelectedStudents = new ObservableCollection<SinhVienDto>(selectedList);

        // State 0: Bảng rỗng
        if (SelectedStudents.Count == 0)
        {
            SinhVienDangChon = null;
            CurrentState = DetailState.None;
            DsDaDangKy = [];
            StudentComparisonList = [];
            _rawComparisonItems.Clear();
            return;
        }

        // State 2: Xem chi tiết 1 SV
        if (SelectedStudents.Count == 1)
        {
            SinhVienDangChon = SelectedStudents[0];
            CurrentState = DetailState.View;
            StudentComparisonList = [];
            _rawComparisonItems.Clear();
            await LoadDangKyCuaSinhVienAsync(SinhVienDangChon.MaSV);
            return;
        }

        // State 1: So sánh nhiều SV
        SinhVienDangChon = null;
        CurrentState = DetailState.Compare;
        DsDaDangKy = [];
        await LoadDuLieuSoSanhAsync(SelectedStudents.ToList());
    }

    /// <summary>
    /// Alias tương thích ngược cho CapNhatDanhSachChonAsync
    /// </summary>
    public Task CapNhatDanhSachChonAsync(IReadOnlyList<SinhVienDto> selectedList) => HandleSelectionChanged(selectedList);

    /// <summary>
    /// Chọn đích danh 1 sinh viên qua mã SV (phục vụ điều hướng từ Tab Sinh Viên)
    /// </summary>
    public async Task ChonSinhVienTheoMaAsync(string maSV)
    {
        if (string.IsNullOrWhiteSpace(maSV)) return;

        // Tránh load lặp nếu đang xem chính sinh viên đó
        if (SinhVienDangChon?.MaSV == maSV && CurrentState == DetailState.View) return;

        var sv = _masterList.FirstOrDefault(s => s.MaSV == maSV);
        if (sv == null)
        {
            var paged = await _sinhVienService.TimKiemAsync(maSV, null, null, 1, 1);
            sv = paged.Items.FirstOrDefault(s => s.MaSV == maSV);
        }

        if (sv != null)
        {
            SelectedStudents = [sv];
            SinhVienDangChon = sv;
            CurrentState = DetailState.View;
            StudentComparisonList = [];
            _rawComparisonItems.Clear();

            await LoadDangKyCuaSinhVienAsync(sv.MaSV);
            RequestSelectStudents?.Invoke([sv]);
        }
    }

    /// <summary>
    /// Tải danh sách môn học đã đăng ký của 1 sinh viên (State View/Edit)
    /// Sử dụng List tạm gán 1 lần duy nhất vào ObservableCollection để triệt tiêu lỗi layout loop (Fix 2).
    /// </summary>
    public async Task LoadDangKyCuaSinhVienAsync(string maSV)
    {
        if (HocKyHienHanh == null)
        {
            DsDaDangKy = [];
            TongTinChiHienTai = 0;
            CapNhatThanhTinChi();
            return;
        }

        try
        {
            var list = await _localReadService.GetDangKyLocalAsync(maSV, HocKyHienHanh.MaHocKy);
            var giangViens = await _nguoiDungRepository.LayGiangVienAsync();
            var gvDict = giangViens.ToDictionary(g => g.TenDangNhap, g => g.HoTen);

            var tempList = new List<DangKyHocPhanDisplayDto>();

            foreach (var dk in list)
            {
                string gvName = "Chưa phân công";
                if (dk.LopHocPhan != null && !string.IsNullOrEmpty(dk.LopHocPhan.MaGV) && gvDict.TryGetValue(dk.LopHocPhan.MaGV, out var name))
                {
                    gvName = $"{name} ({dk.LopHocPhan.MaGV})";
                }

                int tcLT = dk.LopHocPhan?.MonHoc?.SoTinChiLT ?? 0;
                int tcTH = dk.LopHocPhan?.MonHoc?.SoTinChiTH ?? 0;

                tempList.Add(new DangKyHocPhanDisplayDto
                {
                    Id = dk.Id,
                    MaSV = dk.MaSV,
                    MaLHP = dk.MaLHP,
                    MaMon = dk.LopHocPhan?.MaMon ?? string.Empty,
                    TenMon = dk.LopHocPhan?.MonHoc?.TenMon ?? dk.MaLHP,
                    SoTinChiLT = tcLT,
                    SoTinChiTH = tcTH,
                    TenGiangVien = gvName,
                    HinhThucDK = dk.HinhThucDK,
                    NgayDK = dk.NgayDK,
                    TrangThai = dk.TrangThai,
                    SoTienPhaiDong = dk.SoTienPhaiDong,
                    SoTienDaDong = dk.SoTienDaDong,
                    Entity = dk
                });
            }

            // Gán 1 lần duy nhất thay vì Add() từng phần tử trong vòng lặp
            DsDaDangKy = new ObservableCollection<DangKyHocPhanDisplayDto>(tempList);

            // Tính tổng tín chỉ client-side bằng Sum(SoTinChi) trên collection đã load (không query lại DB)
            TongTinChiHienTai = DsDaDangKy.Where(d => d.IsDangHoc).Sum(d => d.TongTinChi);
            CapNhatThanhTinChi();
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải học phần đã đăng ký: {ex.Message}", true);
        }
    }

    /// <summary>
    /// Tải dữ liệu chi tiết cho từng sinh viên phục vụ hiển thị dạng Card riêng biệt (State 1 Compare)
    /// </summary>
    private async Task LoadDuLieuSoSanhAsync(List<SinhVienDto> students)
    {
        if (HocKyHienHanh == null || students.Count == 0)
        {
            _rawComparisonItems.Clear();
            StudentComparisonList = [];
            return;
        }

        try
        {
            Dispatcher.UIThread.Invoke(() => IsLoading = true);

            var maSvList = students.Select(s => s.MaSV).ToList();
            var allRegistrations = await _localReadService.GetDangKyNhieuSvLocalAsync(maSvList, HocKyHienHanh.MaHocKy);
            var regBySv = allRegistrations
                .Where(d => d.TrangThai == "DangHoc")
                .GroupBy(d => d.MaSV)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = new List<StudentComparisonItem>();
            foreach (var sv in students)
            {
                var list = regBySv.TryGetValue(sv.MaSV, out var svRegs) ? svRegs : new List<DangKyHocPhan>();
                var rawCourses = new List<DangKyHocPhanDisplayDto>();

                foreach (var dk in list)
                {
                    int tcLT = dk.LopHocPhan?.MonHoc?.SoTinChiLT ?? 0;
                    int tcTH = dk.LopHocPhan?.MonHoc?.SoTinChiTH ?? 0;

                    rawCourses.Add(new DangKyHocPhanDisplayDto
                    {
                        Id = dk.Id,
                        MaSV = dk.MaSV,
                        MaLHP = dk.MaLHP,
                        MaMon = dk.LopHocPhan?.MaMon ?? string.Empty,
                        TenMon = dk.LopHocPhan?.MonHoc?.TenMon ?? dk.MaLHP,
                        SoTinChiLT = tcLT,
                        SoTinChiTH = tcTH,
                        HinhThucDK = dk.HinhThucDK,
                        NgayDK = dk.NgayDK,
                        TrangThai = dk.TrangThai,
                        Entity = dk
                    });
                }

                items.Add(new StudentComparisonItem
                {
                    StudentInfo = sv,
                    RegisteredCourses = new ObservableCollection<DangKyHocPhanDisplayDto>(rawCourses)
                });
            }

            _rawComparisonItems = items;

            Dispatcher.UIThread.Invoke(() =>
            {
                ApDungLocMonChung();
                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                ShowMessage($"Lỗi tải dữ liệu so sánh: {ex.Message}", true);
                IsLoading = false;
            });
        }
    }

    /// <summary>
    /// Lọc các môn học chung giữa tất cả các sinh viên đang chọn (nếu bật CheckBox)
    /// </summary>
    private void ApDungLocMonChung()
    {
        if (_rawComparisonItems.Count == 0)
        {
            StudentComparisonList = [];
            return;
        }

        if (!ChiHienThiMonChung)
        {
            StudentComparisonList = new ObservableCollection<StudentComparisonItem>(
                _rawComparisonItems.Select(item => new StudentComparisonItem
                {
                    StudentInfo = item.StudentInfo,
                    RegisteredCourses = new ObservableCollection<DangKyHocPhanDisplayDto>(item.RegisteredCourses)
                }));
            return;
        }

        // Lấy danh sách Mã môn xuất hiện ở TẤT CẢ các sinh viên đang chọn
        var commonMonSet = _rawComparisonItems
            .Select(i => i.RegisteredCourses.Select(c => c.MaMon).Distinct())
            .Aggregate((prev, next) => prev.Intersect(next))
            .ToHashSet();

        var filtered = _rawComparisonItems.Select(item => new StudentComparisonItem
        {
            StudentInfo = item.StudentInfo,
            RegisteredCourses = new ObservableCollection<DangKyHocPhanDisplayDto>(
                item.RegisteredCourses.Where(c => commonMonSet.Contains(c.MaMon)))
        }).ToList();

        StudentComparisonList = new ObservableCollection<StudentComparisonItem>(filtered);
    }

    private void CapNhatThanhTinChi()
    {
        ProgressBarValue = TinChiToiDa > 0 ? Math.Min(100.0, (double)TongTinChiHienTai / TinChiToiDa * 100.0) : 0;

        if (TinChiToiDa > 0 && TongTinChiHienTai >= TinChiToiDa)
        {
            ProgressBarColor = "#D64541"; // BrushDanger
        }
        else if (TinChiToiDa > 0 && (double)TongTinChiHienTai / TinChiToiDa >= 0.9)
        {
            ProgressBarColor = "#E0A83C"; // BrushWarning
        }
        else
        {
            ProgressBarColor = "#ACD26B"; // BrushPrimary
        }

        if (SinhVienDangChon != null && TongTinChiHienTai < TinChiToiThieu)
        {
            IsChuaDatToiThieu = true;
            CanhBaoToiThieuText = $"Chưa đạt số tín chỉ tối thiểu ({TongTinChiHienTai}/{TinChiToiThieu})";
        }
        else
        {
            IsChuaDatToiThieu = false;
            CanhBaoToiThieuText = string.Empty;
        }
    }

    // ================== STATE TRANSITIONS ==================

    [RelayCommand]
    private void EnterEditMode()
    {
        if (!CanEdit)
        {
            ShowMessage("Bạn không có quyền chỉnh sửa đăng ký học phần.", true);
            return;
        }

        CurrentState = DetailState.Edit;
    }

    [RelayCommand]
    private async Task ExitEditModeAsync()
    {
        CurrentState = DetailState.View;
        if (SinhVienDangChon != null)
        {
            await LoadDangKyCuaSinhVienAsync(SinhVienDangChon.MaSV);
        }
    }

    // ================== EDIT MODE: FORM ĐĂNG KÝ VÀ HỦY ĐK ==================

    partial void OnMonDangChonChanged(MonHoc? value)
    {
        _ = LoadLHPTheoMonAsync(value);
    }

    private async Task LoadLHPTheoMonAsync(MonHoc? mon)
    {
        if (mon == null || HocKyHienHanh == null)
        {
            DsLHPTheoMon = [];
            LHPDangChon = null;
            return;
        }

        try
        {
            var lhps = await _localReadService.GetLopHocPhansLocalAsync(HocKyHienHanh.MaHocKy, null, mon.MaMon);
            var giangViens = await _nguoiDungRepository.LayGiangVienAsync();
            var gvDict = giangViens.ToDictionary(g => g.TenDangNhap, g => g.HoTen);
            var maLhpList = lhps.Select(l => l.MaLHP).ToList();
            var siSoDict = await _localReadService.DemSiSoDangKyBulkLocalAsync(maLhpList);

            var tempList = new List<LopHocPhanDisplayDto>();

            foreach (var lhp in lhps)
            {
                int siSoRealtime = siSoDict.TryGetValue(lhp.MaLHP, out var count) ? count : 0;
                if (lhp.SiSoToiDa.HasValue && lhp.SiSoToiDa.Value > 0 && siSoRealtime >= lhp.SiSoToiDa.Value)
                {
                    continue; // Ẩn các LHP đã đầy sĩ số
                }

                string gvName = !string.IsNullOrEmpty(lhp.MaGV) && gvDict.TryGetValue(lhp.MaGV, out var name)
                    ? $"{name} ({lhp.MaGV})"
                    : "Chưa phân công";

                tempList.Add(new LopHocPhanDisplayDto
                {
                    MaLHP = lhp.MaLHP,
                    MaMon = lhp.MaMon,
                    TenMon = lhp.MonHoc?.TenMon ?? mon.TenMon,
                    MaGV = lhp.MaGV,
                    TenGiangVien = gvName,
                    LoaiHinhDT = lhp.LoaiHinhDT,
                    SiSoToiDa = lhp.SiSoToiDa,
                    SiSoDangKy = siSoRealtime,
                    GiangDayOnline = lhp.GiangDayOnline,
                    Entity = lhp
                });
            }

            DsLHPTheoMon = new ObservableCollection<LopHocPhanDisplayDto>(tempList);
            LHPDangChon = DsLHPTheoMon.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task DangKyAsync()
    {
        if (SinhVienDangChon == null)
        {
            ShowMessage("Vui lòng chọn sinh viên cần đăng ký học phần.", true);
            return;
        }

        if (LHPDangChon == null)
        {
            ShowMessage("Vui lòng chọn lớp học phần cần đăng ký.", true);
            return;
        }

        try
        {
            var result = await _dangKyService.DangKyAsync(SinhVienDangChon.MaSV, LHPDangChon.MaLHP, boQuaCanhBaoTrungMon: false);

            if (result.IsCanhBaoTrungMon)
            {
                if (ShowWarningConfirmFunc != null)
                {
                    bool confirm = await ShowWarningConfirmFunc(result.Message);
                    if (confirm)
                    {
                        var confirmResult = await _dangKyService.DangKyAsync(SinhVienDangChon.MaSV, LHPDangChon.MaLHP, boQuaCanhBaoTrungMon: true);
                        if (!confirmResult.Success)
                        {
                            ShowMessage(confirmResult.Message, true);
                            return;
                        }
                    }
                    else
                    {
                        return; // Người dùng hủy đăng ký
                    }
                }
                else
                {
                    ShowMessage(result.Message, true);
                    return;
                }
            }
            else if (!result.Success)
            {
                ShowMessage(result.Message, true);
                return;
            }

            ShowMessage("Đăng ký thành công.", false);
            await _syncService.SyncDeltaAsync();
            await LoadDangKyCuaSinhVienAsync(SinhVienDangChon.MaSV);
            await LoadLHPTheoMonAsync(MonDangChon);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task HuyDangKyAsync(DangKyHocPhanDisplayDto? dto)
    {
        if (dto == null || SinhVienDangChon == null) return;

        if (ShowConfirmFunc != null)
        {
            bool confirm = await ShowConfirmFunc($"Bạn có chắc chắn muốn hủy đăng ký lớp học phần {dto.MaLHP} - môn {dto.TenMon} của sinh viên {SinhVienDangChon.HoTen}?");
            if (!confirm) return;
        }

        try
        {
            await _dangKyService.HuyDangKyAsync(SinhVienDangChon.MaSV, dto.MaLHP);
            ShowMessage($"Đã hủy đăng ký lớp học phần {dto.MaLHP} thành công.", false);
            await _syncService.SyncDeltaAsync();
            await LoadDangKyCuaSinhVienAsync(SinhVienDangChon.MaSV);
            await LoadLHPTheoMonAsync(MonDangChon);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }
    [RelayCommand]
    public async Task RefreshDataAsync()
    {
        try
        {
            IsDataReady = false;
            await _cacheStore.InitializeAsync(forceReload: true);
            await InitDataAsync();                 // hoặc InitFiltersAndLoadMasterDataAsync nếu bạn đổi tên
            ShowMessage("Đã làm mới dữ liệu thành công.", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi làm mới dữ liệu: {ex.Message}", true);
        }
        finally
        {
            IsDataReady = true;
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}
