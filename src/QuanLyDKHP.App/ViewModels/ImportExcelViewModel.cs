using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using QuanLyDKHP.App.Messages;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class ImportExcelViewModel : ObservableObject, IRecipient<NavigateToImportExcelMessage>
{
    private readonly IImportExcelService _importExcelService;
    private readonly ISmartImportService _smartImportService;
    private readonly IHocKyService _hocKyService;
    private readonly ILopHocPhanService _lopHocPhanService;
    private readonly IMemoryCacheStore? _memoryCacheStore;

    // --- TAB ĐIỀU HƯỚNG ---
    [ObservableProperty]
    private int _selectedTabIndex = 0; // 0: Nạp SV vào LHP (Smart Studio), 1: File 21 cột toàn trường

    // --- DỮ LIỆU DÙNG CHUNG ---
    public ObservableCollection<HocKy> DsHocKy { get; } = new();

    [ObservableProperty]
    private HocKy? _hocKyDangChon;

    // ==========================================
    // PHÂN HỆ 1 & 3: SMART IMPORT STUDIO & CLASS INSIGHTS
    // ==========================================
    public ObservableCollection<LopHocPhan> DsLopHocPhan { get; } = new();

    [ObservableProperty]
    private LopHocPhan? _lhpDangChon;

    [ObservableProperty]
    private bool _tuDongTaoSv = true;

    [ObservableProperty]
    private bool _boQuaTrungMon = true;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private bool _isEnrolling;

    [ObservableProperty]
    private string? _statusSmartMessage;

    [ObservableProperty]
    private bool _isStatusSmartError;

    public ObservableCollection<SinhVienImportPreviewDto> DanhSachPreview { get; } = new();

    [ObservableProperty]
    private ClassInsightsDto? _insights;

    [ObservableProperty]
    private KetQuaSmartEnrollDto? _smartResult;

    // ==========================================
    // PHÂN HỆ 2: IMPORT MASS 21 CỘT (LEGACY / UNIVERSAL)
    // ==========================================
    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private string? _fileName;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private int _tienDo;

    [ObservableProperty]
    private string _thongBaoTienDo = "Sẵn sàng import";

    [ObservableProperty]
    private KetQuaImportDto? _ketQua;

    public ObservableCollection<LogDongImportDto> DanhSachLogHienThi { get; } = new();

    public Func<Task<string?>>? OpenFileDialogFunc { get; set; }
    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }

    public ImportExcelViewModel(
        IImportExcelService importExcelService,
        ISmartImportService smartImportService,
        IHocKyService hocKyService,
        ILopHocPhanService lopHocPhanService,
        IMemoryCacheStore? memoryCacheStore = null)
    {
        _importExcelService = importExcelService;
        _smartImportService = smartImportService;
        _hocKyService = hocKyService;
        _lopHocPhanService = lopHocPhanService;
        _memoryCacheStore = memoryCacheStore;

        WeakReferenceMessenger.Default.RegisterAll(this);

        _ = InitDataAsync();
    }

    public ImportExcelViewModel()
    {
        _importExcelService = null!;
        _smartImportService = null!;
        _hocKyService = null!;
        _lopHocPhanService = null!;
    }

    private async Task InitDataAsync()
    {
        await LoadHocKyAsync();
    }

    public void Receive(NavigateToImportExcelMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            SelectedTabIndex = 0; // Chuyển sang Tab Smart Import
            if (!string.IsNullOrEmpty(message.MaHocKy))
            {
                var matchedHk = DsHocKy.FirstOrDefault(h => h.MaHocKy == message.MaHocKy);
                if (matchedHk != null)
                {
                    HocKyDangChon = matchedHk;
                }
            }

            if (!string.IsNullOrEmpty(message.MaLHP))
            {
                await LoadLopHocPhanAsync();
                var matchedLhp = DsLopHocPhan.FirstOrDefault(l => l.MaLHP == message.MaLHP);
                if (matchedLhp != null)
                {
                    LhpDangChon = matchedLhp;
                }
            }
        });
    }

    [RelayCommand]
    public async Task LoadHocKyAsync()
    {
        if (_hocKyService == null) return;
        try
        {
            var hks = await _hocKyService.LayTatCaAsync();
            DsHocKy.Clear();
            foreach (var hk in hks)
            {
                DsHocKy.Add(hk);
            }
            HocKyDangChon = hks.FirstOrDefault(h => h.DangMo) ?? hks.FirstOrDefault();
            await LoadLopHocPhanAsync();
        }
        catch
        {
            // ignore
        }
    }

    partial void OnHocKyDangChonChanged(HocKy? value)
    {
        _ = LoadLopHocPhanAsync();
    }

    private async Task LoadLopHocPhanAsync()
    {
        if (_lopHocPhanService == null || HocKyDangChon == null) return;
        try
        {
            var lhps = await _lopHocPhanService.LayTheoHocKyAsync(HocKyDangChon.MaHocKy, null, null);
            DsLopHocPhan.Clear();
            foreach (var lhp in lhps)
            {
                DsLopHocPhan.Add(lhp);
            }
            LhpDangChon = DsLopHocPhan.FirstOrDefault();
        }
        catch
        {
            // ignore
        }
    }

    partial void OnLhpDangChonChanged(LopHocPhan? value)
    {
        if (DanhSachPreview.Count > 0)
        {
            _ = AnalyzeSmartListAsync();
        }
    }

    // ==========================================
    // LOGIC SMART IMPORT (DÁN CLIPBOARD & PREVIEW)
    // ==========================================
    [RelayCommand]
    private async Task DanTuClipboardAsync()
    {
        try
        {
            string? clipboardText = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow?.Clipboard is { } clipboard)
            {
                clipboardText = await clipboard.TryGetTextAsync();
            }

            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                ShowSmartMessage("Bộ nhớ tạm (Clipboard) đang trống. Vui lòng copy danh sách sinh viên từ Excel/Sheets/Zalo trước.", true);
                return;
            }

            var items = _smartImportService.ParseClipboardOrText(clipboardText);
            if (items.Count == 0)
            {
                ShowSmartMessage("Không phân tích được dòng dữ liệu sinh viên nào từ bộ nhớ tạm.", true);
                return;
            }

            DanhSachPreview.Clear();
            foreach (var item in items)
            {
                DanhSachPreview.Add(item);
            }

            ShowSmartMessage($"Đã dán thành công {items.Count} dòng sinh viên từ Clipboard. Đang kiểm chứng dữ liệu...", false);
            await AnalyzeSmartListAsync();
        }
        catch (Exception ex)
        {
            ShowSmartMessage($"Lỗi khi đọc từ Clipboard: {ex.Message}", true);
        }
    }

    [RelayCommand]
    private async Task ChonFileSmartImportAsync()
    {
        if (OpenFileDialogFunc == null) return;
        var path = await OpenFileDialogFunc();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        try
        {
            await using var stream = File.OpenRead(path);
            var items = await _smartImportService.ParseExcelStreamAsync(stream);
            if (items.Count == 0)
            {
                ShowSmartMessage("Không đọc được dữ liệu sinh viên từ file Excel này.", true);
                return;
            }

            DanhSachPreview.Clear();
            foreach (var item in items)
            {
                DanhSachPreview.Add(item);
            }

            ShowSmartMessage($"Đã nạp {items.Count} sinh viên từ file '{Path.GetFileName(path)}'. Đang kiểm chứng dữ liệu...", false);
            await AnalyzeSmartListAsync();
        }
        catch (Exception ex)
        {
            ShowSmartMessage($"Lỗi khi đọc file Excel: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task AnalyzeSmartListAsync()
    {
        if (LhpDangChon == null || HocKyDangChon == null || DanhSachPreview.Count == 0)
            return;

        try
        {
            IsAnalyzing = true;
            var insights = await _smartImportService.AnalyzeAndValidateAsync(
                HocKyDangChon.MaHocKy,
                LhpDangChon.MaLHP,
                DanhSachPreview.ToList(),
                TuDongTaoSv);

            Insights = insights;
            ShowSmartMessage($"Kiểm chứng hoàn tất: {insights.SoLuongHopLe} hợp lệ, {insights.SoLuongDaCo} đã trong lớp, {insights.SoLuongLoi} lỗi.", insights.SoLuongLoi > 0);
        }
        catch (Exception ex)
        {
            ShowSmartMessage($"Lỗi kiểm chứng: {ex.Message}", true);
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private async Task XacNhanSmartEnrollAsync()
    {
        if (LhpDangChon == null || HocKyDangChon == null)
        {
            ShowSmartMessage("Vui lòng chọn Học kỳ và Lớp học phần đích để nạp.", true);
            return;
        }

        var validSelected = DanhSachPreview.Where(x => x.IsSelected && x.IsHopLe).ToList();
        if (validSelected.Count == 0)
        {
            ShowSmartMessage("Không có sinh viên hợp lệ nào được chọn để nạp vào lớp.", true);
            return;
        }

        try
        {
            IsEnrolling = true;
            StatusSmartMessage = "Đang nạp sinh viên vào lớp học phần và tính toán học phí...";

            var result = await _smartImportService.EnrollStudentsAsync(
                HocKyDangChon.MaHocKy,
                LhpDangChon.MaLHP,
                validSelected,
                TuDongTaoSv,
                BoQuaTrungMon);

            SmartResult = result;
            ShowSmartMessage(result.ThongBao, result.Loi > 0 && result.ThanhCong == 0);

            // Tải lại bộ nhớ đệm
            if (_memoryCacheStore != null)
            {
                try { await _memoryCacheStore.InitializeAsync(forceReload: true); } catch { }
            }

            // Gửi thông báo phát sóng tới tất cả màn hình
            WeakReferenceMessenger.Default.Send(new DataImportedNotificationMessage(new KetQuaImportDto
            {
                MaHocKy = HocKyDangChon.MaHocKy,
                SoDangKyMoi = result.ThanhCong,
                SoSinhVienMoi = result.SoSinhVienMoiTao
            }));

            // Cập nhật lại số liệu phân tích sau khi nạp
            await AnalyzeSmartListAsync();
        }
        catch (Exception ex)
        {
            ShowSmartMessage($"Lỗi khi ghi nhận nạp sinh viên: {ex.Message}", true);
        }
        finally
        {
            IsEnrolling = false;
        }
    }

    [RelayCommand]
    private void XoaDanhSachPreview()
    {
        DanhSachPreview.Clear();
        Insights = null;
        SmartResult = null;
        StatusSmartMessage = "Đã dọn sạch danh sách xem trước.";
        IsStatusSmartError = false;
    }

    [RelayCommand]
    private void ChonTatCa()
    {
        foreach (var item in DanhSachPreview.Where(x => x.IsHopLe))
        {
            item.IsSelected = true;
        }
        _ = AnalyzeSmartListAsync();
    }

    [RelayCommand]
    private void BoChonTatCa()
    {
        foreach (var item in DanhSachPreview)
        {
            item.IsSelected = false;
        }
        _ = AnalyzeSmartListAsync();
    }

    private void ShowSmartMessage(string msg, bool isError)
    {
        StatusSmartMessage = msg;
        IsStatusSmartError = isError;
    }

    // ==========================================
    // LOGIC TAB 2: MASS IMPORT 21 CỘT (LEGACY / UNIVERSAL)
    // ==========================================
    [RelayCommand]
    private async Task ChonFileAsync()
    {
        if (OpenFileDialogFunc == null) return;
        var path = await OpenFileDialogFunc();
        if (!string.IsNullOrEmpty(path))
        {
            FilePath = path;
            FileName = Path.GetFileName(path);
            KetQua = null;
            DanhSachLogHienThi.Clear();
            TienDo = 0;
            ThongBaoTienDo = "Đã chọn file: " + FileName;
        }
    }

    [RelayCommand]
    private async Task BatDauImportAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            ThongBaoTienDo = "Vui lòng chọn file Excel hợp lệ.";
            return;
        }

        try
        {
            IsImporting = true;
            TienDo = 0;
            ThongBaoTienDo = "Đang đọc và phân tích file Excel...";

            var progress = new Progress<int>(percent =>
            {
                TienDo = percent;
                if (percent < 90)
                    ThongBaoTienDo = $"Đang import dữ liệu... ({percent}%)";
                else if (percent < 100)
                    ThongBaoTienDo = $"Đang tính toán học phí tự động... ({percent}%)";
                else
                    ThongBaoTienDo = "Hoàn tất import!";
            });

            await using var stream = File.OpenRead(FilePath);
            var result = await _importExcelService.ImportFileAsync(stream, HocKyDangChon?.MaHocKy, progress);

            KetQua = result;
            if (HocKyDangChon == null && !string.IsNullOrEmpty(result.MaHocKy))
            {
                var matchedHk = DsHocKy.FirstOrDefault(h => h.MaHocKy == result.MaHocKy);
                if (matchedHk != null)
                {
                    HocKyDangChon = matchedHk;
                }
                else
                {
                    var newHk = new HocKy 
                    { 
                        MaHocKy = result.MaHocKy, 
                        TenHocKy = !string.IsNullOrEmpty(result.TenHocKy) ? result.TenHocKy : $"Học kỳ {result.MaHocKy}" 
                    };
                    DsHocKy.Add(newHk);
                    HocKyDangChon = newHk;
                }
            }

            DanhSachLogHienThi.Clear();
            foreach (var log in result.DanhSachLog)
            {
                DanhSachLogHienThi.Add(log);
            }

            ThongBaoTienDo = $"Import xong! Thành công: {result.SoDongThanhCong}/{result.TongSoDong} dòng, Lỗi: {result.SoDongLoi}, Cảnh báo: {result.DanhSachLog.Count(l => l.LoaiLog == "CanhBao")}";

            if (_memoryCacheStore != null)
            {
                try { await _memoryCacheStore.InitializeAsync(forceReload: true); } catch { }
            }

            WeakReferenceMessenger.Default.Send(new DataImportedNotificationMessage(result));
        }
        catch (Exception ex)
        {
            ThongBaoTienDo = $"Lỗi trong quá trình import: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private async Task XuatLogTxtAsync()
    {
        var kq = KetQua;
        if (kq == null || SaveFileDialogFunc == null) return;

        string tenHkLog = HocKyDangChon?.TenHocKy ?? kq.TenHocKy ?? "Tự động nhận diện";
        string maHkLog = HocKyDangChon?.MaHocKy ?? kq.MaHocKy ?? "Auto";

        var sb = new StringBuilder();
        sb.AppendLine("=== KẾT QUẢ IMPORT EXCEL ===");
        sb.AppendLine($"Học kỳ: {tenHkLog} ({maHkLog})");
        sb.AppendLine($"File: {FileName}");
        sb.AppendLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Tổng số dòng: {kq.TongSoDong}");
        sb.AppendLine($"Thành công: {kq.SoDongThanhCong}");
        sb.AppendLine($"Lỗi: {kq.SoDongLoi}");
        sb.AppendLine($"Sinh viên mới: {kq.SoSinhVienMoi}");
        sb.AppendLine($"Môn học mới: {kq.SoMonHocMoi}");
        sb.AppendLine($"Lớp học phần mới: {kq.SoLopHocPhanMoi}");
        sb.AppendLine($"Lượt đăng ký mới: {kq.SoDangKyMoi}");
        sb.AppendLine();
        sb.AppendLine("=== CHI TIẾT LOG ===");
        foreach (var log in kq.DanhSachLog)
        {
            sb.AppendLine($"[Dòng {log.DongSo}] [{log.LoaiLog.ToUpper()}] {log.NoiDung}");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        string defaultName = $"LogImport_{maHkLog}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        await SaveFileDialogFunc(defaultName, "txt", bytes);
    }
}
