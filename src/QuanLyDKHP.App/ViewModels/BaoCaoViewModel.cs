// src/QuanLyDKHP.App/ViewModels/BaoCaoViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDKHP.App.Messages;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;
using QuanLyDKHP.Infrastructure.Data;

namespace QuanLyDKHP.App.ViewModels;

public partial class BaoCaoViewModel : ObservableObject, IRecipient<DataImportedNotificationMessage>
{
    private readonly IBaoCaoService _baoCaoService;
    private readonly IHocKyService _hocKyService;
    private readonly IMonHocService _monHocService;
    private readonly ILopHocPhanService _lopHocPhanService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly IMemoryCacheStore _cacheStore;
    private readonly Timer _svSearchDebounceTimer;
    private System.Threading.CancellationTokenSource? _loadCts;

    private static readonly string[] TenThuTuan =
        { "", "", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN" };

    // --- DỮ LIỆU DÙNG CHUNG ---
    public ObservableCollection<HocKy> DsHocKy { get; } = new();
    public ObservableCollection<MonHocDto> DsMonHoc { get; } = new();

    [ObservableProperty]
    private HocKy? _hocKyDangChon;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    // ==========================================
    // TAB 1: DS SINH VIÊN THEO MÔN
    // ==========================================
    [ObservableProperty]
    private MonHocDto? _tab1MonDangChon;

    public ObservableCollection<SinhVienTheoMonDto> Tab1DsSinhVien { get; } = new();

    // ==========================================
    // TAB 2: DANH SÁCH THI
    // ==========================================
    [ObservableProperty]
    private MonHocDto? _tab2MonDangChon;

    [ObservableProperty]
    private LopHocPhan? _tab2LhpDangChon;

    public ObservableCollection<LopHocPhan> Tab2DsLhp { get; } = new();
    public ObservableCollection<DanhSachThiDto> Tab2DsThi { get; } = new();

    // ==========================================
    // TAB 3: THỐNG KÊ SV THEO MÔN
    // ==========================================
    public ObservableCollection<ThongKeMonDto> Tab3DsThongKe { get; } = new();

    [ObservableProperty]
    private int _tab3TongLuotDangKy;

    [ObservableProperty]
    private int _tab3TongLopDangMo;

    // ==========================================
    // TAB 4: XƯỞNG IN PHIẾU ĐKHP & ẤN PHẨM HÀNG LOẠT (NÂNG CẤP 3 - MỤC TIÊU 2)
    // ==========================================
    [ObservableProperty]
    private string? _tab4TuKhoaSinhVien;

    [ObservableProperty]
    private bool _tab4IsGoiYOpen;

    [ObservableProperty]
    private SinhVienDto? _tab4SinhVienDangChon;

    public ObservableCollection<SinhVienDto> Tab4GoiYSinhVien { get; } = new();

    [ObservableProperty]
    private PhieuDangKyDto? _tab4PhieuDangKy;

    public ObservableCollection<string> DsLopSinhHoatInHangLoat { get; } = new();

    [ObservableProperty]
    private string? _tab4LopSinhHoatChon;

    // ==========================================
    // TAB 5: CẢNH BÁO HỌC VỤ & TÍN CHỈ (NÂNG CẤP 3 - MỤC TIÊU 3)
    // ==========================================
    private List<CanhBaoHocVuDto> _masterCanhBaoHocVu = new();
    public ObservableCollection<CanhBaoHocVuDto> Tab5DsCanhBao { get; } = new();

    [ObservableProperty]
    private int _tab5FilterIndex; // 0: Tất cả, 1: 0 Tín chỉ, 2: Thiếu TC tối thiểu, 3: Nợ học phí

    [ObservableProperty]
    private string? _tab5TuKhoa;

    [ObservableProperty]
    private int _tab5SoSvKhongDangKy;

    [ObservableProperty]
    private int _tab5SoSvThieuTinChi;

    [ObservableProperty]
    private int _tab5SoSvNoHocPhi;

    [ObservableProperty]
    private int _tab5TinChiToiThieuQuyDinh = 10;

    // ==========================================
    // TAB 6: RÀ SOÁT TRÙNG PHÒNG / GIẢNG VIÊN & ĐỊNH MỨC GIỜ GIẢNG (NÂNG CẤP 3 - MỤC TIÊU 1 & 4)
    // ==========================================
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTab6ConflictMode))]
    [NotifyPropertyChangedFor(nameof(IsTab6WorkloadMode))]
    private int _tab6ModeIndex; // 0: Rà soát xung đột Phòng/GV, 1: Định mức giờ giảng GV

    public bool IsTab6ConflictMode => Tab6ModeIndex == 0;
    public bool IsTab6WorkloadMode => Tab6ModeIndex == 1;

    public ObservableCollection<XungDotDaoTaoDto> Tab6DsXungDot { get; } = new();
    public ObservableCollection<DinhMucGiangDayDto> Tab6DsDinhMucGiangDay { get; } = new();

    [ObservableProperty]
    private int _tab6SoTrungGiangVien;

    [ObservableProperty]
    private int _tab6SoTrungPhong;

    [ObservableProperty]
    private int _tab6SoChuaHoanThien;

    [ObservableProperty]
    private int _tab6SoLopHopLe;

    public BaoCaoViewModel(
        IBaoCaoService baoCaoService,
        IHocKyService hocKyService,
        IMonHocService monHocService,
        ILopHocPhanService lopHocPhanService,
        ISinhVienService sinhVienService,
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService,
        IMemoryCacheStore cacheStore)
    {
        _baoCaoService = baoCaoService;
        _hocKyService = hocKyService;
        _monHocService = monHocService;
        _lopHocPhanService = lopHocPhanService;
        _sinhVienService = sinhVienService;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        _cacheStore = cacheStore;

        _svSearchDebounceTimer = new Timer(300) { AutoReset = false };
        _svSearchDebounceTimer.Elapsed += (s, e) =>
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(TimKiemSinhVienGoiYTab4Async);

        WeakReferenceMessenger.Default.RegisterAll(this);

        _ = InitDataAsync();
    }

    public void Receive(DataImportedNotificationMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            await InitDataAsync();
        });
    }

    public BaoCaoViewModel()
    {
        _baoCaoService = null!;
        _hocKyService = null!;
        _monHocService = null!;
        _lopHocPhanService = null!;
        _sinhVienService = null!;
        _pdfExportService = null!;
        _excelExportService = null!;
        _cacheStore = null!;
        _svSearchDebounceTimer = new Timer(300);
    }

    private async Task InitDataAsync()
    {
        if (_hocKyService == null) return;
        try
        {
            IsLoading = true;
            if (!_cacheStore.IsInitialized)
            {
                await _cacheStore.InitializeAsync();
            }

            DsHocKy.Clear();
            foreach (var hk in _cacheStore.DanhSachHocKy)
            {
                DsHocKy.Add(hk);
            }
            HocKyDangChon = DsHocKy.FirstOrDefault(h => h.DangMo) ?? DsHocKy.FirstOrDefault();

            DsMonHoc.Clear();
            foreach (var m in _cacheStore.DanhSachMonHoc)
            {
                DsMonHoc.Add(new MonHocDto
                {
                    MaMon = m.MaMon,
                    TenMon = m.TenMon,
                    SoTinChiLT = m.SoTinChiLT,
                    SoTinChiTH = m.SoTinChiTH,
                    BacDaoTao = m.BacDaoTao
                });
            }

            DsLopSinhHoatInHangLoat.Clear();
            foreach (var lop in _cacheStore.DanhSachLopSinhHoat.Where(l => !string.IsNullOrWhiteSpace(l) && l != "Tất cả"))
            {
                DsLopSinhHoatInHangLoat.Add(lop);
            }
            Tab4LopSinhHoatChon = DsLopSinhHoatInHangLoat.FirstOrDefault();

            Tab1MonDangChon = DsMonHoc.FirstOrDefault();
            Tab2MonDangChon = DsMonHoc.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        _ = LoadCurrentTabDataAsync();
    }

    partial void OnHocKyDangChonChanged(HocKy? value)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new System.Threading.CancellationTokenSource();
        _ = LoadCurrentTabDataAsync();
    }

    private async Task LoadCurrentTabDataAsync()
    {
        switch (SelectedTabIndex)
        {
            case 0:
                await LoadTab1DataAsync();
                break;
            case 1:
                await LoadTab2LhpListAsync();
                await LoadTab2DataAsync();
                break;
            case 2:
                await LoadTab3DataAsync();
                break;
            case 3:
                await LoadTab4DataAsync();
                break;
            case 4:
                await LoadTab5CanhBaoHocVuAsync();
                break;
            case 5:
                await LoadTab6DaoTaoVaGiangDayAsync();
                break;
        }
    }

    // ==========================================
    // TAB 1 METHODS: DS SINH VIÊN THEO MÔN
    // ==========================================
    partial void OnTab1MonDangChonChanged(MonHocDto? value)
    {
        _ = LoadTab1DataAsync();
    }

    [RelayCommand]
    public async Task LoadTab1DataAsync()
    {
        if (_baoCaoService == null || HocKyDangChon == null || Tab1MonDangChon == null) return;
        try
        {
            IsLoading = true;
            var list = await _baoCaoService.DsSinhVienTheoMonAsync(Tab1MonDangChon.MaMon, HocKyDangChon.MaHocKy);
            Tab1DsSinhVien.Clear();
            foreach (var item in list)
            {
                Tab1DsSinhVien.Add(item);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab1XuatExcelAsync()
    {
        if (IsLoading) return;
        var snapshot = Tab1DsSinhVien.ToList();
        if (snapshot.Count == 0)
        {
            ShowMessage("Không có dữ liệu để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        var hkSnapshot = HocKyDangChon;
        var monSnapshot = Tab1MonDangChon;

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<SinhVienTheoMonDto, object?> LayGiaTri)>
            {
                ("Mã SV", x => x.MaSV),
                ("Họ và tên", x => x.HoTen),
                ("Lớp sinh hoạt", x => x.LopSinhHoat ?? string.Empty),
                ("Mã LHP", x => x.MaLHP),
                ("Giảng viên", x => x.TenGiangVien ?? string.Empty),
                ("Trạng thái", x => x.IsDangHoc ? "Đang học" : "Đã hủy")
            };

            byte[] excelBytes = await _excelExportService.XuatExcelAsync(
                $"DSSV_{monSnapshot?.MaMon}",
                snapshot,
                cotMap);

            string fileName = $"DSSV_{monSnapshot?.MaMon}_{hkSnapshot?.MaHocKy}.xlsx";
            string? saved = await SaveFileDialogFunc(fileName, "xlsx", excelBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất danh sách Excel thành công tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // TAB 2 METHODS: DANH SÁCH THI
    // ==========================================
    partial void OnTab2MonDangChonChanged(MonHocDto? value)
    {
        _ = LoadTab2LhpListAsync();
    }

    partial void OnTab2LhpDangChonChanged(LopHocPhan? value)
    {
        _ = LoadTab2DataAsync();
    }

    private async Task LoadTab2LhpListAsync()
    {
        if (_lopHocPhanService == null || HocKyDangChon == null || Tab2MonDangChon == null) return;
        try
        {
            var lhps = await _lopHocPhanService.LayTheoHocKyAsync(HocKyDangChon.MaHocKy, null, Tab2MonDangChon.MaMon);
            Tab2DsLhp.Clear();
            foreach (var lhp in lhps)
            {
                Tab2DsLhp.Add(lhp);
            }
            Tab2LhpDangChon = null;
            await LoadTab2DataAsync();
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    public async Task LoadTab2DataAsync()
    {
        if (_baoCaoService == null || HocKyDangChon == null || Tab2MonDangChon == null) return;
        try
        {
            IsLoading = true;
            var list = await _baoCaoService.DsThiTheoMonAsync(
                Tab2MonDangChon.MaMon,
                HocKyDangChon.MaHocKy,
                Tab2LhpDangChon?.MaLHP);

            Tab2DsThi.Clear();
            foreach (var item in list)
            {
                Tab2DsThi.Add(item);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab2XuatExcelAsync()
    {
        if (IsLoading) return;
        var snapshot = Tab2DsThi.ToList();
        if (snapshot.Count == 0)
        {
            ShowMessage("Không có dữ liệu danh sách thi để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        var hkSnapshot = HocKyDangChon;
        var monSnapshot = Tab2MonDangChon;
        var lhpSnapshot = Tab2LhpDangChon;

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<DanhSachThiDto, object?> LayGiaTri)>
            {
                ("STT", x => x.Stt),
                ("Mã SV", x => x.MaSV),
                ("Họ và tên", x => x.HoTen),
                ("Lớp sinh hoạt", x => x.LopSinhHoat ?? string.Empty),
                ("Mã LHP", x => x.MaLHP),
                ("Giảng viên", x => x.TenGiangVien ?? string.Empty),
                ("Điểm", x => string.Empty),
                ("Chữ ký", x => string.Empty),
                ("Ghi chú", x => string.Empty)
            };

            byte[] excelBytes = await _excelExportService.XuatExcelAsync(
                $"DSThi_{monSnapshot?.MaMon}",
                snapshot,
                cotMap);

            string lhpSuffix = lhpSnapshot != null ? $"_{lhpSnapshot.MaLHP}" : "";
            string fileName = $"DanhSachThi_{monSnapshot?.MaMon}{lhpSuffix}_{hkSnapshot?.MaHocKy}.xlsx";
            string? saved = await SaveFileDialogFunc(fileName, "xlsx", excelBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất danh sách thi Excel thành công tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab2XuatPdfAsync()
    {
        if (Tab2DsThi.Count == 0)
        {
            ShowMessage("Không có dữ liệu danh sách thi để xuất PDF.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        try
        {
            IsLoading = true;
            string tenMon = Tab2MonDangChon?.TenMon ?? "";
            string? maLhp = Tab2LhpDangChon?.MaLHP;
            string tenHk = HocKyDangChon?.TenHocKy ?? "";

            byte[] pdfBytes = await _pdfExportService.XuatDanhSachThiPdfAsync(
                Tab2DsThi.ToList(),
                tenMon,
                maLhp,
                tenHk);

            string lhpSuffix = !string.IsNullOrEmpty(maLhp) ? $"_{maLhp}" : "";
            string fileName = $"DanhSachThi_{Tab2MonDangChon?.MaMon}{lhpSuffix}_{HocKyDangChon?.MaHocKy}.pdf";
            string? saved = await SaveFileDialogFunc(fileName, "pdf", pdfBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất danh sách thi PDF thành công tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // TAB 3 METHODS: THỐNG KÊ SV THEO MÔN
    // ==========================================
    [RelayCommand]
    public async Task LoadTab3DataAsync()
    {
        if (_baoCaoService == null || HocKyDangChon == null) return;
        try
        {
            IsLoading = true;
            var list = await _baoCaoService.ThongKeSoLuongTheoMonAsync(HocKyDangChon.MaHocKy);
            int maxSv = list.Count > 0 ? Math.Max(1, list.Max(x => x.SoSinhVien)) : 1;

            Tab3DsThongKe.Clear();
            foreach (var item in list)
            {
                item.TyLePhanBo = Math.Round((double)item.SoSinhVien / maxSv * 100.0, 1);
                Tab3DsThongKe.Add(item);
            }

            Tab3TongLuotDangKy = list.Sum(x => x.SoSinhVien);
            Tab3TongLopDangMo = list.Sum(x => x.SoLhpDangMo);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab3XuatExcelAsync()
    {
        if (IsLoading) return;
        var snapshot = Tab3DsThongKe.ToList();
        if (snapshot.Count == 0)
        {
            ShowMessage("Không có dữ liệu thống kê để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        var hkSnapshot = HocKyDangChon;

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<ThongKeMonDto, object?> LayGiaTri)>
            {
                ("Mã môn", x => x.MaMon),
                ("Tên môn học", x => x.TenMon),
                ("Số SV đăng ký", x => x.SoSinhVien),
                ("Số LHP đang mở", x => x.SoLhpDangMo),
                ("Bình quân / Lớp", x => x.BinhQuanMoiLop)
            };

            byte[] excelBytes = await _excelExportService.XuatExcelAsync(
                $"ThongKe_{hkSnapshot?.MaHocKy}",
                snapshot,
                cotMap);

            string fileName = $"ThongKeSVTheoMon_{hkSnapshot?.MaHocKy}.xlsx";
            string? saved = await SaveFileDialogFunc(fileName, "xlsx", excelBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất thống kê Excel thành công tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // TAB 4 METHODS: XƯỞNG IN PHIẾU ĐKHP & IN HÀNG LOẠT THEO LỚP SINH HOẠT
    // ==========================================
    partial void OnTab4TuKhoaSinhVienChanged(string? value)
    {
        _svSearchDebounceTimer.Stop();
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
        {
            Tab4GoiYSinhVien.Clear();
            Tab4IsGoiYOpen = false;
            return;
        }
        _svSearchDebounceTimer.Start();
    }

    private async Task TimKiemSinhVienGoiYTab4Async()
    {
        if (string.IsNullOrWhiteSpace(Tab4TuKhoaSinhVien) || Tab4TuKhoaSinhVien.Trim().Length < 2)
        {
            Tab4GoiYSinhVien.Clear();
            Tab4IsGoiYOpen = false;
            return;
        }

        try
        {
            var result = await _sinhVienService.TimKiemAsync(Tab4TuKhoaSinhVien.Trim(), null, null, 1, 15);
            Tab4GoiYSinhVien.Clear();
            foreach (var sv in result.Items)
            {
                Tab4GoiYSinhVien.Add(sv);
            }
            Tab4IsGoiYOpen = Tab4GoiYSinhVien.Count > 0;
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
    }

    [RelayCommand]
    private async Task Tab4ChonSinhVienAsync(SinhVienDto sv)
    {
        if (sv == null) return;
        Tab4SinhVienDangChon = sv;
        Tab4IsGoiYOpen = false;
        Tab4TuKhoaSinhVien = $"{sv.MaSV} - {sv.HoTen}";

        await LoadTab4DataAsync();
    }

    [RelayCommand]
    public async Task LoadTab4DataAsync()
    {
        if (_baoCaoService == null)
        {
            ShowMessage("Không thể tải dữ liệu phiếu ĐKHP vì BaoCaoService chưa được khởi tạo.", true);
            return;
        }

        if (Tab4SinhVienDangChon == null)
        {
            Tab4PhieuDangKy = null;
            return;
        }

        if (HocKyDangChon == null)
        {
            Tab4PhieuDangKy = null;
            ShowMessage("Vui lòng chọn học kỳ.", true);
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = null;

            var phieu = await _baoCaoService.LayPhieuDangKyAsync(
                Tab4SinhVienDangChon.MaSV,
                HocKyDangChon.MaHocKy);

            Tab4PhieuDangKy = phieu;

            if (phieu == null || phieu.DanhSachMon.Count == 0)
            {
                ShowMessage(
                    $"Sinh viên {Tab4SinhVienDangChon.MaSV} không có đăng ký học phần trong học kỳ {HocKyDangChon.TenHocKy}.",
                    true);
                return;
            }

            ShowMessage(
                $"Đã tải phiếu ĐKHP của {Tab4SinhVienDangChon.MaSV} – {HocKyDangChon.TenHocKy}.",
                false);
        }
        catch (Exception ex)
        {
            Tab4PhieuDangKy = null;
            ShowMessage($"Không thể tải lại phiếu kết quả ĐKHP: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab4XuatPdfAsync()
    {
        if (Tab4PhieuDangKy == null || Tab4PhieuDangKy.DanhSachMon.Count == 0)
        {
            ShowMessage("Không có dữ liệu phiếu đăng ký học phần để xuất PDF.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        try
        {
            IsLoading = true;
            byte[] pdfBytes = await _pdfExportService.XuatPhieuKetQuaDKHPPdfAsync(Tab4PhieuDangKy);
            string fileName = $"PhieuKetQuaDKHP_{Tab4PhieuDangKy.MaSV}_{Tab4PhieuDangKy.MaHocKy}.pdf";
            string? saved = await SaveFileDialogFunc(fileName, "pdf", pdfBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"Đã xuất phiếu kết quả ĐKHP PDF thành công tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab4XuatPdfHangLoatTheoLopAsync()
    {
        if (IsLoading || _baoCaoService == null || _sinhVienService == null) return;
        if (HocKyDangChon == null)
        {
            ShowMessage("Vui lòng chọn học kỳ cần xuất ấn phẩm.", true);
            return;
        }
        if (string.IsNullOrWhiteSpace(Tab4LopSinhHoatChon))
        {
            ShowMessage("Vui lòng chọn Lớp sinh hoạt cần in hàng loạt.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        try
        {
            IsLoading = true;
            string lopChon = Tab4LopSinhHoatChon.Trim();
            string maHk = HocKyDangChon.MaHocKy;

            var pagedSv = await _sinhVienService.TimKiemAsync(null, lopChon, null, 1, 500);
            var danhSachPhieu = new List<PhieuDangKyDto>();

            foreach (var sv in pagedSv.Items.OrderBy(s => s.MaSV))
            {
                var phieu = await _baoCaoService.LayPhieuDangKyAsync(sv.MaSV, maHk);
                if (phieu != null && phieu.DanhSachMon.Count > 0)
                {
                    danhSachPhieu.Add(phieu);
                }
            }

            if (danhSachPhieu.Count == 0)
            {
                ShowMessage($"Lớp sinh hoạt {lopChon} chưa có sinh viên nào đăng ký học phần trong {HocKyDangChon.TenHocKy}.", true);
                return;
            }

            byte[] pdfBytes = await _pdfExportService.XuatPhieuKetQuaDKHPHangLoatPdfAsync(danhSachPhieu);
            string fileName = $"PhieuDKHP_HangLoat_{lopChon}_{maHk}.pdf";
            string? saved = await SaveFileDialogFunc(fileName, "pdf", pdfBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"✔ Đã đóng gói {danhSachPhieu.Count} trang phiếu ĐKHP của lớp {lopChon} vào 1 file PDF tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi xuất PDF hàng loạt: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // TAB 5 METHODS: TRUNG TÂM CẢNH BÁO HỌC VỤ & SINH VIÊN NGUY CƠ (NÂNG CẤP 3 - MỤC TIÊU 3)
    // ==========================================
    partial void OnTab5FilterIndexChanged(int value)
    {
        ApplyTab5Filter();
    }

    partial void OnTab5TuKhoaChanged(string? value)
    {
        ApplyTab5Filter();
    }

    [RelayCommand]
    public async Task LoadTab5CanhBaoHocVuAsync()
    {
        if (HocKyDangChon == null || App.ServiceProvider == null) return;
        try
        {
            IsLoading = true;
            var dbFactory = App.ServiceProvider.GetService<IDbContextFactory<AppDbContext>>();
            if (dbFactory == null) return;

            await using var db = await dbFactory.CreateDbContextAsync();
            string maHk = HocKyDangChon.MaHocKy;

            var cauHinhDict = await db.CauHinhHeThongs.AsNoTracking()
                .ToDictionaryAsync(c => c.Key, c => c.Value);

            int tcToiThieu = 10;
            if (cauHinhDict.TryGetValue("SoTinChiToiThieu", out var strMin) && int.TryParse(strMin, out int parsedMin) && parsedMin > 0)
            {
                tcToiThieu = parsedMin;
            }
            Tab5TinChiToiThieuQuyDinh = tcToiThieu;

            var allStudents = await db.SinhViens.AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.LopSinhHoat)
                .ThenBy(s => s.MaSV)
                .ToListAsync();

            var enrollments = await db.DangKyHocPhans.AsNoTracking()
                .Include(d => d.LopHocPhan)
                .ThenInclude(l => l.MonHoc)
                .Where(d => d.TrangThai == "DangHoc"
                            && d.LopHocPhan != null
                            && !d.LopHocPhan.IsDeleted
                            && d.LopHocPhan.MaHocKy == maHk)
                .ToListAsync();

            var tcBySv = enrollments
                .GroupBy(d => d.MaSV)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d => (d.LopHocPhan?.MonHoc?.SoTinChiLT ?? 0) + (d.LopHocPhan?.MonHoc?.SoTinChiTH ?? 0)));

            var debtBySv = await db.HocPhiHocKys.AsNoTracking()
                .Where(h => h.MaHocKy == maHk && h.ConNo > 0)
                .ToDictionaryAsync(h => h.MaSV, h => h.ConNo);

            var alerts = new List<CanhBaoHocVuDto>();
            int count0Tc = 0;
            int countLowTc = 0;
            int countDebt = 0;

            foreach (var sv in allStudents)
            {
                int soTc = tcBySv.TryGetValue(sv.MaSV, out int tc) ? tc : 0;
                decimal conNo = debtBySv.TryGetValue(sv.MaSV, out decimal no) ? no : 0m;

                if (soTc == 0)
                {
                    count0Tc++;
                    alerts.Add(new CanhBaoHocVuDto
                    {
                        MaSV = sv.MaSV,
                        HoTen = sv.HoTen,
                        LopSinhHoat = sv.LopSinhHoat ?? "--",
                        KhoaHoc = sv.KhoaHoc ?? "--",
                        SoTinChiDangKy = 0,
                        TinChiToiThieu = tcToiThieu,
                        ConNoHocPhi = conNo,
                        LoaiNguyCo = "KhongDangKy",
                        NhanNguyCo = "Mất tích (0 TC)",
                        HuongXuLyDeXuat = "Gửi CVHT liên hệ gấp để nhắc ĐKHP hoặc làm thủ tục bảo lưu"
                    });
                }
                else if (soTc < tcToiThieu)
                {
                    countLowTc++;
                    alerts.Add(new CanhBaoHocVuDto
                    {
                        MaSV = sv.MaSV,
                        HoTen = sv.HoTen,
                        LopSinhHoat = sv.LopSinhHoat ?? "--",
                        KhoaHoc = sv.KhoaHoc ?? "--",
                        SoTinChiDangKy = soTc,
                        TinChiToiThieu = tcToiThieu,
                        ConNoHocPhi = conNo,
                        LoaiNguyCo = "ThieuTinChi",
                        NhanNguyCo = $"Thiếu {tcToiThieu - soTc} TC tối thiểu",
                        HuongXuLyDeXuat = $"Yêu cầu đăng ký bổ sung tối thiểu {tcToiThieu - soTc} tín chỉ theo quy chế"
                    });
                }
                else if (conNo > 0)
                {
                    countDebt++;
                    alerts.Add(new CanhBaoHocVuDto
                    {
                        MaSV = sv.MaSV,
                        HoTen = sv.HoTen,
                        LopSinhHoat = sv.LopSinhHoat ?? "--",
                        KhoaHoc = sv.KhoaHoc ?? "--",
                        SoTinChiDangKy = soTc,
                        TinChiToiThieu = tcToiThieu,
                        ConNoHocPhi = conNo,
                        LoaiNguyCo = "NoHocPhi",
                        NhanNguyCo = "Nợ học phí quá hạn",
                        HuongXuLyDeXuat = "Nhắc hoàn tất học phí trước khi lập danh sách cấm thi"
                    });
                }
            }

            _masterCanhBaoHocVu = alerts;
            Tab5SoSvKhongDangKy = count0Tc;
            Tab5SoSvThieuTinChi = countLowTc;
            Tab5SoSvNoHocPhi = countDebt;

            ApplyTab5Filter();
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi rà soát cảnh báo học vụ: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyTab5Filter()
    {
        IEnumerable<CanhBaoHocVuDto> query = _masterCanhBaoHocVu;

        switch (Tab5FilterIndex)
        {
            case 1:
                query = query.Where(x => x.LoaiNguyCo == "KhongDangKy");
                break;
            case 2:
                query = query.Where(x => x.LoaiNguyCo == "ThieuTinChi");
                break;
            case 3:
                query = query.Where(x => x.LoaiNguyCo == "NoHocPhi");
                break;
        }

        if (!string.IsNullOrWhiteSpace(Tab5TuKhoa))
        {
            string kw = Tab5TuKhoa.Trim();
            query = query.Where(x =>
                x.MaSV.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.HoTen.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.LopSinhHoat.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();
        for (int i = 0; i < filtered.Count; i++)
        {
            filtered[i].Stt = i + 1;
        }

        Tab5DsCanhBao.Clear();
        foreach (var item in filtered)
        {
            Tab5DsCanhBao.Add(item);
        }
    }

    [RelayCommand]
    private async Task Tab5XuatExcelCanhBaoAsync()
    {
        if (IsLoading) return;
        var snapshot = Tab5DsCanhBao.ToList();
        if (snapshot.Count == 0)
        {
            ShowMessage("Không có danh sách cảnh báo học vụ để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<CanhBaoHocVuDto, object?> LayGiaTri)>
            {
                ("STT", x => x.Stt),
                ("Mã SV", x => x.MaSV),
                ("Họ và tên", x => x.HoTen),
                ("Lớp sinh hoạt", x => x.LopSinhHoat),
                ("Khóa học", x => x.KhoaHoc),
                ("Tín chỉ ĐK", x => x.SoTinChiDangKy),
                ("TC Tối thiểu", x => x.TinChiToiThieu),
                ("Còn nợ học phí", x => x.ConNoHocPhi),
                ("Mức cảnh báo", x => x.NhanNguyCo),
                ("Đề xuất xử lý", x => x.HuongXuLyDeXuat)
            };

            string maHk = HocKyDangChon?.MaHocKy ?? "HK";
            byte[] excelBytes = await _excelExportService.XuatExcelAsync($"CanhBaoHocVu_{maHk}", snapshot, cotMap);
            string fileName = $"DanhSachCanhBaoHocVu_{maHk}.xlsx";
            string? saved = await SaveFileDialogFunc(fileName, "xlsx", excelBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"✔ Đã xuất danh sách cảnh báo học vụ gửi CVHT tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // TAB 6 METHODS: RÀ SOÁT TRÙNG PHÒNG / GIẢNG VIÊN & ĐỊNH MỨC GIỜ GIẢNG (NÂNG CẤP 3 - MỤC TIÊU 1 & 4)
    // ==========================================
    [RelayCommand]
    public async Task LoadTab6DaoTaoVaGiangDayAsync()
    {
        if (HocKyDangChon == null || App.ServiceProvider == null) return;
        try
        {
            IsLoading = true;
            var dbFactory = App.ServiceProvider.GetService<IDbContextFactory<AppDbContext>>();
            if (dbFactory == null) return;

            await using var db = await dbFactory.CreateDbContextAsync();
            string maHk = HocKyDangChon.MaHocKy;

            var lhps = await db.LopHocPhans.AsNoTracking()
                .Include(l => l.MonHoc)
                .Where(l => !l.IsDeleted && l.MaHocKy == maHk)
                .OrderBy(l => l.MaLHP)
                .ToListAsync();

            var gvUsers = await db.NguoiDungs.AsNoTracking()
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            var gvNameDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var u in gvUsers)
            {
                if (!string.IsNullOrWhiteSpace(u.MaGV))
                    gvNameDict[u.MaGV] = u.HoTen;
                if (!string.IsNullOrWhiteSpace(u.TenDangNhap))
                    gvNameDict[u.TenDangNhap] = u.HoTen;
            }

            var siSoByLhp = await db.DangKyHocPhans.AsNoTracking()
                .Where(d => d.TrangThai == "DangHoc" && d.LopHocPhan != null && d.LopHocPhan.MaHocKy == maHk)
                .GroupBy(d => d.MaLHP)
                .Select(g => new { MaLHP = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MaLHP, x => x.Count);

            // --- PHẦN 1: QUÉT XUNG ĐỘT PHÒNG HỌC & GIẢNG VIÊN ---
            var conflicts = new List<XungDotDaoTaoDto>();
            var lhpIdsWithIssue = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int countGvOverlap = 0;
            int countRoomOverlap = 0;
            int countIncomplete = 0;

            for (int i = 0; i < lhps.Count; i++)
            {
                var a = lhps[i];
                if (!a.Thu.HasValue || !a.TietBatDau.HasValue || !a.SoTiet.HasValue)
                    continue;

                int startA = a.TietBatDau.Value;
                int endA = startA + a.SoTiet.Value - 1;
                string thuText = (a.Thu.Value >= 2 && a.Thu.Value <= 8) ? TenThuTuan[a.Thu.Value] : $"Thứ {a.Thu.Value}";

                for (int j = i + 1; j < lhps.Count; j++)
                {
                    var b = lhps[j];
                    if (!b.Thu.HasValue || !b.TietBatDau.HasValue || !b.SoTiet.HasValue)
                        continue;
                    if (a.Thu.Value != b.Thu.Value)
                        continue;

                    int startB = b.TietBatDau.Value;
                    int endB = startB + b.SoTiet.Value - 1;
                    bool overlap = startA <= endB && startB <= endA;
                    if (!overlap) continue;

                    string giaoTiet = $"Tiết {Math.Max(startA, startB)}–{Math.Min(endA, endB)}";

                    // 1.1. Kiểm tra trùng Giảng viên
                    if (!string.IsNullOrWhiteSpace(a.MaGV) &&
                        string.Equals(a.MaGV.Trim(), b.MaGV?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        countGvOverlap++;
                        lhpIdsWithIssue.Add(a.MaLHP);
                        lhpIdsWithIssue.Add(b.MaLHP);
                        string tenGv = gvNameDict.TryGetValue(a.MaGV.Trim(), out var name) ? $"{name} ({a.MaGV})" : a.MaGV;

                        conflicts.Add(new XungDotDaoTaoDto
                        {
                            LoaiXungDot = "TrungGiangVien",
                            TenLoaiHienThi = "Trùng lịch Giảng viên",
                            MucDo = "NghiemTrong",
                            DoiTuongXungDot = $"GV: {tenGv}",
                            MaLhpChinh = a.MaLHP,
                            TenMonChinh = a.MonHoc?.TenMon ?? a.MaMon,
                            MaLhpDoiUng = b.MaLHP,
                            TenMonDoiUng = b.MonHoc?.TenMon ?? b.MaMon,
                            ThoiGianDienRa = $"{thuText}, {giaoTiet}",
                            KhuyenNghiXuLy = $"Đổi ca giảng hoặc phân công giảng viên khác cho lớp {b.MaLHP}"
                        });
                    }

                    // 1.2. Kiểm tra trùng Phòng học (chỉ tính lớp học trực tiếp tại phòng)
                    if (!a.GiangDayOnline && !b.GiangDayOnline &&
                        !string.IsNullOrWhiteSpace(a.Phong) &&
                        string.Equals(a.Phong.Trim(), b.Phong?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        countRoomOverlap++;
                        lhpIdsWithIssue.Add(a.MaLHP);
                        lhpIdsWithIssue.Add(b.MaLHP);

                        conflicts.Add(new XungDotDaoTaoDto
                        {
                            LoaiXungDot = "TrungPhongHoc",
                            TenLoaiHienThi = "Trùng Phòng học",
                            MucDo = "NghiemTrong",
                            DoiTuongXungDot = $"Phòng {a.Phong.Trim()}",
                            MaLhpChinh = a.MaLHP,
                            TenMonChinh = a.MonHoc?.TenMon ?? a.MaMon,
                            MaLhpDoiUng = b.MaLHP,
                            TenMonDoiUng = b.MonHoc?.TenMon ?? b.MaMon,
                            ThoiGianDienRa = $"{thuText}, {giaoTiet}",
                            KhuyenNghiXuLy = $"Chuyển lớp {b.MaLHP} sang phòng học trống khác hoặc đổi tiết"
                        });
                    }
                }
            }

            // 1.3. Kiểm tra các lớp chưa xếp đủ thông tin (Lịch / Phòng / Giảng viên)
            foreach (var lhp in lhps)
            {
                var missingParts = new List<string>();
                if (string.IsNullOrWhiteSpace(lhp.MaGV))
                    missingParts.Add("Chưa phân công GV");
                if (!lhp.Thu.HasValue || !lhp.TietBatDau.HasValue || !lhp.SoTiet.HasValue)
                    missingParts.Add("Chưa xếp Thứ/Tiết");
                else if (!lhp.GiangDayOnline && string.IsNullOrWhiteSpace(lhp.Phong))
                    missingParts.Add("Chưa xếp Phòng học");

                if (missingParts.Count > 0)
                {
                    countIncomplete++;
                    lhpIdsWithIssue.Add(lhp.MaLHP);
                    string thuHienTai = (lhp.Thu.HasValue && lhp.Thu.Value >= 2 && lhp.Thu.Value <= 8 && lhp.TietBatDau.HasValue && lhp.SoTiet.HasValue)
                        ? $"{TenThuTuan[lhp.Thu.Value]}, tiết {lhp.TietBatDau}-{lhp.TietBatDau + lhp.SoTiet - 1}"
                        : "Chưa xếp lịch";

                    conflicts.Add(new XungDotDaoTaoDto
                    {
                        LoaiXungDot = "ChuaHoanThien",
                        TenLoaiHienThi = "Thiếu thông tin LHP",
                        MucDo = "CanhBao",
                        DoiTuongXungDot = string.Join(" • ", missingParts),
                        MaLhpChinh = lhp.MaLHP,
                        TenMonChinh = lhp.MonHoc?.TenMon ?? lhp.MaMon,
                        MaLhpDoiUng = "--",
                        TenMonDoiUng = string.Empty,
                        ThoiGianDienRa = thuHienTai,
                        KhuyenNghiXuLy = "Vào màn hình Học kỳ / LHP cập nhật đầy đủ Giảng viên, Thứ, Tiết và Phòng học"
                    });
                }
            }

            for (int idx = 0; idx < conflicts.Count; idx++)
            {
                conflicts[idx].Stt = idx + 1;
            }

            Tab6DsXungDot.Clear();
            foreach (var c in conflicts)
            {
                Tab6DsXungDot.Add(c);
            }

            Tab6SoTrungGiangVien = countGvOverlap;
            Tab6SoTrungPhong = countRoomOverlap;
            Tab6SoChuaHoanThien = countIncomplete;
            Tab6SoLopHopLe = Math.Max(0, lhps.Count - lhpIdsWithIssue.Count);

            // --- PHẦN 2: BẢNG THEO DÕI ĐỊNH MỨC & GIỜ GIẢNG GIẢNG VIÊN ---
            var workloads = new List<DinhMucGiangDayDto>();
            var groupedByGv = lhps
                .GroupBy(l => string.IsNullOrWhiteSpace(l.MaGV) ? "CHUA_PHAN_CONG" : l.MaGV.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key == "CHUA_PHAN_CONG" ? 1 : 0)
                .ThenBy(g => g.Key);

            int sttGv = 1;
            foreach (var group in groupedByGv)
            {
                string maGv = group.Key;
                string hoTenGv = maGv == "CHUA_PHAN_CONG"
                    ? "Chưa phân công giảng viên"
                    : (gvNameDict.TryGetValue(maGv, out var ten) ? ten : $"Giảng viên ({maGv})");

                int soLop = 0;
                int tongSv = 0;
                int tietLt = 0;
                int tietTh = 0;
                var lopNames = new List<string>();

                foreach (var lhp in group)
                {
                    soLop++;
                    int svCount = siSoByLhp.TryGetValue(lhp.MaLHP, out int c) ? c : 0;
                    tongSv += svCount;

                    int tcLt = lhp.MonHoc?.SoTinChiLT ?? 0;
                    int tcTh = lhp.MonHoc?.SoTinChiTH ?? 0;
                    tietLt += tcLt * 15;
                    tietTh += tcTh * 30;
                    lopNames.Add($"{lhp.MaLHP} ({svCount} SV)");
                }

                double quyDoi = Math.Round(tietLt * 1.0 + tietTh * 0.7, 1);
                string trangThai;
                string nhan;

                if (maGv == "CHUA_PHAN_CONG")
                {
                    trangThai = "ThieuGio";
                    nhan = "Cần phân công GV";
                }
                else if (quyDoi > 180.0)
                {
                    trangThai = "QuaTai";
                    nhan = "Vượt định mức (Quá tải)";
                }
                else if (quyDoi >= 90.0)
                {
                    trangThai = "DatChuan";
                    nhan = "Đạt định mức chuẩn";
                }
                else
                {
                    trangThai = "ThieuGio";
                    nhan = "Chưa đủ giờ chuẩn";
                }

                workloads.Add(new DinhMucGiangDayDto
                {
                    Stt = sttGv++,
                    MaGV = maGv == "CHUA_PHAN_CONG" ? "--" : maGv,
                    HoTenGV = hoTenGv,
                    SoLopPhuTrach = soLop,
                    TongSinhVien = tongSv,
                    TietLyThuyet = tietLt,
                    TietThucHanh = tietTh,
                    TongTietQuyDoi = quyDoi,
                    DinhMucChuan = 135.0,
                    TrangThaiTai = trangThai,
                    NhanTrangThai = nhan,
                    DanhSachLopPhuTrach = string.Join(", ", lopNames.Take(5)) + (lopNames.Count > 5 ? $" (+{lopNames.Count - 5} lớp)" : "")
                });
            }

            Tab6DsDinhMucGiangDay.Clear();
            foreach (var w in workloads)
            {
                Tab6DsDinhMucGiangDay.Add(w);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi phân tích lịch đào tạo & giờ giảng: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab6XuatExcelXungDotAsync()
    {
        if (IsLoading) return;
        var snapshot = Tab6DsXungDot.ToList();
        if (snapshot.Count == 0)
        {
            ShowMessage("Không có xung đột nào để xuất biên bản Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<XungDotDaoTaoDto, object?> LayGiaTri)>
            {
                ("STT", x => x.Stt),
                ("Loại rà soát", x => x.TenLoaiHienThi),
                ("Đối tượng / Chi tiết", x => x.DoiTuongXungDot),
                ("Lớp học phần 1", x => x.MaLhpChinh),
                ("Môn học 1", x => x.TenMonChinh),
                ("Lớp học phần 2", x => x.MaLhpDoiUng),
                ("Thời gian", x => x.ThoiGianDienRa),
                ("Khuyến nghị xử lý", x => x.KhuyenNghiXuLy)
            };

            string maHk = HocKyDangChon?.MaHocKy ?? "HK";
            byte[] excelBytes = await _excelExportService.XuatExcelAsync($"RaSoatLich_{maHk}", snapshot, cotMap);
            string fileName = $"BienBanRaSoatXungDotLich_{maHk}.xlsx";
            string? saved = await SaveFileDialogFunc(fileName, "xlsx", excelBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"✔ Đã xuất biên bản rà soát xung đột lịch học tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Tab6XuatExcelGiangDayAsync()
    {
        if (IsLoading) return;
        var snapshot = Tab6DsDinhMucGiangDay.ToList();
        if (snapshot.Count == 0)
        {
            ShowMessage("Không có dữ liệu định mức giảng dạy để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<DinhMucGiangDayDto, object?> LayGiaTri)>
            {
                ("STT", x => x.Stt),
                ("Mã GV", x => x.MaGV),
                ("Họ và tên Giảng viên", x => x.HoTenGV),
                ("Số LHP phụ trách", x => x.SoLopPhuTrach),
                ("Tổng SV theo học", x => x.TongSinhVien),
                ("Tiết Lý thuyết", x => x.TietLyThuyet),
                ("Tiết Thực hành", x => x.TietThucHanh),
                ("Tổng tiết quy đổi", x => x.TongTietQuyDoi),
                ("Định mức chuẩn", x => x.DinhMucChuan),
                ("Đánh giá định mức", x => x.NhanTrangThai),
                ("Danh sách lớp", x => x.DanhSachLopPhuTrach)
            };

            string maHk = HocKyDangChon?.MaHocKy ?? "HK";
            byte[] excelBytes = await _excelExportService.XuatExcelAsync($"GioGiang_{maHk}", snapshot, cotMap);
            string fileName = $"BangThongKeGioGiangGiangVien_{maHk}.xlsx";
            string? saved = await SaveFileDialogFunc(fileName, "xlsx", excelBytes);
            if (!string.IsNullOrEmpty(saved))
            {
                ShowMessage($"✔ Đã xuất bảng tổng hợp định mức giờ giảng tại: {saved}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}
