// src/QuanLyDKHP.App/ViewModels/HocPhiViewModel.cs

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
using Microsoft.EntityFrameworkCore;
using QuanLyDKHP.Infrastructure.Data;
using System.Diagnostics; // Đảm bảo đã có using này ở đầu file


namespace QuanLyDKHP.App.ViewModels;

public partial class HocPhiViewModel : ObservableObject
{
    private readonly IHocPhiService _hocPhiService;
    private readonly IHocKyService _hocKyService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly IMemoryCacheStore _cacheStore;
    private readonly ILocalReadService _localReadService;
    private readonly Timer _searchDebounceTimer;

    [ObservableProperty]
    private HocKy? _hocKyDangChon;

    [ObservableProperty]
    private string _phamVi = "SinhVien"; // "SinhVien", "Lop", "Khoa"

    [ObservableProperty]
    private bool _isTheoSinhVien = true;

    [ObservableProperty]
    private bool _isTheoLop;

    [ObservableProperty]
    private bool _isTheoKhoa;

    [RelayCommand]
    private void ChonPhamVi(string phamVi)
    {
        PhamVi = phamVi;
    }

    [ObservableProperty]
    private string? _tuKhoaSinhVien;

    [ObservableProperty]
    private bool _isGoiYOpen;

    [ObservableProperty]
    private SinhVienDto? _sinhVienDangChon;

    [ObservableProperty]
    private string? _lopDangChon;

    [ObservableProperty]
    private string? _khoaDangChon;

    [ObservableProperty]
    private HocPhiChiTietDto? _hocPhiChiTietHienTai;

    [ObservableProperty]
    private bool _isXemChiTiet;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    public ObservableCollection<HocKy> DsHocKy { get; } = new();
    public ObservableCollection<string> DsLopSinhHoat { get; } = new();
    public ObservableCollection<string> DsKhoaHoc { get; } = new();
    public ObservableCollection<SinhVienDto> GoiYSinhVien { get; } = new();
    public ObservableCollection<HocPhiTongHopDto> DsHocPhiTongHop { get; } = new();

    /// <summary>
    /// Hàm ủy nhiệm mở hộp thoại lưu file (trả về đường dẫn đã lưu).
    /// </summary>
    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    private readonly IDbContextFactory<LocalAppDbContext> _localContextFactory;

    public HocPhiViewModel(
        IHocPhiService hocPhiService,
        IHocKyService hocKyService,
        ISinhVienService sinhVienService,
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService,
        IMemoryCacheStore cacheStore,
        IDbContextFactory<LocalAppDbContext> localContextFactory,
        ILocalReadService localReadService)
        
    {
        _hocPhiService = hocPhiService;
        _hocKyService = hocKyService;
        _sinhVienService = sinhVienService;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        _cacheStore = cacheStore;
        _localContextFactory = localContextFactory;
        _localReadService = localReadService;

        _searchDebounceTimer = new Timer(300) { AutoReset = false };
        _searchDebounceTimer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(TimKiemSinhVienGoiYAsync);

        _ = InitDataAsync();
    }

