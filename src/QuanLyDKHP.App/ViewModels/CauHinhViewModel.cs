using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuanLyDKHP.Core.Dtos;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.App.ViewModels;

public partial class CauHinhViewModel : ObservableObject
{
    private readonly ICauHinhService _cauHinhService;
    private readonly IHocPhiService _hocPhiService;
    private readonly IHocKyService _hocKyService;
    private readonly ISinhVienService _sinhVienService;
    private readonly IBackupService _backupService;
    private readonly IMemoryCacheStore _memoryCacheStore;

    // Delegates tương tác hộp thoại File từ View
    public Func<string, string, byte[], Task<string?>>? SaveFileDialogFunc { get; set; }
    public Func<Task<(string FileName, Stream Content)?>>? OpenBackupFileDialogFunc { get; set; }

    [ObservableProperty]
    private int _soTinChiToiThieu = 10;

    [ObservableProperty]
    private int _soTinChiToiDa = 25;

    [ObservableProperty]
    private decimal _donGiaTinChiLT = 500000m;

    [ObservableProperty]
    private decimal _donGiaTinChiTH = 700000m;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    [ObservableProperty]
    private bool _hienThiHoiTinhLaiHocPhi;

    // --- Thuộc tính quản lý Sao lưu & Phục hồi ---
    [ObservableProperty]
    private BackupDatabaseStatusDto? _backupStatus;

    [ObservableProperty]
    private ObservableCollection<BackupCommitInfoDto> _backupHistory = new();

    [ObservableProperty]
    private BackupCommitInfoDto? _selectedBackupCommit;

    [ObservableProperty]
    private BackupCommitDetailDto? _selectedCommitDetail;

    [ObservableProperty]
    private string _ghiChuSaoLuu = string.Empty;

    [ObservableProperty]
    private bool _isBackingUp;

    [ObservableProperty]
    private bool _hienThiXacNhanKhoiPhuc;

    [ObservableProperty]
    private string _thongBaoKhoiPhuc = string.Empty;

    // --- Thuộc tính Import file Backup ---
    [ObservableProperty]
    private bool _hienThiXacNhanImportFile;

    [ObservableProperty]
    private string _pendingImportFileName = string.Empty;

    [ObservableProperty]
    private BackupArchiveManifestDto? _pendingImportManifest;

    [ObservableProperty]
    private string _thongBaoImportFile = string.Empty;

    private byte[]? _pendingImportBytes;

    // --- Cấu hình PostgreSQL Backup (tầng 2, không bắt buộc) ---
    [ObservableProperty]
    private string _backupConnectionString = string.Empty;

    [ObservableProperty]
    private string _backupConfigFilePath = string.Empty;

    [ObservableProperty]
    private bool _isTestingBackupConnection;

    [ObservableProperty]
    private bool _hasBackupConfig;

    [ObservableProperty]
    private RemoteBackupPreviewDto? _remotePreview;

    [ObservableProperty]
    private bool _hienThiXacNhanKhoiPhucRemote;

    [ObservableProperty]
    private string _thongBaoKhoiPhucRemote = string.Empty;

    private static readonly (string Table, string Label)[] PreviewTableLabels =
    [
        ("NguoiDung", "Tài khoản người dùng"),
        ("CauHinhHeThong", "Cấu hình hệ thống"),
        ("HocKy", "Học kỳ"),
        ("MonHoc", "Môn học"),
        ("SinhVien", "Sinh viên"),
        ("LopHocPhan", "Lớp học phần"),
        ("DangKyHocPhan", "Đăng ký học phần"),
        ("HocPhiHocKy", "Học phí học kỳ")
    ];

    public CauHinhViewModel(
        ICauHinhService cauHinhService,
        IHocPhiService hocPhiService,
        IHocKyService hocKyService,
        ISinhVienService sinhVienService,
        IBackupService backupService,
        IMemoryCacheStore memoryCacheStore)
    {
        _cauHinhService = cauHinhService;
        _hocPhiService = hocPhiService;
        _hocKyService = hocKyService;
        _sinhVienService = sinhVienService;
        _backupService = backupService;
        _memoryCacheStore = memoryCacheStore;

        LoadBackupConfig();
        _ = LoadCauHinhAsync();
        _ = LoadBackupInfoAsync();
    }

