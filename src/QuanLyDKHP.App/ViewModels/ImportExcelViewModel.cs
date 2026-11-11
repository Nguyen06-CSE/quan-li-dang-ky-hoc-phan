using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using QuanLyDKHP.App.Messages;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Entities;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class ImportExcelViewModel : ObservableObject
{
    private readonly IImportExcelService _importExcelService;
    private readonly IHocKyService _hocKyService;
    private readonly IMemoryCacheStore? _memoryCacheStore;

    public ObservableCollection<HocKy> DsHocKy { get; } = new();

    [ObservableProperty]
    private HocKy? _hocKyDangChon;

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
        IHocKyService hocKyService,
        IMemoryCacheStore? memoryCacheStore = null)
    {
        _importExcelService = importExcelService;
        _hocKyService = hocKyService;
        _memoryCacheStore = memoryCacheStore;
        _ = LoadHocKyAsync();
    }

    public ImportExcelViewModel()
    {
        _importExcelService = null!;
        _hocKyService = null!;
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
        }
        catch
        {
            // ignore
        }
    }

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

            // 1. Tải lại bộ nhớ đệm In-Memory tĩnh
            if (_memoryCacheStore != null)
            {
                try
                {
                    await _memoryCacheStore.InitializeAsync(forceReload: true);
                }
                catch { }
            }

            // 2. Phát thông báo cho toàn bộ các màn hình (Sinh viên, Môn học, Học kỳ/LHP, Trang chủ, Học phí) làm mới dữ liệu tức thì
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
