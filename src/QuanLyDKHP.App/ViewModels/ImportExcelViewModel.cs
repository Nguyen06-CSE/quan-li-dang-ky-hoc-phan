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
    [NotifyPropertyChangedFor(nameof(IsBusySmart))]
    private bool _isAnalyzing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusySmart))]
    private bool _isEnrolling;

    public bool IsBusySmart => IsAnalyzing || IsEnrolling;

    [ObservableProperty]
    private string? _statusSmartMessage;

    [ObservableProperty]
    private bool _isStatusSmartError;

    public ObservableCollection<SinhVienImportPreviewDto> DanhSachPreview { get; } = new();

    [ObservableProperty]
    private ClassInsightsDto? _insights;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSmartResultErrors))]
    [NotifyPropertyChangedFor(nameof(SmartResultDetailSummary))]
    private KetQuaSmartEnrollDto? _smartResult;

    public bool HasSmartResultErrors => SmartResult != null && SmartResult.ChiTietLoi.Count > 0;

    public string? SmartResultDetailSummary => SmartResult == null || SmartResult.ChiTietLoi.Count == 0
        ? null
        : string.Join(Environment.NewLine, SmartResult.ChiTietLoi.Take(8))
          + (SmartResult.ChiTietLoi.Count > 8 ? $"{Environment.NewLine}... và {SmartResult.ChiTietLoi.Count - 8} chi tiết khác." : string.Empty);

    private bool _isInitializingHocKy;
    private NavigateToImportExcelMessage? _pendingNavigationMessage;

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
            if (_isInitializingHocKy || DsHocKy.Count == 0)
            {
                _pendingNavigationMessage = message;
                return;
            }
            await ApplyNavigationMessageAsync(message);
        });
    }

    private async Task ApplyNavigationMessageAsync(NavigateToImportExcelMessage message)
    {
        if (!string.IsNullOrEmpty(message.MaHocKy))
        {
            var matchedHk = DsHocKy.FirstOrDefault(h => h.MaHocKy == message.MaHocKy);
            if (matchedHk != null && !ReferenceEquals(HocKyDangChon, matchedHk))
            {
                _isInitializingHocKy = true;
                try
                {
                    HocKyDangChon = matchedHk;
                }
                finally
                {
                    _isInitializingHocKy = false;
                }
            }
        }

        await LoadLopHocPhanAsync(message.MaLHP);
    }

    [RelayCommand]
    public async Task LoadHocKyAsync()
    {
        if (_hocKyService == null) return;
        _isInitializingHocKy = true;
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
        finally
        {
            _isInitializingHocKy = false;
        }

        if (_pendingNavigationMessage != null)
        {
            var pending = _pendingNavigationMessage;
            _pendingNavigationMessage = null;
            await ApplyNavigationMessageAsync(pending);
        }
    }

    partial void OnHocKyDangChonChanged(HocKy? value)
    {
        if (_isInitializingHocKy) return;
        _ = LoadLopHocPhanAsync();
    }

    partial void OnTuDongTaoSvChanged(bool value)
    {
        if (DanhSachPreview.Count > 0)
        {
            _ = AnalyzeSmartListAsync();
        }
    }

    private async Task LoadLopHocPhanAsync(string? preferredMaLhp = null)
    {
        if (_lopHocPhanService == null || HocKyDangChon == null)
        {
            DsLopHocPhan.Clear();
            LhpDangChon = null;
            return;
        }
        try
        {
            var lhps = await _lopHocPhanService.LayTheoHocKyAsync(HocKyDangChon.MaHocKy, null, null);
            DsLopHocPhan.Clear();
            foreach (var lhp in lhps)
            {
                DsLopHocPhan.Add(lhp);
            }
            LhpDangChon = !string.IsNullOrWhiteSpace(preferredMaLhp)
                ? DsLopHocPhan.FirstOrDefault(l => l.MaLHP == preferredMaLhp) ?? DsLopHocPhan.FirstOrDefault()
                : DsLopHocPhan.FirstOrDefault();
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

            SmartResult = null;
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
            IsAnalyzing = true;
            SmartResult = null;
            ShowSmartMessage($"Đang đọc file '{Path.GetFileName(path)}'...", false);

            List<SinhVienImportPreviewDto> items;
            if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                await using var csvStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var rawCsv = await reader.ReadToEndAsync();
                items = await Task.Run(() => _smartImportService.ParseClipboardOrText(rawCsv));
            }
            else
            {
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                items = await _smartImportService.ParseExcelStreamAsync(stream);
            }

            if (items.Count == 0)
            {
                ShowSmartMessage("Không đọc được dữ liệu sinh viên từ file này.", true);
                return;
            }

            DanhSachPreview.Clear();
            foreach (var item in items)
            {
                DanhSachPreview.Add(item);
            }

            ShowSmartMessage($"Đã đọc {items.Count} dòng từ file '{Path.GetFileName(path)}'. Đang kiểm chứng dữ liệu...", false);
            await AnalyzeSmartListInternalAsync(updateStatusBanner: true);
        }
        catch (Exception ex)
        {
            ShowSmartMessage($"Lỗi khi đọc file Excel/CSV: {ex.Message}", true);
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    public async Task AnalyzeSmartListAsync()
    {
        await AnalyzeSmartListInternalAsync(updateStatusBanner: true);
    }

    private async Task AnalyzeSmartListInternalAsync(bool updateStatusBanner)
    {
        if (DanhSachPreview.Count == 0)
            return;

        bool hasCourseColumnsInFile = DanhSachPreview.Any(x => !string.IsNullOrWhiteSpace(x.MaLHP) || !string.IsNullOrWhiteSpace(x.MaMon));

        if (HocKyDangChon == null && !hasCourseColumnsInFile)
        {
            if (updateStatusBanner)
            {
                ShowSmartMessage("Đã đọc danh sách xem trước. Hãy chọn Học kỳ và Lớp học phần đích để kiểm chứng & nạp.", true);
            }
            return;
        }

        if (LhpDangChon == null && !hasCourseColumnsInFile)
        {
            if (updateStatusBanner)
            {
                ShowSmartMessage("Đã đọc danh sách xem trước. Hãy chọn Lớp học phần đích trên thanh công cụ để kiểm chứng & nạp.", true);
            }
            return;
        }

        try
        {
            IsAnalyzing = true;
            var insights = await _smartImportService.AnalyzeAndValidateAsync(
                HocKyDangChon?.MaHocKy ?? string.Empty,
                LhpDangChon?.MaLHP ?? string.Empty,
                DanhSachPreview.ToList(),
                TuDongTaoSv);

            Insights = insights;
            if (updateStatusBanner)
            {
                int uniqueSvCount = DanhSachPreview.Where(x => !string.IsNullOrWhiteSpace(x.MaSV))
                    .Select(x => x.MaSV.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                bool hasSemesterClosedWarning = insights.DanhSachCanhBao.Any(w => w.Contains("ĐÓNG cổng", StringComparison.OrdinalIgnoreCase));
                string warnPrefix = hasSemesterClosedWarning ? "⚠️ Học kỳ đang đóng cổng — " : string.Empty;
                bool isError = hasSemesterClosedWarning || (insights.SoLuongHopLe == 0 && insights.SoLuongDaCo == 0);
                ShowSmartMessage(
                    $"{warnPrefix}Kiểm chứng hoàn tất ({DanhSachPreview.Count} lượt đăng ký / {uniqueSvCount} sinh viên): {insights.SoLuongHopLe} hợp lệ sẵn sàng nạp, {insights.SoLuongDaCo} đã có trong lớp, {insights.SoLuongLoi} lỗi.",
                    isError);
            }
        }
        catch (Exception ex)
        {
            if (updateStatusBanner)
            {
                ShowSmartMessage($"Lỗi kiểm chứng: {ex.Message}", true);
            }
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private async Task XacNhanSmartEnrollAsync()
    {
        if (DanhSachPreview.Count == 0)
        {
            ShowSmartMessage("Danh sách xem trước đang trống. Vui lòng dán từ Clipboard hoặc chọn file Excel trước.", true);
            return;
        }

        bool hasCourseColumnsInFile = DanhSachPreview.Any(x => !string.IsNullOrWhiteSpace(x.MaLHP) || !string.IsNullOrWhiteSpace(x.MaMon));

        if (HocKyDangChon == null && !hasCourseColumnsInFile)
        {
            ShowSmartMessage("Vui lòng chọn Học kỳ đích để nạp sinh viên.", true);
            return;
        }

        if (LhpDangChon == null && !hasCourseColumnsInFile)
        {
            ShowSmartMessage($"Vui lòng chọn Lớp học phần đích trên thanh công cụ trước khi nạp.", true);
            return;
        }

        var validSelected = DanhSachPreview.Where(x => x.IsSelected && x.IsHopLe).ToList();
        if (validSelected.Count == 0)
        {
            string reason = DanhSachPreview.All(x => x.IsDaCo)
                ? $"Tất cả {DanhSachPreview.Count} lượt đăng ký học phần trong danh sách đều đã tồn tại trong CSDL (0 lượt mới cần nạp thêm)."
                : DanhSachPreview.Any(x => x.IsHopLe)
                    ? "Bạn chưa tích chọn dòng hợp lệ nào (hãy nhấn 'Chọn tất cả hợp lệ' trước khi nạp)."
                    : $"Không có dòng hợp lệ nào để nạp vào CSDL (kiểm tra cột trạng thái của các dòng báo lỗi).";
            ShowSmartMessage(reason, !DanhSachPreview.All(x => x.IsDaCo));
            return;
        }

        try
        {
            IsEnrolling = true;
            SmartResult = null;
            string targetLabel = LhpDangChon?.MaLHP ?? "các lớp học phần trong file";
            ShowSmartMessage($"Đang nạp {validSelected.Count} lượt đăng ký học phần vào CSDL ({targetLabel})...", false);

            var result = await _smartImportService.EnrollStudentsAsync(
                HocKyDangChon?.MaHocKy ?? string.Empty,
                LhpDangChon?.MaLHP ?? string.Empty,
                validSelected,
                TuDongTaoSv,
                BoQuaTrungMon);

            SmartResult = result;
            bool isFailed = result.ThanhCong == 0 && result.SoCapNhat == 0 && result.Loi > 0;
            ShowSmartMessage(result.ThongBao, isFailed);

            if (result.ThanhCong > 0 || result.SoCapNhat > 0 || result.SoLopHocPhanMoiTao > 0 || result.SoMonHocMoiTao > 0)
            {
                if (result.SoLopHocPhanMoiTao > 0)
                {
                    await LoadLopHocPhanAsync(LhpDangChon?.MaLHP);
                }

                // Tải lại bộ nhớ đệm từ SQLite local
                if (_memoryCacheStore != null)
                {
                    try { await _memoryCacheStore.InitializeAsync(forceReload: true); } catch { }
                }

                // Gửi thông báo phát sóng tới tất cả màn hình
                WeakReferenceMessenger.Default.Send(new DataImportedNotificationMessage(new KetQuaImportDto
                {
                    MaHocKy = HocKyDangChon?.MaHocKy ?? string.Empty,
                    SoDangKyMoi = result.ThanhCong,
                    SoSinhVienMoi = result.SoSinhVienMoiTao,
                    SoMonHocMoi = result.SoMonHocMoiTao,
                    SoLopHocPhanMoi = result.SoLopHocPhanMoiTao
                }));
            }

            // Cập nhật lại trạng thái bảng xem trước nhưng GIỮ NGUYÊN thông báo kết quả nạp
            await AnalyzeSmartListInternalAsync(updateStatusBanner: false);
        }
        catch (Exception ex)
        {
            ShowSmartMessage($"Lỗi khi ghi nhận nạp dữ liệu: {ex.Message}", true);
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
        foreach (var item in DanhSachPreview)
        {
            if (item.IsHopLe)
            {
                item.IsSelected = true;
            }
        }
        RefreshInsightsSelectionCounts();
        ShowSmartMessage($"Đã chọn {DanhSachPreview.Count(x => x.IsHopLe && x.IsSelected)} sinh viên hợp lệ để nạp.", false);
    }

    [RelayCommand]
    private void BoChonTatCa()
    {
        foreach (var item in DanhSachPreview)
        {
            item.IsSelected = false;
        }
        RefreshInsightsSelectionCounts();
        ShowSmartMessage("Đã bỏ chọn toàn bộ sinh viên trong danh sách xem trước.", false);
    }

    private void RefreshInsightsSelectionCounts()
    {
        if (Insights == null) return;
        Insights.SiSoThemVao = DanhSachPreview.Count(x => x.IsHopLe && x.IsSelected);
        var current = Insights;
        Insights = null;
        Insights = current;
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

            await using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