    public CauHinhViewModel()
    {
        _cauHinhService = null!;
        _hocPhiService = null!;
        _hocKyService = null!;
        _sinhVienService = null!;
        _backupService = null!;
        _memoryCacheStore = null!;
    }

    [RelayCommand]
    public async Task LoadCauHinhAsync()
    {
        if (_cauHinhService == null) return;
        try
        {
            IsLoading = true;
            SoTinChiToiThieu = await _cauHinhService.GetInt("SoTinChiToiThieu");
            SoTinChiToiDa = await _cauHinhService.GetInt("SoTinChiToiDa");
            DonGiaTinChiLT = await _cauHinhService.GetDecimal("DonGiaTinChiLT");
            DonGiaTinChiTH = await _cauHinhService.GetDecimal("DonGiaTinChiTH");
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải cấu hình: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadBackupInfoAsync()
    {
        if (_backupService == null) return;
        try
        {
            BackupStatus = await _backupService.GetStatusAsync();
            var commits = await _backupService.GetCommitHistoryAsync(50);
            BackupHistory.Clear();
            foreach (var c in commits)
            {
                BackupHistory.Add(c);
            }

            if (SelectedBackupCommit == null && BackupHistory.Count > 0)
            {
                await ChonXemChiTietCommitAsync(BackupHistory[0]);
            }
            else if (SelectedBackupCommit != null)
            {
                var match = BackupHistory.FirstOrDefault(x => x.CommitSha == SelectedBackupCommit.CommitSha) ?? BackupHistory.FirstOrDefault();
                if (match != null) await ChonXemChiTietCommitAsync(match);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tải lịch sử sao lưu: {ex.Message}", true);
        }
    }

    partial void OnSelectedBackupCommitChanged(BackupCommitInfoDto? value)
    {
        if (value != null && _backupService != null)
        {
            _ = LoadCommitDetailInternalAsync(value.CommitSha);
        }
    }

    [RelayCommand]
    public async Task ChonXemChiTietCommitAsync(BackupCommitInfoDto? commit)
    {
        if (commit == null) return;
        SelectedBackupCommit = commit;
        await LoadCommitDetailInternalAsync(commit.CommitSha);
    }

    private async Task LoadCommitDetailInternalAsync(string commitSha)
    {
        if (_backupService == null) return;
        try
        {
            SelectedCommitDetail = await _backupService.GetCommitDetailAsync(commitSha);
        }
        catch (Exception ex)
        {
            ShowMessage($"Không thể đọc chi tiết bản sao lưu: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task XuatFileBackupAsync(BackupCommitInfoDto? commit)
    {
        var target = commit ?? SelectedBackupCommit;
        if (_backupService == null || target == null) return;

        if (SaveFileDialogFunc == null)
        {
            ShowMessage("Không thể mở hộp thoại lưu tệp.", true);
            return;
        }

        try
        {
            IsBackingUp = true;
            var bytes = await _backupService.ExportCommitToArchiveBytesAsync(target.CommitSha);
            var suggestedName = $"QLDKHP_Backup_{target.CommittedAtLocal:yyyyMMdd_HHmm}_{target.ShortSha}.dkhp-backup.zip";
            var savedPath = await SaveFileDialogFunc(suggestedName, "zip", bytes);
            if (!string.IsNullOrEmpty(savedPath))
            {
                ShowMessage($"Đã xuất bản sao lưu {target.ShortSha} ra tệp: {savedPath}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi xuất tệp sao lưu: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    public async Task ChonFileImportBackupAsync()
    {
        if (_backupService == null || OpenBackupFileDialogFunc == null) return;

        try
        {
            var picked = await OpenBackupFileDialogFunc();
            if (picked == null) return;

            await using (picked.Value.Content)
            {
                using var ms = new MemoryStream();
                await picked.Value.Content.CopyToAsync(ms);
                var rawBytes = ms.ToArray();

                using var checkStream = new MemoryStream(rawBytes);
                var manifest = await _backupService.InspectAndValidateBackupArchiveAsync(checkStream);

                _pendingImportBytes = rawBytes;
                PendingImportFileName = picked.Value.FileName;
                PendingImportManifest = manifest;

                int totalRecords = manifest.TableRecordCounts.Values.Sum();
                var shortSha = manifest.SourceCommitSha.Length >= 7 ? manifest.SourceCommitSha[..7] : manifest.SourceCommitSha;
                ThongBaoImportFile =
                    $"Tệp sao lưu hợp lệ (Chữ ký chuẩn: {manifest.FormatSignature} & Toàn vẹn SHA-256 đã xác thực).\n\n" +
                    $"• Tên tệp: {picked.Value.FileName}\n" +
                    $"• Mã phiên bản gốc: {shortSha} ({manifest.BackupType})\n" +
                    $"• Thời điểm tạo bản gốc: {manifest.CreatedAtLocal:dd/MM/yyyy HH:mm}\n" +
                    $"• Tổng số bản ghi trong gói: {totalRecords:N0} bản ghi trên {manifest.TableRecordCounts.Count} bảng\n\n" +
                    $"Bạn có chắc chắn muốn GHI ĐÈ toàn bộ CSDL hiện tại bằng tệp này không? (Hệ thống sẽ tự động tạo điểm lưu [Pre-Import] trước khi ghi đè).";

                HienThiXacNhanImportFile = true;
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Tệp không hợp lệ: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void HuyImportGhiDe()
    {
        HienThiXacNhanImportFile = false;
        _pendingImportBytes = null;
        PendingImportManifest = null;
        PendingImportFileName = string.Empty;
    }

    [RelayCommand]
    public async Task XacNhanImportGhiDeAsync()
    {
        if (_backupService == null || _pendingImportBytes == null) return;
        HienThiXacNhanImportFile = false;

        try
        {
            IsBackingUp = true;
            using var ms = new MemoryStream(_pendingImportBytes);
            await _backupService.ImportFromBackupArchiveAsync(ms);
            await _memoryCacheStore.InitializeAsync(forceReload: true);
            await LoadCauHinhAsync();
            await LoadBackupInfoAsync();
            ShowMessage($"Đã nhập tệp '{PendingImportFileName}' và ghi đè CSDL thành công!", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi ghi đè CSDL từ tệp backup: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
            _pendingImportBytes = null;
            PendingImportManifest = null;
        }
    }

    [RelayCommand]
    public async Task TaoBanSaoLuuGitAsync()
    {
        if (_backupService == null) return;
        try
        {
            IsBackingUp = true;
            var result = await _backupService.CreateLocalGitBackupAsync("Manual", GhiChuSaoLuu, skipIfNoChanges: false);
            GhiChuSaoLuu = string.Empty;
            await LoadBackupInfoAsync();
            ShowMessage($"Tạo bản sao lưu Git thành công! (Phiên bản: {result?.ShortSha})", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi tạo bản sao lưu: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    public async Task DongBoLenDbHostAsync()
    {
        if (_backupService == null) return;
        try
        {
            IsBackingUp = true;
            var (_, msg) = await _backupService.PushToRemoteHostAsync();
            await LoadBackupInfoAsync();
            ShowMessage(msg, false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi đẩy dữ liệu lên PostgreSQL backup: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    public async Task KiemTraKetNoiBackupAsync()
    {
        if (_backupService == null) return;
        try
        {
            IsTestingBackupConnection = true;
            var (success, message) = await _backupService.TestRemoteConnectionAsync(BackupConnectionString);
            ShowMessage(message, !success);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi kiểm tra kết nối: {ex.Message}", true);
        }
        finally
        {
            IsTestingBackupConnection = false;
        }
    }

    [RelayCommand]
    public async Task LuuCauHinhBackupAsync()
    {
        if (_backupService == null) return;

        if (string.IsNullOrWhiteSpace(BackupConnectionString))
        {
            // Cho phép để trống: xóa cấu hình đã lưu (nếu có) và tiếp tục chạy offline.
            await XoaCauHinhBackupAsync();
            return;
        }

        try
        {
            IsLoading = true;
            await _backupService.SaveBackupConnectionStringAsync(BackupConnectionString);
            LoadBackupConfig();
            await LoadBackupInfoAsync();
            ShowMessage("Đã lưu cấu hình PostgreSQL backup trên máy này.", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi lưu cấu hình backup: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task XoaCauHinhBackupAsync()
    {
        if (_backupService == null) return;
        try
        {
            IsLoading = true;
            await _backupService.ClearBackupConnectionStringAsync();
            LoadBackupConfig();
            await LoadBackupInfoAsync();
            ShowMessage("Đã xóa cấu hình PostgreSQL backup trên máy này.", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi xóa cấu hình backup: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task MoXacNhanKhoiPhucRemoteAsync()
    {
        if (_backupService == null) return;
        try
        {
            IsBackingUp = true;
            var preview = await _backupService.GetRemoteBackupPreviewAsync();
            RemotePreview = preview;

            var details = string.Join("\n", PreviewTableLabels.Select(t =>
                $"   • {t.Label}: {preview.TableRecordCounts.GetValueOrDefault(t.Table):N0}"));

            ThongBaoKhoiPhucRemote =
                "Bạn có chắc chắn muốn GHI ĐÈ toàn bộ CSDL SQLite local bằng dữ liệu từ PostgreSQL backup?\n\n" +
                $"• Máy chủ: {preview.Host}\n" +
                $"• CSDL: {preview.Database}\n" +
                $"• Thời điểm đọc: {preview.CapturedAtLocal:dd/MM/yyyy HH:mm}\n" +
                $"• Tổng bản ghi: {preview.TotalRecords:N0}\n{details}\n\n" +
                "Hệ thống sẽ tạo điểm lưu Git [Pre-Restore] trước khi ghi đè. " +
                "Thao tác này chỉ đọc từ PostgreSQL, không tự đẩy ngược dữ liệu lên máy chủ.";

            HienThiXacNhanKhoiPhucRemote = true;
        }
        catch (Exception ex)
        {
            ShowMessage($"Không thể đọc dữ liệu backup: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    public void HuyKhoiPhucRemote()
    {
        HienThiXacNhanKhoiPhucRemote = false;
        RemotePreview = null;
    }

    [RelayCommand]
    public async Task XacNhanKhoiPhucRemoteAsync()
    {
        if (_backupService == null || RemotePreview == null) return;
        HienThiXacNhanKhoiPhucRemote = false;

        try
        {
            IsBackingUp = true;
            var progress = new Progress<(double Percent, string Status)>(p =>
                ShowMessage($"{p.Percent:0}% - {p.Status}", false));

            await _backupService.RestoreFromRemoteHostAsync(progress);
            await _memoryCacheStore.InitializeAsync(forceReload: true);
            await LoadCauHinhAsync();
            await LoadBackupInfoAsync();
            ShowMessage($"Đã khôi phục CSDL từ PostgreSQL backup ({RemotePreview.TotalRecords:N0} bản ghi).", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi khôi phục từ PostgreSQL backup: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
            RemotePreview = null;
        }
    }

    private void LoadBackupConfig()
    {
        if (_backupService == null) return;
        BackupConfigFilePath = _backupService.BackupConfigFilePath;
        BackupConnectionString = _backupService.GetBackupConnectionString() ?? string.Empty;
        HasBackupConfig = !string.IsNullOrWhiteSpace(BackupConnectionString);
    }

    [RelayCommand]
    public void MoXacNhanKhoiPhuc(BackupCommitInfoDto commit)
    {
        SelectedBackupCommit = commit;
        ThongBaoKhoiPhuc = $"Bạn có chắc chắn muốn khôi phục toàn bộ dữ liệu CSDL về phiên bản ngày {commit.CommittedAtLocal:dd/MM/yyyy HH:mm} (Mã: {commit.ShortSha}) không?\n\nHệ thống sẽ tự động tạo một điểm lưu an toàn trước khi khôi phục.";
        HienThiXacNhanKhoiPhuc = true;
    }

    [RelayCommand]
    public void HuyKhoiPhuc()
    {
        HienThiXacNhanKhoiPhuc = false;
        SelectedBackupCommit = null;
    }

    [RelayCommand]
    public async Task XacNhanKhoiPhucAsync()
    {
        if (_backupService == null || SelectedBackupCommit == null) return;
        HienThiXacNhanKhoiPhuc = false;

        try
        {
            IsBackingUp = true;
            await _backupService.RestoreFromGitCommitAsync(SelectedBackupCommit.CommitSha);
            await _memoryCacheStore.InitializeAsync(forceReload: true);
            await LoadCauHinhAsync();
            await LoadBackupInfoAsync();
            ShowMessage($"Đã khôi phục thành công CSDL về phiên bản {SelectedBackupCommit.ShortSha}!", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi khôi phục dữ liệu: {ex.Message}", true);
        }
        finally
        {
            IsBackingUp = false;
            SelectedBackupCommit = null;
        }
    }

    [RelayCommand]
    public async Task LuuCauHinhAsync()
    {
        if (_cauHinhService == null) return;

        // 1. Validate
        if (SoTinChiToiThieu < 1)
        {
            ShowMessage("Số tín chỉ tối thiểu phải lớn hơn hoặc bằng 1.", true);
            return;
        }

        if (SoTinChiToiDa <= SoTinChiToiThieu)
        {
            ShowMessage("Số tín chỉ tối đa phải lớn hơn số tín chỉ tối thiểu.", true);
            return;
        }

        if (DonGiaTinChiLT < 0 || DonGiaTinChiTH < 0)
        {
            ShowMessage("Đơn giá tín chỉ không được âm.", true);
            return;
        }

        try
        {
            IsLoading = true;
            var dict = new Dictionary<string, string>
            {
                ["SoTinChiToiThieu"] = SoTinChiToiThieu.ToString(),
                ["SoTinChiToiDa"] = SoTinChiToiDa.ToString(),
                ["DonGiaTinChiLT"] = DonGiaTinChiLT.ToString("F0"),
                ["DonGiaTinChiTH"] = DonGiaTinChiTH.ToString("F0")
            };

            await _cauHinhService.CapNhatAsync(dict);
            ShowMessage("Lưu cấu hình hệ thống thành công!", false);

            // Mở prompt hỏi tính lại học phí
            HienThiHoiTinhLaiHocPhi = true;
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi lưu cấu hình: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DongYTinhLaiHocPhiAsync()
    {
        HienThiHoiTinhLaiHocPhi = false;
        if (_hocKyService == null || _sinhVienService == null || _hocPhiService == null) return;

        try
        {
            IsLoading = true;
            var tatCaHk = await _hocKyService.LayTatCaAsync();
            var hkMacDinh = tatCaHk.FirstOrDefault(h => h.DangMo) ?? tatCaHk.FirstOrDefault();

            if (hkMacDinh == null)
            {
                ShowMessage("Không tìm thấy học kỳ hiện hành để tính lại học phí.", true);
                return;
            }

            var tatCaSv = await _sinhVienService.LayDanhSachAsync(null, null, null);
            await _hocPhiService.TinhLaiHocPhiHangLoatAsync(tatCaSv.Select(s => s.MaSV), hkMacDinh.MaHocKy);

            ShowMessage($"Đã tính lại học phí thành công cho {tatCaSv.Count} sinh viên trong học kỳ {hkMacDinh.TenHocKy}.", false);
        }
        catch (Exception ex)
        {
            ShowMessage($"Lỗi khi tính lại học phí: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void TuChoiTinhLaiHocPhi()
    {
        HienThiHoiTinhLaiHocPhi = false;
    }

    private void ShowMessage(string msg, bool isError)
    {
        StatusMessage = msg;
        IsStatusError = isError;
    }
}
