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

public partial class HocPhiViewModel : ObservableObject
{
    private readonly IHocPhiService _hocPhiService;
    private readonly IHocKyService _hocKyService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
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

    public HocPhiViewModel(
        IHocPhiService hocPhiService,
        IHocKyService hocKyService,
        ISinhVienService sinhVienService,
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService)
    {
        _hocPhiService = hocPhiService;
        _hocKyService = hocKyService;
        _sinhVienService = sinhVienService;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;

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
        _searchDebounceTimer = new Timer(300);
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

            var lops = await _sinhVienService.GetDanhSachLopSinhHoatAsync();
            DsLopSinhHoat.Clear();
            foreach (var l in lops)
            {
                DsLopSinhHoat.Add(l);
            }
            LopDangChon = DsLopSinhHoat.FirstOrDefault();

            var khoas = await _sinhVienService.GetDanhSachKhoaHocAsync();
            DsKhoaHoc.Clear();
            foreach (var k in khoas)
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

    partial void OnTuKhoaSinhVienChanged(string? value)
    {
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
            var result = await _sinhVienService.TimKiemAsync(TuKhoaSinhVien.Trim(), null, null, 1, 15);
            GoiYSinhVien.Clear();
            foreach (var sv in result.Items)
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
        SinhVienDangChon = sv;
        IsGoiYOpen = false;
        TuKhoaSinhVien = $"{sv.MaSV} - {sv.HoTen}";

        await LoadDanhSachTheoPhamViAsync();
    }

    [RelayCommand]
    public async Task LoadDanhSachTheoPhamViAsync()
    {
        if (_hocPhiService == null || HocKyDangChon == null) return;

        try
        {
            IsLoading = true;
            var dsMaSV = new List<string>();

            if (IsTheoSinhVien)
            {
                if (SinhVienDangChon != null)
                {
                    dsMaSV.Add(SinhVienDangChon.MaSV);
                }
                else if (!string.IsNullOrWhiteSpace(TuKhoaSinhVien))
                {
                    // Lấy các SV khớp từ khóa
                    var svs = await _sinhVienService.LayDanhSachAsync(TuKhoaSinhVien.Trim(), null, null);
                    dsMaSV.AddRange(svs.Select(s => s.MaSV));
                }
            }
            else if (IsTheoLop)
            {
                if (!string.IsNullOrWhiteSpace(LopDangChon))
                {
                    var svs = await _sinhVienService.LayDanhSachAsync(null, LopDangChon, null);
                    dsMaSV.AddRange(svs.Select(s => s.MaSV));
                }
            }
            else if (IsTheoKhoa)
            {
                if (!string.IsNullOrWhiteSpace(KhoaDangChon))
                {
                    var svs = await _sinhVienService.LayDanhSachAsync(null, null, KhoaDangChon);
                    dsMaSV.AddRange(svs.Select(s => s.MaSV));
                }
            }

            DsHocPhiTongHop.Clear();
            if (dsMaSV.Count > 0)
            {
                var result = await _hocPhiService.TinhHocPhiTheoDanhSachAsync(dsMaSV, HocKyDangChon.MaHocKy);
                foreach (var item in result)
                {
                    DsHocPhiTongHop.Add(item);
                }
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