    public HocPhiViewModel()
    {
        _hocPhiService = null!;
        _hocKyService = null!;
        _sinhVienService = null!;
        _pdfExportService = null!;
        _excelExportService = null!;
        _cacheStore = null!;
        _searchDebounceTimer = new Timer(300);
        _localContextFactory = null!;
        _localReadService = null!;
    }

private async Task InitDataAsync()
{
    if (_hocKyService == null) return;

    try
    {
        IsLoading = true;

        // Đảm bảo cache đã sẵn sàng (nếu người dùng login quá nhanh)
        if (!_cacheStore.IsInitialized)
        {
            await _cacheStore.InitializeAsync();
        }

        // ĐỌC THẲNG TỪ RAM (0ms - Không tốn 1 request nào lên Neon)
        DsHocKy.Clear();
        foreach (var hk in _cacheStore.DanhSachHocKy)
        {
            DsHocKy.Add(hk);
        }
        HocKyDangChon = DsHocKy.FirstOrDefault(h => h.DangMo) ?? DsHocKy.FirstOrDefault();

        DsLopSinhHoat.Clear();
        foreach (var l in _cacheStore.DanhSachLopSinhHoat)
        {
            DsLopSinhHoat.Add(l);
        }
        LopDangChon = DsLopSinhHoat.FirstOrDefault();

        DsKhoaHoc.Clear();
        foreach (var k in _cacheStore.DanhSachKhoaHoc)
        {
            DsKhoaHoc.Add(k);
        }
        KhoaDangChon = DsKhoaHoc.FirstOrDefault();
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
    partial void OnPhamViChanged(string value)
    {
        IsTheoSinhVien = value == "SinhVien";
        IsTheoLop = value == "Lop";
        IsTheoKhoa = value == "Khoa";
        _ = LoadDanhSachTheoPhamViAsync();
    }

    partial void OnHocKyDangChonChanged(HocKy? value)
    {
        _ = LoadDanhSachTheoPhamViAsync();
        if (IsXemChiTiet && HocPhiChiTietHienTai != null && value != null)
        {
            _ = LoadChiTietAsync(HocPhiChiTietHienTai.MaSV);
        }
    }

    partial void OnLopDangChonChanged(string? value)
    {
        if (IsTheoLop)
        {
            _ = LoadDanhSachTheoPhamViAsync();
        }
    }

    partial void OnKhoaDangChonChanged(string? value)
    {
        if (IsTheoKhoa)
        {
            _ = LoadDanhSachTheoPhamViAsync();
        }
    }

    private bool _isSelectingFromSuggestion;

    partial void OnTuKhoaSinhVienChanged(string? value)
    {
        if (_isSelectingFromSuggestion) return;
        
        _searchDebounceTimer.Stop();
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
        {
            GoiYSinhVien.Clear();
            IsGoiYOpen = false;
            return;
        }
        _searchDebounceTimer.Start();
    }

  private async Task TimKiemSinhVienGoiYAsync()
{
    if (string.IsNullOrWhiteSpace(TuKhoaSinhVien) || TuKhoaSinhVien.Trim().Length < 2)
    {
        GoiYSinhVien.Clear();
        IsGoiYOpen = false;
        return;
    }

    try
    {
        var key = TuKhoaSinhVien.Trim().ToLower();

        // Mở kết nối đọc trực tiếp từ SQLite Local
        await using var localContext = await _localContextFactory.CreateDbContextAsync();

        var query = localContext.SinhViens
            .AsNoTracking()
            .Where(s => !s.IsDeleted && (s.MaSV.ToLower().Contains(key) || s.HoTen.ToLower().Contains(key)))
            .OrderBy(s => s.MaSV)
            .Take(15);

        var list = await query
            .Select(s => new SinhVienDto
            {
                MaSV = s.MaSV,
                HoTen = s.HoTen,
                LopSinhHoat = s.LopSinhHoat,
                KhoaHoc = s.KhoaHoc
            })
            .ToListAsync();

        GoiYSinhVien.Clear();
        foreach (var sv in list)
        {
            GoiYSinhVien.Add(sv);
        }

        IsGoiYOpen = GoiYSinhVien.Count > 0;
    }
    catch (Exception ex)
    {
        ShowMessage(ex.Message, true);
    }
}
    [RelayCommand]
    private async Task ChonSinhVienAsync(SinhVienDto sv)
    {
        if (sv == null) return;
        try
        {
            _isSelectingFromSuggestion = true;
            SinhVienDangChon = sv;
            IsGoiYOpen = false;
            TuKhoaSinhVien = $"{sv.MaSV} - {sv.HoTen}";

            await LoadDanhSachTheoPhamViAsync();
        }
        finally
        {
            _isSelectingFromSuggestion = false;
        }
    }

[RelayCommand]
public async Task LoadDanhSachTheoPhamViAsync()
{
    if (_hocPhiService == null || HocKyDangChon == null) return;

    var totalSw = Stopwatch.StartNew();
    var stepSw = Stopwatch.StartNew();

    try
    {
        IsLoading = true;
        var dsMaSV = new List<string>();

        Console.WriteLine($"[DEBUG LoadHocPhi] Scope: IsSV={IsTheoSinhVien}, SelectedSV={SinhVienDangChon?.MaSV}, Lop={LopDangChon}, Khoa={KhoaDangChon}");

        // CHẶNG 1: LẤY MÃ SINH VIÊN TỪ SQLITE LOCAL
        stepSw.Restart();
        await using (var localContext = await _localContextFactory.CreateDbContextAsync())
        {
            if (IsTheoSinhVien)
            {
                if (SinhVienDangChon != null)
                {
                    dsMaSV.Add(SinhVienDangChon.MaSV);
                }
                else if (!string.IsNullOrWhiteSpace(TuKhoaSinhVien))
                {
                    var key = TuKhoaSinhVien.Trim().ToLower();
                    dsMaSV = await localContext.SinhViens
                        .AsNoTracking()
                        .Where(s => !s.IsDeleted && (s.MaSV.ToLower().Contains(key) || s.HoTen.ToLower().Contains(key)))
                        .Select(s => s.MaSV)
                        .Take(500)
                        .ToListAsync();
                }
                else
                {
                    // Mặc định lấy 300 sinh viên nếu chưa nhập từ khóa
                    dsMaSV = await localContext.SinhViens
                        .AsNoTracking()
                        .Where(s => !s.IsDeleted)
                        .OrderBy(s => s.MaSV)
                        .Select(s => s.MaSV)
                        .Take(300)
                        .ToListAsync();
                }
            }
            else if (IsTheoLop && !string.IsNullOrWhiteSpace(LopDangChon))
            {
                dsMaSV = await localContext.SinhViens
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted && s.LopSinhHoat == LopDangChon)
                    .Select(s => s.MaSV)
                    .ToListAsync();
            }
            else if (IsTheoKhoa && !string.IsNullOrWhiteSpace(KhoaDangChon))
            {
                dsMaSV = await localContext.SinhViens
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted && s.KhoaHoc == KhoaDangChon)
                    .Select(s => s.MaSV)
                    .ToListAsync();
            }
        }
        long timeSqlite = stepSw.ElapsedMilliseconds;

        // CHẶNG 2: GỌI SERVICE TÍNH TOÁN HỌC PHÍ
        stepSw.Restart();
        List<HocPhiTongHopDto> result = [];
        if (dsMaSV.Count > 0)
        {
            result = (await _localReadService.GetHocPhiTheoDanhSachLocalAsync(dsMaSV, HocKyDangChon.MaHocKy)).ToList();
        }
        long timeHocPhiService = stepSw.ElapsedMilliseconds;

        // CHẶNG 3: CẬP NHẬT GIAO DIỆN UI
        stepSw.Restart();
        // CHẶNG 3: CẬP NHẬT GIAO DIỆN UI
stepSw.Restart();
DsHocPhiTongHop.Clear();
foreach (var item in result)
{
    DsHocPhiTongHop.Add(item);
}
        long timeUpdateUi = stepSw.ElapsedMilliseconds;

        totalSw.Stop();

        // IN KẾT QUẢ ĐO RA TERMINAL
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n================== [BENCHMARK HỌC PHÍ] ==================");
        Console.WriteLine($"[1] Lấy {dsMaSV.Count} mã SV từ SQLite Local:    {timeSqlite} ms");
        Console.WriteLine($"[2] Tính học phí (SQLite LocalReadService):  {timeHocPhiService} ms");
        Console.WriteLine($"[3] Render lên giao diện (UI Grid):          {timeUpdateUi} ms");
        Console.WriteLine($"---> TỔNG THỜI GIAN:                         {totalSw.ElapsedMilliseconds} ms");
        Console.WriteLine($"=========================================================\n");
        Console.ResetColor();
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
    private async Task XemChiTietAsync(HocPhiTongHopDto dto)
    {
        if (dto == null || HocKyDangChon == null) return;
        await LoadChiTietAsync(dto.MaSV);
    }

    private async Task LoadChiTietAsync(string maSV)
    {
        if (HocKyDangChon == null) return;

        try
        {
            IsLoading = true;
            HocPhiChiTietHienTai = await _hocPhiService.TinhHocPhiSinhVienAsync(maSV, HocKyDangChon.MaHocKy);
            IsXemChiTiet = true;
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
    private void DongChiTiet()
    {
        IsXemChiTiet = false;
    }

    [RelayCommand]
    private async Task TinhLaiHocPhiAsync()
    {
        if (HocKyDangChon == null)
        {
            ShowMessage("Vui lòng chọn học kỳ.", true);
            return;
        }

        if (DsHocPhiTongHop.Count == 0)
        {
            ShowMessage("Không có sinh viên nào trong phạm vi hiện tại để tính lại học phí.", true);
            return;
        }

        try
        {
            IsLoading = true;
            foreach (var item in DsHocPhiTongHop)
            {
                await _hocPhiService.TinhLaiHocPhiAsync(item.MaSV, HocKyDangChon.MaHocKy);
            }

            ShowMessage($"Đã tính lại học phí thành công cho {DsHocPhiTongHop.Count} sinh viên.", false);

            await LoadDanhSachTheoPhamViAsync();

            if (IsXemChiTiet && HocPhiChiTietHienTai != null)
            {
                await LoadChiTietAsync(HocPhiChiTietHienTai.MaSV);
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
    private async Task XuatPdfChiTietAsync(HocPhiChiTietDto? chiTiet)
    {
        var target = chiTiet ?? HocPhiChiTietHienTai;
        if (target == null)
        {
            ShowMessage("Vui lòng chọn sinh viên để xuất phiếu học phí.", true);
            return;
        }

        if (SaveFileDialogFunc == null)
        {
            ShowMessage("Hộp thoại lưu file chưa được cấu hình.", true);
            return;
        }

        try
        {
            IsLoading = true;
            byte[] pdfBytes = await _pdfExportService.XuatPhieuHocPhiPdfAsync(target);
            string suggestedName = $"PhieuHocPhi_{target.MaSV}_{target.MaHocKy}.pdf";

            string? savedPath = await SaveFileDialogFunc(suggestedName, "pdf", pdfBytes);
            if (!string.IsNullOrEmpty(savedPath))
            {
                ShowMessage($"Đã xuất phiếu học phí PDF thành công tại: {savedPath}", false);
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
    private async Task XuatExcelDanhSachAsync()
    {
        if (DsHocPhiTongHop.Count == 0)
        {
            ShowMessage("Không có dữ liệu trong bảng để xuất Excel.", true);
            return;
        }

        if (SaveFileDialogFunc == null)
        {
            ShowMessage("Hộp thoại lưu file chưa được cấu hình.", true);
            return;
        }

        try
        {
            IsLoading = true;
            var cotMap = new List<(string TieuDe, Func<HocPhiTongHopDto, object?> LayGiaTri)>
            {
                ("Mã SV", x => x.MaSV),
                ("Họ và tên", x => x.HoTen),
                ("Lớp sinh hoạt", x => x.LopSinhHoat ?? string.Empty),
                ("Tổng số tín chỉ", x => x.TongSoTinChi),
                ("Tổng học phí (VNĐ)", x => x.TongHocPhi),
                ("Đã đóng (VNĐ)", x => x.DaDong),
                ("Còn nợ (VNĐ)", x => x.ConNo),
                ("Trạng thái", x => x.TrangThaiText)
            };

            string hkName = HocKyDangChon?.TenHocKy ?? "HocKy";
            byte[] excelBytes = await _excelExportService.XuatExcelAsync($"HocPhi_{HocKyDangChon?.MaHocKy}", DsHocPhiTongHop, cotMap);
            string suggestedName = $"DanhSachHocPhi_{HocKyDangChon?.MaHocKy}.xlsx";

            string? savedPath = await SaveFileDialogFunc(suggestedName, "xlsx", excelBytes);
            if (!string.IsNullOrEmpty(savedPath))
            {
                ShowMessage($"Đã xuất danh sách Excel thành công tại: {savedPath}", false);
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
