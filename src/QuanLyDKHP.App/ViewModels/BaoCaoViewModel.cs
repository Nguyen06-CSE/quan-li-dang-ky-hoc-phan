using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class BaoCaoViewModel : ObservableObject
{
    private readonly IBaoCaoService _baoCaoService;
    private readonly IHocKyService _hocKyService;
    private readonly IMonHocService _monHocService;
    private readonly ILopHocPhanService _lopHocPhanService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly Timer _svSearchDebounceTimer;

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

    // ==========================================
    // TAB 4: IN PHIẾU KẾT QUẢ ĐKHP
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

    public BaoCaoViewModel(
        IBaoCaoService baoCaoService,
        IHocKyService hocKyService,
        IMonHocService monHocService,
        ILopHocPhanService lopHocPhanService,
        ISinhVienService sinhVienService,
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService)
    {
        _baoCaoService = baoCaoService;
        _hocKyService = hocKyService;
        _monHocService = monHocService;
        _lopHocPhanService = lopHocPhanService;
        _sinhVienService = sinhVienService;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;

        _svSearchDebounceTimer = new Timer(300) { AutoReset = false };
        _svSearchDebounceTimer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(TimKiemSinhVienGoiYTab4Async);

        _ = InitDataAsync();
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
        _svSearchDebounceTimer = new Timer(300);
    }

    private async Task InitDataAsync()
    {
        if (_hocKyService == null) return;
        try
        {
            IsLoading = true;
            var hks = await _hocKyService.LayTatCaAsync();
            DsHocKy.Clear();
            foreach (var hk in hks)
            {
                DsHocKy.Add(hk);
            }
            HocKyDangChon = hks.FirstOrDefault(h => h.DangMo) ?? hks.FirstOrDefault();

            var mons = await _monHocService.LayDanhSachDtoAsync(null);
            DsMonHoc.Clear();
            foreach (var m in mons)
            {
                DsMonHoc.Add(m);
            }

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
        }
    }

    // ==========================================
    // TAB 1 METHODS
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
        if (Tab1DsSinhVien.Count == 0)
        {
            ShowMessage("Không có dữ liệu để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

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
                ("Trạng thái", x => x.TrangThai == "DangHoc" ? "Đang học" : "Đã hủy")
            };

            byte[] excelBytes = await _excelExportService.XuatExcelAsync(
                $"DSSV_{Tab1MonDangChon?.MaMon}",
                Tab1DsSinhVien,
                cotMap);

            string fileName = $"DSSV_{Tab1MonDangChon?.MaMon}_{HocKyDangChon?.MaHocKy}.xlsx";
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
    // TAB 2 METHODS
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
            Tab2LhpDangChon = null; // Tất cả LHP
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
        if (Tab2DsThi.Count == 0)
        {
            ShowMessage("Không có dữ liệu danh sách thi để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

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
                ("Điểm", x => string.Empty),
                ("Chữ ký", x => string.Empty),
                ("Ghi chú", x => string.Empty)
            };

            byte[] excelBytes = await _excelExportService.XuatExcelAsync(
                $"DSThi_{Tab2MonDangChon?.MaMon}",
                Tab2DsThi,
                cotMap);

            string lhpSuffix = Tab2LhpDangChon != null ? $"_{Tab2LhpDangChon.MaLHP}" : "";
            string fileName = $"DanhSachThi_{Tab2MonDangChon?.MaMon}{lhpSuffix}_{HocKyDangChon?.MaHocKy}.xlsx";
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
    // TAB 3 METHODS
    // ==========================================
    [RelayCommand]
    public async Task LoadTab3DataAsync()
    {
        if (_baoCaoService == null || HocKyDangChon == null) return;
        try
        {
            IsLoading = true;
            var list = await _baoCaoService.ThongKeSoLuongTheoMonAsync(HocKyDangChon.MaHocKy);
            Tab3DsThongKe.Clear();
            foreach (var item in list)
            {
                Tab3DsThongKe.Add(item);
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
    private async Task Tab3XuatExcelAsync()
    {
        if (Tab3DsThongKe.Count == 0)
        {
            ShowMessage("Không có dữ liệu thống kê để xuất Excel.", true);
            return;
        }
        if (SaveFileDialogFunc == null) return;

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<ThongKeMonDto, object?> LayGiaTri)>
            {
                ("Mã môn", x => x.MaMon),
                ("Tên môn học", x => x.TenMon),
                ("Số SV đăng ký", x => x.SoSinhVien),
                ("Số LHP đang mở", x => x.SoLhpDangMo)
            };

            byte[] excelBytes = await _excelExportService.XuatExcelAsync(
                $"ThongKe_{HocKyDangChon?.MaHocKy}",
                Tab3DsThongKe,
                cotMap);

            string fileName = $"ThongKeSVTheoMon_{HocKyDangChon?.MaHocKy}.xlsx";
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
    // TAB 4 METHODS
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
            ShowMessage("Vui lòng chọn sinh viên.", true);
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
                $"Đã tải lại phiếu ĐKHP của {Tab4SinhVienDangChon.MaSV} – {HocKyDangChon.TenHocKy}.",
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

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}
